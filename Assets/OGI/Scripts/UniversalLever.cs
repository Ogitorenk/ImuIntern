using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class UniversalLever : MonoBehaviour
{
    [Header("Ayarlar")]
    public KeyCode interactionKey = KeyCode.F;
    public UnityEvent onActivate;   // Kapıyı açacak event
    public UnityEvent onDeactivate; // Kapıyı kapatacak event

    [Header("Zaman Ayarı")]
    [Tooltip("Şalter otomatik olarak eski haline dönsün mü? (Tiki kaldırırsan Aç-Kapa (Toggle) mantığıyla çalışır)")]
    public bool autoReset = true;

    [Tooltip("Şalter çekildikten kaç saniye sonra eski haline dönsün? (Sadece Auto Reset açıksa çalışır)")]
    public float resetTime = 10f;

    [Header("Görsel Geri Bildirim")]
    public Transform leverHandle;
    public Vector3 pulledRotation = new Vector3(0, 0, -45f); // Z ekseninde -45

    private bool isPulled = false;
    private bool playerInRange = false;
    private Quaternion originalRotation;
    private bool isAnimating = false; // Animasyon oynarken spamlamayı engellemek için

    void Start()
    {
        if (leverHandle != null)
        {
            originalRotation = leverHandle.localRotation;
        }
    }

    void Update()
    {
        // === KESİN ÇÖZÜM: ÖNCE F TUŞUNU SORGULA ===
        // Oyuncu F tuşuna basmadıysa alt satırlara hiç bakma, sesi ve mantığı tamamen engelle kanka!
        if (Input.GetKeyDown(interactionKey))
        {
            // F'ye basıldıktan sonra oyuncu menzildeyse ve animasyon kilitli değilse çalıştır kanka:
            if (playerInRange && !isAnimating)
            {
                if (!isPulled)
                {
                    Pull(); // Çekili değilse ÇEK
                }
                else if (!autoReset)
                {
                    PushBack(); // Çekiliyse ve otomatik kapanma yoksa MANUEL KAPAT
                }
            }
        }
    }

    private void Pull()
    {
        isPulled = true;

        if (leverHandle != null)
        {
            leverHandle.localRotation = Quaternion.Euler(pulledRotation);
        }

        // --- SES ENTEGRASYONU (Şalter Çekilme Sesi) ---
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(AudioManager.Instance.leverSound, transform.position);
        }

        if (onActivate != null) onActivate.Invoke();

        if (autoReset)
        {
            Debug.Log("<color=orange>🕹️ Şalter F ile çekildi! Geri sayım başladı...</color>");
            StartCoroutine(ResetRoutine());
        }
        else
        {
            Debug.Log("<color=orange>🕹️ Şalter F ile çekildi! (Kapatmak için tekrar F'ye bas)</color>");
        }
    }

    // --- MANUEL KAPATMA FONKSİYONU ---
    private void PushBack()
    {
        isPulled = false;

        // --- SES ENTEGRASYONU (Manuel Kapatma Sesi) ---
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(AudioManager.Instance.leverSound, transform.position);
        }

        if (onDeactivate != null) onDeactivate.Invoke();

        Debug.Log("<color=cyan>🔄 Şalter manuel olarak kapatıldı!</color>");

        // Kapanma animasyonunu başlat
        StartCoroutine(CloseAnimationRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(resetTime);

        // --- SES ENTEGRASYONU (Otomatik Süre Bitip Kapanma Sesi) ---
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(AudioManager.Instance.leverSound, transform.position);
        }

        if (onDeactivate != null) onDeactivate.Invoke();

        isPulled = false;
        Debug.Log("<color=cyan>🔄 Süre bitti! Şalter otomatik kapandı.</color>");

        // Süre bitince kapanma animasyonunu başlat
        yield return StartCoroutine(CloseAnimationRoutine());
    }

    // --- KAPANMA ANİMASYONU (Ortak Kullanım İçin Ayrıldı) ---
    private IEnumerator CloseAnimationRoutine()
    {
        isAnimating = true; // Kapanırken tekrar basmayı engelle

        if (leverHandle != null)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Quaternion startRot = leverHandle.localRotation;

            while (elapsed < duration)
            {
                leverHandle.localRotation = Quaternion.Slerp(startRot, originalRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            leverHandle.localRotation = originalRotation;
        }

        isAnimating = false; // Animasyon bitti, tekrar etkileşime girilebilir
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}