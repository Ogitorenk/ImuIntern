using UnityEngine;
using System.Collections;
using Cinemachine;

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

    private CinemachineBasicMultiChannelPerlin cvcNoise;

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

    // --- SOL BÖLÜM (Eski Şalter Fonksiyonun) ---
    public void SetFirstGateOpen()
    {
        if (progressionData != null)
        {
            progressionData.isFirstIronGateOpen = true;
            progressionData.SaveToDisk(); 
            TriggerFeedback();
        }
    }

    // --- SAĞ BÖLÜM ŞALTERLERİ ---

    // 1. Up Forest Şalteri Tetiklendiğinde Çalışacak
    public void PullUpForestLever()
    {
        if (progressionData != null && !progressionData.isUpForestLeverPulled)
        {
            progressionData.isUpForestLeverPulled = true;
            progressionData.SaveToDisk();
            Debug.Log("<color=yellow>🌲 Up Forest Şalteri İndirildi!</color>");
            CheckRightSectionLevers();
        }
    }

    // 2. Maze Şalteri Tetiklendiğinde Çalışacak
    public void PullMazeLever()
    {
        if (progressionData != null && !progressionData.isMazeLeverPulled)
        {
            progressionData.isMazeLeverPulled = true;
            progressionData.SaveToDisk();
            Debug.Log("<color=yellow>🌀 Maze Şalteri İndirildi!</color>");
            CheckRightSectionLevers();
        }
    }

    // 3. Pit Şalteri Tetiklendiğinde Çalışacak
    public void PullPitLever()
    {
        if (progressionData != null && !progressionData.isPitLeverPulled)
        {
            progressionData.isPitLeverPulled = true;
            progressionData.SaveToDisk();
            Debug.Log("<color=yellow>🕳️ Pit Şalteri İndirildi!</color>");
            CheckRightSectionLevers();
        }
    }

    // Ortak Kontrol Mekanizması
    private void CheckRightSectionLevers()
    {
        // Eğer üç şalter de indirildiyse ve ikinci kapı henüz açılmadıysa
        if (progressionData.isUpForestLeverPulled && 
            progressionData.isMazeLeverPulled && 
            progressionData.isPitLeverPulled && 
            !progressionData.isSecondIronGateOpen)
        {
            progressionData.isSecondIronGateOpen = true;
            progressionData.SaveToDisk();
            
            Debug.Log("<color=green>🔑 MÜKEMMEL! Sağ bölümdeki tüm şalterler indirildi. İkinci Demir Kapı Açıldı!</color>");
            
            // Büyük kapının açılma feedback'ini (ses ve sarsıntı) tetikle
            TriggerFeedback();
        }
    }

    // Geri bildirimleri tek çatı altında topladık
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
}