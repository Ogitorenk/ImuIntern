using UnityEngine;
using System.Collections;
using Cinemachine;

public class ProgressionTrigger : MonoBehaviour
{
    [SerializeField] private GameProgressData progressionData;

    [Header("Cinemachine Ayarları (Kamera Sallama)")]
    [Tooltip("Oyuncuyu takip eden aktif kameranı (Main_NormalCamera) buraya sürükle")]
    [SerializeField] private GameObject activePlayerCamera; // Artık GameObject alıyoruz, böylece her şeyi sürükleyebilirsin!
    
    [Tooltip("Sallantı ne kadar sürecek?")]
    [SerializeField] private float shakeDuration = 1.0f;
    
    [Tooltip("Sallantının şiddeti (Büyüklüğü)")]
    [SerializeField] private float shakeAmplitude = 3.0f; 
    
    [Tooltip("Sallantının hızı (Titreşim sıklığı)")]
    [SerializeField] private float shakeFrequency = 2.0f; 

    [Header("İşitsel Feedback (Ses Efekti)")]
    [Tooltip("Kapının uzaktan açılma/deprem ses efekti")]
    [SerializeField] private AudioClip gateOpenSound;
    [SerializeField] private AudioSource audioSource;

    private CinemachineBasicMultiChannelPerlin cvcNoise;

    void Start()
    {
        if (activePlayerCamera != null)
        {
            // Önce bu bir normal Virtual Camera mı diye bakıyoruz
            var vCam = activePlayerCamera.GetComponent<CinemachineVirtualCamera>();
            if (vCam != null)
            {
                cvcNoise = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }
            else
            {
                // Değilse, bu bir FreeLook kamerası mıdır diye bakıyoruz
                var freeLook = activePlayerCamera.GetComponent<CinemachineFreeLook>();
                if (freeLook != null)
                {
                    // FreeLook kameralarda kamera 3 çembere ayrılır. Titreşim genelde orta yörüngeye (Rig 1) eklenir.
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

    public void SetFirstGateOpen()
    {
        if (progressionData != null)
        {
            progressionData.isFirstIronGateOpen = true;
            progressionData.SaveToDisk(); 
            
            Debug.Log("<color=green>💾 İlerleme Kaydedildi: İlk Demir Kapı Açık!</color>");
            
            if (audioSource != null && gateOpenSound != null)
            {
                audioSource.PlayOneShot(gateOpenSound);
            }

            if (cvcNoise != null)
            {
                StartCoroutine(ShakeCameraRoutine());
            }
            else
            {
                Debug.LogWarning("⚠️ Kamerada 'Noise' modülü bulunamadı! Ayarları kontrol et.");
            }
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