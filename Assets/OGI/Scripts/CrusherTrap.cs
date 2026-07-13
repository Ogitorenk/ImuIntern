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

    [Header("Oyuncu Tabanlı Kodsal Ses Ayarları (Yeni)")]
    [Tooltip("Sesin duyulmaya başlayacağı maksimum mesafe kanka.")]
    public float maxAudioDistance = 15f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private AudioSource myAudioSource;
    private Transform playerTransform; // Sahnede aktif olan oyuncunun transformu kanka

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + (Vector3.down * dropDistance);

        // Ses cihazını tuzağın üzerine kuruyoruz kanka
        SetupAudioSource();

        // Coroutine ile döngüyü başlatıyoruz
        StartCoroutine(CrusherRoutine());
    }

    private void SetupAudioSource()
    {
        myAudioSource = GetComponent<AudioSource>();
        if (myAudioSource == null)
        {
            myAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (AudioManager.Instance != null && AudioManager.Instance.crusherSound != null)
        {
            myAudioSource.clip = AudioManager.Instance.crusherSound;
        }

        // Kanka madem sesi tamamen kodla yöneteceğiz, Unity'nin kendi iç sönümlemesini 
        // devre dışı bırakmak için spatialBlend'i 0.5f yapıyoruz. %50 yön hissi kalırken ses asla kesilmez!
        myAudioSource.spatialBlend = 0.5f;
        myAudioSource.playOnAwake = false;
        myAudioSource.loop = false;
    }

    void Update()
    {
        // === TAM İSTEDİĞİN DINAMIK MESAFE FORMÜLÜ KANKA ===
        // Eğer sahnede oyuncu transformu henüz bulunmadıysa veya karakter değiştiyse Player tag'li objeyi bul kanka
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        // Oyuncu sahnede aktifse tuzağa olan mesafesine göre sesi anlık güncelle kanka küt diye çözülsün
        if (playerTransform != null && myAudioSource != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance <= maxAudioDistance)
            {
                // İstediğin o matematiksel formül kanka: Mesafe 0'a yaklaştıkça volume 1'e yaklaşır!
                float customVolume = 1f - (distance / maxAudioDistance);
                myAudioSource.volume = Mathf.Clamp(customVolume, 0f, 1f);
            }
            else
            {
                // Menzilin dışındaysa ses şiddetini zınk diye sıfırla, haritanın ucuna taşmasın kanka
                myAudioSource.volume = 0f;
            }
        }
    }

    IEnumerator CrusherRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // === SES TETİKLENME NOKTASI ===
            // Update fonksiyonu volume değerini o salisede milimetrik ayarladığı için burası sadece Play diyecek kanka
            if (myAudioSource != null && myAudioSource.clip != null && myAudioSource.volume > 0.05f)
            {
                myAudioSource.Play();
            }

            // 2. İNİŞ (SMASH)
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, dropSpeed * Time.deltaTime);
                yield return null;
            }

            // 3. YERDE BEKLEME
            yield return new WaitForSeconds(downWaitTime);

            // 4. KALKIŞ (RESET)
            while (Vector3.Distance(transform.position, startPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPos, riseSpeed * Time.deltaTime);
                yield return null;
            }

            // 5. TAVANDA BEKLEME
            yield return new WaitForSeconds(upWaitTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
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