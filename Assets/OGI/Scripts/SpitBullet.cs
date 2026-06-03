using UnityEngine;

public class SpitBullet : MonoBehaviour
{
    private float damage = 10f;
    private float lifetime = 5f; // Havada kalma sınırı

    [Header("--- ROTASYON AYARI (YUKARI BAKMA BUG FIX) ---")]
    [Tooltip("Ok havada doğduğunda X ekseninde kaç derece yatsın? Genelde 90 kanka.")]
    public float fixRotationX = 90f;
    [Tooltip("Eğer ok sağa sola bakarsa burayı değiştirirsin, şimdilik 0 kanka.")]
    public float fixRotationY = 0f;
    [Tooltip("Eğer ok eksen etrafında dönerse burayı değiştirirsin kanka.")]
    public float fixRotationZ = 0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Havada hiçbir yere çarpmazsa 5 saniye sonra kendi kendini imha etsin
        Destroy(gameObject, lifetime);

        // --- GÜNCELLENDİ: OKUN KAFASINI KODLA YERE YATIRMA HİLESİ ---
        // Ok doğduğu an mevcut bakış yönünün üzerine bizim verdiğimiz düzeltme açısını çakıyoruz kanka
        transform.localRotation = transform.localRotation * Quaternion.Euler(fixRotationX, fixRotationY, fixRotationZ);
    }

    void Update()
    {
        // --- YENİ KONTROL: OKUN HAVADA GİDERKEN YÖNÜNÜN BOZULMAMASI İÇİN ---
        // Eğer ok Rigidbody hızıyla gidiyorsa, burnu her zaman gittiği yöne doğru baksın kanka
        if (rb != null && rb.velocity != Vector3.zero)
        {
            // Gittiği yöne doğru rotasyon oluşturup üzerine bizim 90 derecelik düzeltmeyi ekliyoruz
            Quaternion targetRotation = Quaternion.LookRotation(rb.velocity.normalized);
            transform.rotation = targetRotation * Quaternion.Euler(fixRotationX, fixRotationY, fixRotationZ);
        }
    }

    // Bu fonksiyonu haydut veya bitki mermiyi ateşlerken hasarı dinamik ayarlasın diye çağırıyoruz
    public void SetupBullet(float bulletDamage)
    {
        damage = bulletDamage;
    }

    // Tükürük/Ok bir şeye çarptığı an bu fonksiyon tetiklenir (Is Trigger açık olmalı!)
    void OnTriggerEnter(Collider other)
    {
        // 1. Kendi takım arkadaşını (Düşmanları) vurmasın kanka!
        if (other.CompareTag("Enemy")) return;

        // 2. Eğer çarptığı şey bizim Player ise:
        if (other.CompareTag("Player"))
        {
            IDamageable playerDamageable = other.GetComponent<IDamageable>();
            if (playerDamageable != null)
            {
                playerDamageable.TakeDamage(damage);
                Debug.Log($"🎯 Ok oyuncuya çarptı ve {damage} hasar verdi!");
            }

            // Oyuncuya çarptığı için mermiyi yok et kanka
            Destroy(gameObject);
            return;
        }

        // 3. Eğer haritada "Ground" tag'li bir duvara, zemine veya statik objeye çarparsa:
        if (other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Debug.Log("🎯 Ok zemine/duvara çarptı ve patladı.");
            Destroy(gameObject);
        }
    }
}