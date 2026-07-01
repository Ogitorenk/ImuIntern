using UnityEngine;
using System.Collections; // YENİ: Coroutine kullanmak için eklendi

public class LanceObj : MonoBehaviour
{
    // ========================================================
    // --- SAHNEDEKİ TEK MIZRAĞI TUTACAK HAFIZA ---
    // ========================================================
    private static LanceObj currentActiveLance;

    private Rigidbody rb;
    public bool isStuck = false;

    [Header("--- Saplanma Ayarları ---")]
    public float embedDepth = 0.2f;
    public float maxHitAngle = 45f;

    // --- SAPLANMA ROTASYONU ---
    [Tooltip("Duvara saplandığında ters duruyorsa bu değerleri 0, 0, 0 yap!")]
    public Vector3 stickRotationOffset = new Vector3(90f, 0f, 0f);

    // --- Duvarın dışarı doğru bakan yönü ---
    [HideInInspector] public Vector3 wallNormal;

    private Coroutine destroyRoutine; // Ölüm sayacını tutacağımız değişken

    [Header("--- Düşman Hasar Ayarları ---")]
    [Tooltip("Mızrak düşmana çarptığında ne kadar hasar verecek?")]
    public float damageAmount = 40f;
    private bool hasHitEnemy = false; // Çift vuruş bug'ını önlemek için emniyet kilidi

    // ========================================================
    // --- YENİ MIZRAK DOĞDUĞUNDA ESKİSİNİ YOK ET ---
    // ========================================================
    void Awake()
    {
        if (currentActiveLance != null && currentActiveLance != this)
        {
            Destroy(currentActiveLance.gameObject);
        }

        currentActiveLance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || collision.gameObject.CompareTag("Player")) return;

        // ========================================================
        // === DÜŞMAN HASAR KONTROLÜ ===
        // ========================================================
        IDamageable enemy = collision.gameObject.GetComponent<IDamageable>();
        if (enemy == null) enemy = collision.gameObject.GetComponentInParent<IDamageable>();

        if (enemy != null)
        {
            if (!hasHitEnemy)
            {
                hasHitEnemy = true;
                enemy.TakeDamage(damageAmount);
                Debug.Log($"🎯 Mızrak {collision.gameObject.name} düşmanına çarptı ve {damageAmount} hasar verdi!");

                if (currentActiveLance == this) currentActiveLance = null; // Bellek temizliği kanka
                Destroy(gameObject);
            }
            return;
        }

        // ========================================================
        // --- SENİN ORİJİNAL DUVARA SAPLANMA MANTIĞIN ---
        // ========================================================
        if (!collision.gameObject.CompareTag("Wall"))
        {
            CancelStick();
            return;
        }

        ContactPoint contact = collision.contacts[0];

        // --- KÖŞE BUG'I VE HIZ DÜZELTMESİ ---
        Vector3 gercekCarpmaYonu = -collision.relativeVelocity.normalized;

        float hitAngle = Vector3.Angle(gercekCarpmaYonu, -contact.normal);

        Debug.Log($"Mızrak Duvara Vurdu! Çarpma Açısı: {hitAngle}");

        if (hitAngle > maxHitAngle)
        {
            Debug.Log("❌ AÇI ÇOK GENİŞ! Mızrak saplanması iptal edildi.");
            CancelStick();
            return;
        }

        // --- KUSURSUZ SAPLANMA ---
        Debug.Log("✅ AÇI UYGUN! Mızrak saplanıyor.");
        isStuck = true;

        // ==============================================================================
        // === FİZİKSEL ARTIK TEMİZLEME DUVARI: Saçmalamayı engelleyen asıl kısım burası kanka ===
        // ==============================================================================
        if (rb != null)
        {
            rb.velocity = Vector3.zero;        // Mızrağın ileri/geri tüm hızını anında sıfırla
            rb.angularVelocity = Vector3.zero; // Çarpma anındaki dönme momentumunu tamamen sıfırlaki dik kalmasın
            rb.isKinematic = true;             // Fizik motorunu devre dışı bırak
        }

        wallNormal = contact.normal;

        if (destroyRoutine != null)
        {
            StopCoroutine(destroyRoutine);
            destroyRoutine = null;
            Debug.Log("🛡️ Mızrak saplandığı için yok olma emri iptal edildi!");
        }

        // === SEVDİĞİN ORİJİNAL HESAPLAMALARINA HİÇ DOKUNULMADI KANKA ===
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

        if (destroyRoutine == null && gameObject.activeInHierarchy)
        {
            destroyRoutine = StartCoroutine(DestroyAfterTime(2f));
        }
    }

    private IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        if (currentActiveLance == this) currentActiveLance = null; // Bellek emniyeti
        Destroy(gameObject);
    }
}