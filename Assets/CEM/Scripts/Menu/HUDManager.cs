using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [System.Serializable]
    public struct CharacterHUDComponents
    {
        [Tooltip("Aktif can kutuları (Filled Image)")]
        public Image activeHealthImage;
        [Tooltip("Seçili silahın görüneceği slot")]
        public Image weaponSlotImage;

        // --- YENİ EKLENDİ: İKSİR UI ELEMENTLERİ ---
        [Tooltip("Can İksiri sayısını gösterecek Text component'i")]
        public Text healthPotionText;
        [Tooltip("Zaman İksiri sayısını gösterecek Text component'i")]
        public Text slowPotionText;
    }

    [System.Serializable]
    public struct WeaponSprites
    {
        public Sprite shieldSprite;
        public Sprite spearSprite;
    }

    [System.Serializable]
    public struct HUDSettings
    {
        public bool snapToHearts;
        public float totalBoxes;
    }

    [Header("--- HUD GROUPS ---")]
    [SerializeField] private GameObject donHUDGroup;
    [SerializeField] private GameObject sanchoHUDGroup;

    [Header("--- DON QUIXOTE ELEMENTS ---")]
    [SerializeField] private CharacterHUDComponents donQuixoteUI;
    [SerializeField] private WeaponSprites donWeaponSprites;

    [Header("--- SANCHO PANZA ELEMENTS ---")]
    [SerializeField] private CharacterHUDComponents sanchoUI;

    [Header("--- CONFIGURATION ---")]
    [SerializeField] private HUDSettings hudSettings = new HUDSettings { snapToHearts = true, totalBoxes = 13f };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- KARAKTER GÜÇ/HUD YÖNETİMİ ---

    public void SwitchHUD(bool isDonActive)
    {
        if (donHUDGroup != null) donHUDGroup.SetActive(isDonActive);
        if (sanchoHUDGroup != null) sanchoHUDGroup.SetActive(!isDonActive);
    }

    // --- DON KİŞOT İÇİN FONKSİYONLAR ---

    public void UpdateDonQuixoteHealth(float currentHealth, float maxHealth)
    {
        UpdateHealthBar(donQuixoteUI.activeHealthImage, currentHealth, maxHealth);
    }

    public void ChangeDonQuixoteWeapon(bool isWeaponShield)
    {
        if (donQuixoteUI.weaponSlotImage == null) return;
        donQuixoteUI.weaponSlotImage.sprite = isWeaponShield ? donWeaponSprites.shieldSprite : donWeaponSprites.spearSprite;
    }

    // --- YENİ FONKSİYONLAR: DON İKSİR SAYILARINI GÜNCELLEME ---
    public void UpdateDonQuixotePotions(int healthCount, int slowCount)
    {
        if (donQuixoteUI.healthPotionText != null)
            donQuixoteUI.healthPotionText.text = healthCount.ToString();

        if (donQuixoteUI.slowPotionText != null)
            donQuixoteUI.slowPotionText.text = slowCount.ToString();
    }

    // --- SANCHO İÇİN FONKSİYONLAR ---

    public void UpdateSanchoHealth(float currentHealth, float maxHealth)
    {
        UpdateHealthBar(sanchoUI.activeHealthImage, currentHealth, maxHealth);
    }

    // --- YENİ FONKSİYONLAR: SANCHO İKSİR SAYILARINI GÜNCELLEME ---
    // (Sancho'nun iksir mantığı da Don gibiyse bunu tetiklersin kanka)
    public void UpdateSanchoPotions(int healthCount, int slowCount)
    {
        if (sanchoUI.healthPotionText != null)
            sanchoUI.healthPotionText.text = healthCount.ToString();

        if (sanchoUI.slowPotionText != null)
            sanchoUI.slowPotionText.text = slowCount.ToString();
    }

    // --- ORTAK YARDIMCI METOT ---

    private void UpdateHealthBar(Image healthImage, float current, float max)
    {
        if (healthImage == null) return;
        float ratio = Mathf.Clamp01(current / max); // Güvence amacıyla 0-1 arasına kilitledim kanka

        if (hudSettings.snapToHearts && hudSettings.totalBoxes > 0)
        {
            ratio = Mathf.Round(ratio * hudSettings.totalBoxes) / hudSettings.totalBoxes;
        }
        healthImage.fillAmount = ratio;
    }
}