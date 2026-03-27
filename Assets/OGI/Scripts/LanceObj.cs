using UnityEngine;

public class LanceObj : MonoBehaviour
{
    private Rigidbody rb;
    public bool isStuck = false;

    [Header("--- Saplanma Ayarları ---")]
    public float embedDepth = 0.2f;
    public float maxHitAngle = 45f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || collision.gameObject.CompareTag("Player")) return;

        if (!collision.gameObject.CompareTag("Wall"))
        {
            CancelStick();
            return;
        }

        ContactPoint contact = collision.contacts[0];

        // --- KÖŞE BUG'I VE HIZ DÜZELTMESİ ---
        // Unity'nin sekme yalanına kanmamak için gerçek çarpışma vektörünü alıyoruz (- relativeVelocity)
        Vector3 gercekCarpmaYonu = -collision.relativeVelocity.normalized;

        // Mızrağın geliş açısı ile duvarın yüzey açısını karşılaştır
        float hitAngle = Vector3.Angle(gercekCarpmaYonu, -contact.normal);

        // 🚨 KONSOL AJANI: Eğer mızrak tutunmazsa konsola bak, sana sebebini söyleyecek!
        Debug.Log($"Mızrak Duvara Vurdu! Çarpma Açısı: {hitAngle}");

        // Çok yandan veya köşeden çarptıysa saplanma, sekip düşsün
        if (hitAngle > maxHitAngle)
        {
            Debug.Log("❌ AÇI ÇOK GENİŞ! Mızrak saplanması iptal edildi.");
            CancelStick();
            return;
        }

        // --- KUSURSUZ SAPLANMA ---
        Debug.Log("✅ AÇI UYGUN! Mızrak saplanıyor.");
        isStuck = true;
        rb.isKinematic = true;

        Quaternion lookRot = Quaternion.LookRotation(-contact.normal);
        transform.rotation = lookRot * Quaternion.Euler(90f, 0f, 0f);

        transform.position = contact.point;
        transform.position += -contact.normal * embedDepth;

        transform.SetParent(collision.transform);
        gameObject.tag = "Lance";
    }

    private void CancelStick()
    {
        gameObject.tag = "Untagged";
        Destroy(gameObject, 2f);
    }
}