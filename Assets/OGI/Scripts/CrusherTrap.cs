using UnityEngine;
using System.Collections;

public class CrusherTrap : MonoBehaviour
{
    [Header("Mekanik Ayarlar")]
    public float dropSpeed = 40f;    // Küt diye inme hızı
    public float riseSpeed = 8f;     // Yavaşça kalkma hızı
    public float downWaitTime = 1f;  // Yerde ne kadar beklesin?
    public float upWaitTime = 2f;    // Tavanda ne kadar beklesin?
    public float dropDistance = 6f;  // Ne kadar aşağı inecek?

    [Header("Fabrika Senkronizasyonu")]
    [Tooltip("Oyun başladığında kaç saniye bekleyip harekete geçsin? (Sıralı dizim için)")]
    public float startDelay = 0f;

    [Header("Ses Menzil Ayarları (Büyütüldü)")]
    [Tooltip("Sesin duyulmaya başlayacağı maksimum mesafe kanka.")]
    public float maxAudioDistance = 35f; // Menzil 35 metreye çıkarıldı!

    private Vector3 startPos;
    private Vector3 targetPos;
    private Transform playerTransform;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + (Vector3.down * dropDistance);

        StartCoroutine(CrusherRoutine());
    }

    void Update()
    {
        // Sahnede aktif olan oyuncuyu canlı tespit etme
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            DonMovement don = FindObjectOfType<DonMovement>();
            if (don != null && don.gameObject.activeInHierarchy)
            {
                playerTransform = don.transform;
            }
            else
            {
                SanchoMovement sancho = FindObjectOfType<SanchoMovement>();
                if (sancho != null && sancho.gameObject.activeInHierarchy)
                {
                    playerTransform = sancho.transform;
                }
                else
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null && playerObj.activeInHierarchy)
                    {
                        playerTransform = playerObj.transform;
                    }
                }
            }
        }
    }

    IEnumerator CrusherRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // === KÜT DİYE İNME ANI: SIFIR HATA İLE SES ÇALDIRMA ===
            PlayCrusherSoundByPlayerDistance();

            // 1. İNİŞ (SMASH)
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, dropSpeed * Time.deltaTime);
                yield return null;
            }

            // 2. YERDE BEKLEME
            yield return new WaitForSeconds(downWaitTime);

            // 3. KALKIŞ (RESET)
            while (Vector3.Distance(transform.position, startPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPos, riseSpeed * Time.deltaTime);
                yield return null;
            }

            // 4. TAVANDA BEKLEME
            yield return new WaitForSeconds(upWaitTime);
        }
    }

    private void PlayCrusherSoundByPlayerDistance()
    {
        if (AudioManager.Instance == null || AudioManager.Instance.crusherSound == null) return;

        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            // Eğer oyuncu belirlenen menzildeyse sesi mesafeye göre pürüzsüzce ayarla
            if (distance <= maxAudioDistance)
            {
                float baseVolume = AudioManager.Instance.crusherVolume;

                // DOYGUN SES EĞRİSİ: Yakındayken ses hemen %100 olur, uzaklaşınca yavaşça düşer kanka!
                float linearProximity = 1f - (distance / maxAudioDistance);
                float boostedProximity = Mathf.Sqrt(linearProximity); // Yakınlarda ses artık çok daha gür!

                float finalVolume = Mathf.Clamp01(boostedProximity * baseVolume);

                if (finalVolume > 0.001f)
                {
                    AudioManager.Instance.PlaySound(AudioManager.Instance.crusherSound, transform.position, finalVolume);
                }
            }
        }
    }

    // ==============================================================================
    // === ÖLÜM KONTROLÜ (SADECE TUZAĞIN BİZZAT ALTINDAKİ FİZİKSEL TEMAS) ===
    // ==============================================================================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            if (other.TryGetComponent(out DonMovement don)) don.TakeDamage(999f);
            else if (other.TryGetComponent(out SanchoMovement sancho)) sancho.TakeDamage(999f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxAudioDistance);
    }
}