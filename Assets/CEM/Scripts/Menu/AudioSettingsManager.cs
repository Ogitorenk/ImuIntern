using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer Reference")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    private const string MASTER_PREF_KEY = "MasterVolumePref";
    private const string SFX_PREF_KEY = "SFXVolumePref";

    private void Start()
    {
        // 1. MASTER YÜKLEME VE BAĞLANTI
        float savedMaster = PlayerPrefs.GetFloat(MASTER_PREF_KEY, 0.75f);
        if (masterSlider != null)
        {
            masterSlider.value = savedMaster;
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        SetMasterVolume(savedMaster);

        // 2. SFX YÜKLEME VE BAĞLANTI
        float savedSFX = PlayerPrefs.GetFloat(SFX_PREF_KEY, 0.75f);
        if (sfxSlider != null)
        {
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        SetSFXVolume(savedSFX);
    }

    public void SetMasterVolume(float sliderValue)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, sliderValue)) * 20f;
        mainMixer.SetFloat(masterVolumeParam, dB);
        PlayerPrefs.SetFloat(MASTER_PREF_KEY, sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, sliderValue)) * 20f;
        mainMixer.SetFloat(sfxVolumeParam, dB);
        PlayerPrefs.SetFloat(SFX_PREF_KEY, sliderValue);
    }

    private void OnDestroy()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }
}