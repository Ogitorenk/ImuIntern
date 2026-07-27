using UnityEngine;
using System.Collections;
using Cinemachine;
using UnityEngine.SceneManagement; // Sahne geçişi için eklendi

public class ProgressionTrigger : MonoBehaviour
{
    [SerializeField] private GameProgressData progressionData;

    [Header("Cinemachine Ayarları (Kamera Sallama)")]
    [SerializeField] private GameObject activePlayerCamera; 
    [SerializeField] private float shakeDuration = 1.0f;
    [SerializeField] private float shakeAmplitude = 3.0f; 
    [SerializeField] private float shakeFrequency = 2.0f; 

    [Header("İşitsel Feedback (Ses Efekti)")]
    [SerializeField] private AudioClip gateOpenSound;
    [SerializeField] private AudioSource audioSource;

    [Header("--- OTOMATİK IŞINLANMA AYARLARI ---")]
    [Tooltip("Bu sahnedeki şalter çekilince otomatik ışınlanma yapılsın mı?")]
    [SerializeField] private bool autoTeleportAfterLever = false;
    [Tooltip("Geri dönülecek sahnenin adı")]
    [SerializeField] private string targetSceneName = "Right_Section";
    [Tooltip("Şalter çekildikten kaç saniye sonra ışınlanma gerçekleşsin? (Efektlerin bitmesi için)")]
    [SerializeField] private float teleportDelay = 2.0f;

    [Header("Spesifik Doğma Ayarı (Opsiyonel)")]
    [SerializeField] private bool ozelKoordinataIsinla = false;
    [Tooltip("Hedef sahnede doğulacak X, Y, Z koordinatları.")]
    [SerializeField] private Vector3 hedefKoordinat;

    private CinemachineBasicMultiChannelPerlin cvcNoise;
    private bool isTeleporting = false;

    void Start()
    {
        if (activePlayerCamera != null)
        {
            var vCam = activePlayerCamera.GetComponent<CinemachineVirtualCamera>();
            if (vCam != null)
            {
                cvcNoise = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }
            else
            {
                var freeLook = activePlayerCamera.GetComponent<CinemachineFreeLook>();
                if (freeLook != null)
                {
                    cvcNoise = freeLook.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                }
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    // --- SOL BÖLÜM ---
    public void SetFirstGateOpen()
    {
        if (progressionData != null)
        {
            progressionData.isFirstIronGateOpen = true;
            progressionData.SaveToDisk(); 
            TriggerFeedback();
            TryAutoTeleport();
        }
    }

    // --- SAĞ BÖLÜM ŞALTERLERİ ---

    // 1. Up Forest Şalteri
    public void PullUpForestLever()
    {
        if (progressionData != null && !progressionData.isUpForestLeverPulled)
        {
            progressionData.isUpForestLeverPulled = true;
            progressionData.SaveToDisk();
            Debug.Log("<color=yellow>🌲 Up Forest Şalteri İndirildi!</color>");
            
            TriggerFeedback();
            CheckRightSectionLevers();
            TryAutoTeleport();
        }
    }

    // 2. Maze Şalteri
    public void PullMazeLever()
    {
        if (progressionData != null && !progressionData.isMazeLeverPulled)
        {
            progressionData.isMazeLeverPulled = true;
            progressionData.SaveToDisk();
            Debug.Log("<color=yellow>🌀 Maze Şalteri İndirildi!</color>");
            
            TriggerFeedback();
            CheckRightSectionLevers();
            TryAutoTeleport();
        }
    }

    // 3. Pit Şalteri
    public void PullPitLever()
    {
        if (progressionData != null && !progressionData.isPitLeverPulled)
        {
            progressionData.isPitLeverPulled = true;
            progressionData.SaveToDisk();
            Debug.Log("<color=yellow>🕳️ Pit Şalteri İndirildi!</color>");
            
            TriggerFeedback();
            CheckRightSectionLevers();
            TryAutoTeleport();
        }
    }

    // Ortak Kontrol Mekanizması
    private void CheckRightSectionLevers()
    {
        if (progressionData.isUpForestLeverPulled && 
            progressionData.isMazeLeverPulled && 
            progressionData.isPitLeverPulled && 
            !progressionData.isSecondIronGateOpen)
        {
            progressionData.isSecondIronGateOpen = true;
            progressionData.SaveToDisk();
            
            Debug.Log("<color=green>🔑 MÜKEMMEL! Sağ bölümdeki tüm şalterler indirildi. İkinci Demir Kapı Açıldı!</color>");
        }
    }

    // Geri bildirimler (Ses + Sarsıntı)
    private void TriggerFeedback()
    {
        if (audioSource != null && gateOpenSound != null)
        {
            audioSource.PlayOneShot(gateOpenSound);
        }

        if (cvcNoise != null)
        {
            StartCoroutine(ShakeCameraRoutine());
        }
    }

    private IEnumerator ShakeCameraRoutine()
    {
        cvcNoise.m_AmplitudeGain = shakeAmplitude;
        cvcNoise.m_FrequencyGain = shakeFrequency;

        yield return new WaitForSeconds(shakeDuration);

        cvcNoise.m_AmplitudeGain = 0f;
        cvcNoise.m_FrequencyGain = 0f;
    }

    // --- OTOMATİK IŞINLANMA KONTROLÜ VE ZAMANLAYICI ---
    private void TryAutoTeleport()
    {
        if (autoTeleportAfterLever && !isTeleporting)
        {
            StartCoroutine(AutoTeleportRoutine());
        }
    }

    private IEnumerator AutoTeleportRoutine()
    {
        isTeleporting = true;
        
        Debug.Log($"<color=cyan>⏳ Şalter çekildi. {teleportDelay} saniye sonra {targetSceneName} sahnesine ışınlanılıyor...</color>");

        // Belirlenen süre kadar bekle (Ekran sarsıntısı ve ses efekti oynarken oyuncu hissiyatı alsın)
        yield return new WaitForSeconds(teleportDelay);

        // Eğer SceneChanger'daki gibi özel doğma koordinatı kullanılıyorsa statik hafızaya aktar
        if (ozelKoordinataIsinla)
        {
            SceneChanger.ozelIsinlanmaAktif = true;
            SceneChanger.transferKoordinat = hedefKoordinat;
        }

        // LoadingManager var ise onunla, yoksa doğrudan sahne geçişi yap
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}