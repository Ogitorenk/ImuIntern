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
        
        // --- YENİ EKLENDİ: STAMINA UI ---
        [Tooltip("Aktif üstteki dolu stamina barı (Filled Image)")]
        public Image activeStaminaImage;

        [Tooltip("Seçili silahın görüneceği slot")]
        public Image weaponSlotImage;

        // --- İKSİR UI ELEMENTLERİ ---
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

    [Header("--- BOSS UI ELEMENTS ---")]
    [SerializeField] private GameObject bossHUDGroup; // Boss can barının içinde bulunduğu UI Paneli (aktif/pasif yapmak için)
    [SerializeField] private Image bossHealthBarImage; // Boss'un 10 karelik can barı (Filled Image)
    [SerializeField] private Text bossNameText; // İstersen boss'un adını yazdıracağın Text bileşeni
    [SerializeField] private float bossTotalBoxes = 10f; // Can barının kaç bölmeli/kareli olduğu (Seninki 10 kare)

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
        UpdateBar(donQuixoteUI.activeHealthImage, currentHealth, maxHealth, hudSettings.snapToHearts);
    }

    // --- YENİ: DON KİŞOT STAMINA GÜNCELLEME ---
    public void UpdateDonQuixoteStamina(float currentStamina, float maxStamina)
    {
        // Staminada kalpli snap özelliğine gerek olmadığı için false gönderiyoruz kanka
        UpdateBar(donQuixoteUI.activeStaminaImage, currentStamina, maxStamina, false);
    }

    public void ChangeDonQuixoteWeapon(bool isWeaponShield)
    {
        if (donQuixoteUI.weaponSlotImage == null) return;
        donQuixoteUI.weaponSlotImage.sprite = isWeaponShield ? donWeaponSprites.shieldSprite : donWeaponSprites.spearSprite;
    }

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
        UpdateBar(sanchoUI.activeHealthImage, currentHealth, maxHealth, hudSettings.snapToHearts);
    }

    // --- YENİ: SANCHO STAMINA GÜNCELLEME ---
    public void UpdateSanchoStamina(float currentStamina, float maxStamina)
    {
        UpdateBar(sanchoUI.activeStaminaImage, currentStamina, maxStamina, false);
    }

    public void UpdateSanchoPotions(int healthCount, int slowCount)
    {
        if (sanchoUI.healthPotionText != null)
            sanchoUI.healthPotionText.text = healthCount.ToString();

        if (sanchoUI.slowPotionText != null)
            sanchoUI.slowPotionText.text = slowCount.ToString();
    }

    // --- ORTAK YARDIMCI METOT (İsim can barından daha genel bir isme çevrildi kanka) ---

    private void UpdateBar(Image barImage, float current, float max, bool useSnap)
    {
        if (barImage == null || max <= 0) return;
        float ratio = Mathf.Clamp01(current / max);

        if (useSnap && hudSettings.snapToHearts && hudSettings.totalBoxes > 0)
        {
            ratio = Mathf.Round(ratio * hudSettings.totalBoxes) / hudSettings.totalBoxes;
        }

        barImage.fillAmount = ratio;
    }

    public void ToggleBossUI(bool isActive, string bossName = "Dev Slime / Fare")
{
    if (bossHUDGroup != null) bossHUDGroup.SetActive(isActive);
    if (bossNameText != null) bossNameText.text = bossName;
}

// Boss Canını Güncelle (Tıpkı karakterlerdeki gibi adımlı/snap)
public void UpdateBossHealth(float currentHealth, float maxHealth)
{
    if (bossHealthBarImage == null || maxHealth <= 0) return;
    
    // Can oranını hesapla
    float ratio = Mathf.Clamp01(currentHealth / maxHealth);
    
    // 10 kareye snaple kanka (Örn: 0.82 ise 0.8'e yuvarlar, tam kareler silinir)
    if (bossTotalBoxes > 0)
    {
        ratio = Mathf.Round(ratio * bossTotalBoxes) / bossTotalBoxes;
    }
    
    bossHealthBarImage.fillAmount = ratio;
}
}