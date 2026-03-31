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

    private Vector3 startPos;
    private Vector3 targetPos;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + (Vector3.down * dropDistance);

        // Coroutine ile döngüyü başlatıyoruz
        StartCoroutine(CrusherRoutine());
    }

    IEnumerator CrusherRoutine()
    {
        // 1. BAŞLANGIÇ GECİKMESİ (Fabrika sırası için)
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
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

    // TETİKLEYİCİ (Şimdilik sadece konsola yazar, hata vermez)
    private void OnTriggerEnter(Collider other)
    {
        // Konsolda her türlü teması görelim (Tag'den bağımsız)
        Debug.Log("<color=yellow>🔍 Temas Algılandı:</color> " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            // Karakterin ismini ve o anki konumunu basıyoruz
            Debug.Log("<color=red>🚨 ÖLÜM GERÇEKLEŞTİ!</color> Çarpan Obje: " + other.gameObject.name);

            // Eğer DonMovement veya SanchoMovement varsa onları da teyit edelim
            if (other.TryGetComponent(out DonMovement don))
            {
                Debug.Log("<color=cyan>👤 Don Quixote ezildi.</color>");
                // don.TakeDamage(100f); // Şimdilik kapalı kalabilir, sadece log yeterli
            }
            else if (other.TryGetComponent(out SanchoMovement sancho))
            {
                Debug.Log("<color=cyan>👤 Sancho ezildi.</color>");
                // sancho.TakeDamage(100f); // Şimdilik kapalı kalabilir
            }
        }
    }
}