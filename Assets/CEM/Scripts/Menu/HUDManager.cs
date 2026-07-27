using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // --- TextMeshPro kütüphanesi eklendi kanka ---

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [System.Serializable]
    public struct CharacterHUDComponents
    {
        [Tooltip("Aktif can kutuları (Filled Image)")]
        public Image activeHealthImage;
        
        // --- STAMINA UI ---
        [Tooltip("Aktif üstteki dolu stamina barı (Filled Image)")]
        public Image activeStaminaImage;

        [Tooltip("Seçili silahın görüneceği slot")]
        public Image weaponSlotImage;

        // --- İKSİR UI ELEMENTLERİ ---
        [Tooltip("Can İksiri sayısını gösterecek Text component'i")]
        public TextMeshProUGUI healthPotionText; // TextMeshProUGUI yapıldı
        [Tooltip("Zaman İksiri sayısını gösterecek Text component'i")]
        public TextMeshProUGUI slowPotionText; // TextMeshProUGUI yapıldı
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
    [SerializeField] private TextMeshProUGUI bossNameText; // TextMeshProUGUI yapıldı
    [SerializeField] private float bossTotalBoxes = 10f; // Can barının kaç bölmeli/kareli olduğu (Seninki 10 kare)

    [Header("--- NOTIFICATION / WARNING UI ---")]
    [SerializeField] private GameObject warningPanel; // Ekrana gelecek küçük uyarı kutusu/paneli
    [SerializeField] private TextMeshProUGUI warningText; // TextMeshProUGUI yapıldı kanka (artık sürükleyebilirsin!)
    [SerializeField] private float displayDuration = 2.0f; // Ekranda kaç saniye kalacağı
    private Coroutine warningCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (warningPanel != null) warningPanel.SetActive(false);
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

    public void UpdateDonQuixoteStamina(float currentStamina, float maxStamina)
    {
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

    // --- ORTAK YARDIMCI METOT ---

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

    public void UpdateBossHealth(float currentHealth, float maxHealth)
    {
        if (bossHealthBarImage == null || maxHealth <= 0) return;
        
        float ratio = Mathf.Clamp01(currentHealth / maxHealth);
        
        if (bossTotalBoxes > 0)
        {
            ratio = Mathf.Round(ratio * bossTotalBoxes) / bossTotalBoxes;
        }
        
        bossHealthBarImage.fillAmount = ratio;
    }

    // --- NOTIFICATION / WARNING SYSTEM ---

    public void ShowWarning(string message)
    {
        if (warningPanel == null || warningText == null) return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(ShowWarningRoutine(message));
    }

    private IEnumerator ShowWarningRoutine(string message)
    {
        warningText.text = message;
        warningPanel.SetActive(true);

        yield return new WaitForSeconds(displayDuration);

        warningPanel.SetActive(false);
        warningCoroutine = null;
    }
}