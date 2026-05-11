using UnityEngine;
using UnityEngine.Rendering.PostProcessing; // 1. BURAYI EKLEDİK: Post-processing kütüphanesi

public class DualRealityDecor : MonoBehaviour
{
    [Header("--- İllüzyon Objeleri ---")]
    [Tooltip("Don Kişot aktifken nerede ve ne şekilde görüneceğini ayarladığın obje")]
    public GameObject donView;

    [Tooltip("Sancho aktifken nerede ve ne şekilde görüneceğini ayarladığın obje")]
    public GameObject sanchoView;

    [Header("--- Renk Tonu Ayarları ---")] // 2. BURAYI EKLEDİK: Müfettiş (Inspector) için yeni alanlar
    public PostProcessVolume globalVolume; 
    public PostProcessProfile donProfile;
    public PostProcessProfile sanchoProfile;

    // Arka planda karakterin değişip değişmediğini takip eden hafıza
    private bool lastState;

    void Start()
    {
        // Oyun başlarken kim aktifse ona göre objeleri aç/kapat
        if (DualRealityManager.Instance != null)
        {
            lastState = DualRealityManager.Instance.isDonActive;
            UpdatePerception(lastState);
        }
    }

    void Update()
    {
        // Her karede karakterin değişip değişmediğini kontrol et
        // (DualRealityManager'a kod eklememek için bu taktiği kullanıyoruz)
        if (DualRealityManager.Instance != null)
        {
            bool currentState = DualRealityManager.Instance.isDonActive;

            // Eğer karakter değiştiyse (TAB tuşuna basıldıysa) durumu güncelle
            if (currentState != lastState)
            {
                lastState = currentState;
                UpdatePerception(lastState);
            }
        }
    }

    public void UpdatePerception(bool isDon)
    {
        // Don aktifse Don'un objesi açılsın (Sancho'nunki kapansın)
        if (donView != null) donView.SetActive(isDon);

        // Sancho aktifse Sancho'nun objesi açılsın (Don'unki kapansın)
        if (sanchoView != null) sanchoView.SetActive(!isDon);

        // 3. BURAYI EKLEDİK: Renk profili geçiş mantığı
        if (globalVolume != null)
        {
            if (isDon && donProfile != null)
            {
                globalVolume.profile = donProfile;
            }
            else if (!isDon && sanchoProfile != null)
            {
                globalVolume.profile = sanchoProfile;
            }
        }
    }
}