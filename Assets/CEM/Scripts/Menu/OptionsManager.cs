using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle; // Panelindeki Toggle'ı buraya bağla

    private Resolution[] resolutions;

    void Start()
    {
        // 1. SES AYARINI YÜKLE
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        masterVolumeSlider.value = savedVolume;
        SetVolume(savedVolume);
        masterVolumeSlider.onValueChanged.AddListener(SetVolume);

        // 2. EKRAN MODUNU YÜKLE
        int savedFullscreen = PlayerPrefs.GetInt("Fullscreen", 1); // 1 = Fullscreen, 0 = Windowed
        bool isFS = (savedFullscreen == 1);
        fullscreenToggle.isOn = isFS;
        SetFullscreen(isFS);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        // 3. ÇÖZÜNÜRLÜK LİSTESİNİ HAZIRLA VE YÜKLE
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        
        int currentResolutionIndex = 0;
        int savedResWidth = PlayerPrefs.GetInt("ResWidth", Screen.currentResolution.width);
        int savedResHeight = PlayerPrefs.GetInt("ResHeight", Screen.currentResolution.height);

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // Eğer listenin bu elemanı, kaydedilen çözünürlüğe eşitse indeksini tut
            if (resolutions[i].width == savedResWidth && resolutions[i].height == savedResHeight)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        
        // Dropdown değiştiğinde fonksiyonu tetikle
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    // Ses Değişimi ve Kaydı
    public void SetVolume(float volume)
    {
        // Mixer'da "Master" parametresini kontrol eder (Logaritmik ses ayarı)
        audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    // Ekran Modu Değişimi ve Kaydı
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    // Çözünürlük Değişimi ve Kaydı
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        // Genişlik ve yüksekliği ayrı ayrı kaydediyoruz
        PlayerPrefs.SetInt("ResWidth", resolution.width);
        PlayerPrefs.SetInt("ResHeight", resolution.height);
    }
}