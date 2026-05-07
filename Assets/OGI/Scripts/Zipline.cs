using UnityEngine;
using System.Collections;

public class ZiplinePrefab : MonoBehaviour
{
    [Header("Referanslar (Child Objerler)")]
    public Transform startPoint;
    public Transform endPoint;
    public Transform visualRope;

    [Header("Ayarlar")]
    public float zipSpeed = 12f;
    public float playerOffset = -2.2f; // İpin ne kadar altında asılacak?
    public KeyCode interactKey = KeyCode.F;

    // --- YENİ EKLENDİ: GLOBAL ZİPLİNE ŞALTERİ ---
    // Bu şalter static olduğu için oyunun her yerinden (karakter değiştirme scriptinden bile) okunabilir!
    public static bool isAnyPlayerZiplining = false;

    private bool playerInRange = false;
    private GameObject currentPlayer;
    private bool isZipping = false;

    void Start()
    {
        UpdateRopeVisual();
    }

    // Editor'de noktaları hareket ettirdiğinde ipin otomatik güncellenmesi için
    void OnValidate()
    {
        if (startPoint != null && endPoint != null && visualRope != null)
            UpdateRopeVisual();
    }

    void Update()
    {
        if (playerInRange && !isZipping && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(ZipRoutine());
        }
    }

    void UpdateRopeVisual()
    {
        // İpi tam iki noktanın ortasına koy
        visualRope.position = (startPoint.position + endPoint.position) / 2f;

        // Silindir boylamasına Y eksenindedir. LookAt (Z'yi çevirir) yerine objenin Y (up) eksenini yatırıyoruz.
        visualRope.up = (endPoint.position - startPoint.position).normalized;

        // İpin uzunluğunu iki nokta arasındaki mesafeye göre ayarla (Default boy 2 olduğu için 2'ye bölüyoruz)
        float dist = Vector3.Distance(startPoint.position, endPoint.position);
        visualRope.localScale = new Vector3(0.1f, dist / 2f, 0.1f);
    }

    IEnumerator ZipRoutine()
    {
        isZipping = true;

        // --- YENİ EKLENDİ: Zipline başladı, karakter değiştirmeyi kilitle! ---
        isAnyPlayerZiplining = true;

        // Karakter scriptlerini al
        CharacterController cc = currentPlayer.GetComponent<CharacterController>();
        DonMovement don = currentPlayer.GetComponent<DonMovement>();
        SanchoMovement sancho = currentPlayer.GetComponent<SanchoMovement>();

        // CharacterController'ı kapatıyoruz ki Transform ile pürüzsüz kaydıralım
        if (cc != null) cc.enabled = false;

        // Movement scriptlerini KAPATMIYORUZ (Animatör çalışsın diye). Sadece şalteri açıyoruz.
        if (don != null) don.isZiplining = true;
        if (sancho != null) sancho.isZiplining = true;

        float distance = Vector3.Distance(startPoint.position, endPoint.position);
        float t = 0;

        while (t < 1f)
        {
            // Eğer kayarken zıplarsa teli bırak
            if (Input.GetButtonDown("Jump"))
            {
                break; // Döngüyü kır, aşağıya düşüşe geç
            }

            t += (zipSpeed / distance) * Time.deltaTime;

            Vector3 targetPos = Vector3.Lerp(startPoint.position, endPoint.position, t);
            targetPos.y += playerOffset; // İpin altında asılı kalma payı

            currentPlayer.transform.position = targetPos;

            yield return null;
        }

        if (don != null) don.isZiplining = false;
        if (sancho != null) sancho.isZiplining = false;

        // Fiziği geri aç ki yere basabilsin
        if (cc != null) cc.enabled = true;

        // --- YENİ EKLENDİ: Zipline bitti, karakter değiştirmeyi serbest bırak! ---
        isAnyPlayerZiplining = false;
        isZipping = false;
        Debug.Log("<color=cyan>🚠 Zipline başarıyla tamamlandı!</color>");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            currentPlayer = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (!isZipping) currentPlayer = null;
        }
    }
}