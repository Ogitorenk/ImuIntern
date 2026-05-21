using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Ayarlar")]
    public float damage = 20f;
    public float lifeTime = 5f;

    private Rigidbody rb;
    private bool hasHit = false;

    [Header("Uçuş Fiziği")]
    [Tooltip("Crosshair'a dümdüz (lazer gibi) gitmesi için yerçekimini 0 yapıyoruz kanka!")]
    public float customGravity = 0f; // Dümdüz gitmesi için burayı 0 yaptık!

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false; // Unity'nin o ağır yerçekimini KAPATTIK!
        }

        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (!hasHit)
        {
            // Eğer customGravity 0'dan büyükse aşağı çeker, 0 ise ok ip gibi dümdüz gider kanka!
            if (customGravity > 0.01f)
            {
                rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
            }

            // Okun havada giderken yönüne doğru bakması (Mızrak gibi dönme efekti)
            if (rb != null && rb.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.velocity);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit || other.CompareTag("Player")) return;

        // ========================================================
        // --- GÜNCELLENDİ: SLIME, FARE VE HAYDUTU BULMA GARANTİSİ ---
        // ========================================================
        IDamageable enemy = other.GetComponent<IDamageable>();
        if (enemy == null) enemy = other.GetComponentInParent<IDamageable>();
        if (enemy == null) enemy = other.GetComponentInChildren<IDamageable>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage); // Slime, Fare veya Hayduta hasarı yapıştır!
            Debug.Log($"🎯 Ok {other.gameObject.name} düşmanına saplandı! Hasar: {damage}");

            Destroy(gameObject); // Düşmana çarpınca ok yok olsun
            return;
        }

        // Zemine, duvara veya haritadaki diğer statik objelere çarptıysa saplanıp kalsın
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.CompareTag("Ground") || other.gameObject.CompareTag("Wall"))
        {
            hasHit = true;
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
            Destroy(gameObject, 1f); // Saplandıktan 1 saniye sonra sahnede kalabalık yapmasın, silinsin
        }
    }
}