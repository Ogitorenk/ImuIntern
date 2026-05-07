using UnityEngine;
using System.Collections; // YENİ: Coroutine kullanmak için eklendi

public class LanceObj : MonoBehaviour
{
    private Rigidbody rb;
    public bool isStuck = false;

    [Header("--- Saplanma Ayarları ---")]
    public float embedDepth = 0.2f;
    public float maxHitAngle = 45f;

    // --- YENİ: SAPLANMA ROTASYONU ---
    [Tooltip("Duvara saplandığında ters duruyorsa bu değerleri 0, 0, 0 yap!")]
    public Vector3 stickRotationOffset = new Vector3(90f, 0f, 0f);

    // --- YENİ EKLENDİ: Duvarın dışarı doğru bakan yönü ---
    [HideInInspector] public Vector3 wallNormal;

    private Coroutine destroyRoutine; // YENİ: Ölüm sayacını tutacağımız değişken

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

        // 🚨 KONSOL AJANI
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

        // --- YENİ EKLENDİ: Duvarın yönünü kaydet ki karakter bilsin ---
        wallNormal = contact.normal;

        // --- YENİ: ÖLÜM SAYACINI İPTAL ET ---
        // Eğer sürtünüp silinme emri aldıysa, duvara saplandığı için o emri iptal ediyoruz!
        if (destroyRoutine != null)
        {
            StopCoroutine(destroyRoutine);
            destroyRoutine = null;
            Debug.Log("🛡️ Mızrak saplandığı için yok olma emri iptal edildi!");
        }

        // --- GÜNCELLENDİ: ZORLA BÜKME YERİNE AYARLANABİLİR BÜKME ---
        Quaternion lookRot = Quaternion.LookRotation(-contact.normal);
        transform.rotation = lookRot * Quaternion.Euler(stickRotationOffset);

        transform.position = contact.point;
        transform.position += -contact.normal * embedDepth;

        transform.SetParent(collision.transform);
        gameObject.tag = "Lance";
    }

    private void CancelStick()
    {
        gameObject.tag = "Untagged";

        // --- GÜNCELLENDİ: İPTAL EDİLEBİLİR YOK OLMA SİSTEMİ ---
        if (destroyRoutine == null && gameObject.activeInHierarchy)
        {
            destroyRoutine = StartCoroutine(DestroyAfterTime(2f));
        }
    }

    private IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}