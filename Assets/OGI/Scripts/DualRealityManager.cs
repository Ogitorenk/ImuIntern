using UnityEngine;

public class DualRealityManager : MonoBehaviour
{
    public static DualRealityManager Instance;

    [Header("Karakter Prefabları")]
    public GameObject donQuixote;
    public GameObject sancho;

    [HideInInspector] public bool isDonActive = true;

    // --- YENİ EKLENDİ: SWITCH KİLİDİ ---
    [HideInInspector] public bool canSwitch = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Oyun başlarken Don'u aç, Sancho'yu kapat
        SwitchCharacter(true);
    }

    void Update()
    {
        // TAB tuşuna basıldığında
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // --- YENİ: SANCHO KUTU TUTUYOR MU KONTROLÜ ---
            bool isSanchoHoldingBox = false;
            if (!isDonActive && sancho != null)
            {
                SanchoMovement sm = sancho.GetComponent<SanchoMovement>();
                if (sm != null && sm.isHoldingBox)
                {
                    isSanchoHoldingBox = true;
                }
            }

            // Kilit açıkken VE Sancho kutu tutmuyorken karakter değiştir
            if (canSwitch && !isSanchoHoldingBox)
            {
                SwitchCharacter(!isDonActive);
            }
            else
            {
                // Kutu iterken veya canSwitch false iken basarsa konsola uyarı atsın
                Debug.Log("🚫 Şu an karakter değiştirilemez! (Geçiş kilitli veya Sancho kutu tutuyor)");
            }
        }
    }

    void SwitchCharacter(bool toDon)
    {
        isDonActive = toDon;

        GameObject activeChar = isDonActive ? donQuixote : sancho;
        GameObject inactiveChar = isDonActive ? sancho : donQuixote;

        // İnaktif karakterin pozisyonunu, aktif karaktere kopyala
        CharacterController ccActive = activeChar.GetComponent<CharacterController>();

        if (ccActive != null) ccActive.enabled = false;

        activeChar.transform.position = inactiveChar.transform.position;
        activeChar.transform.rotation = inactiveChar.transform.rotation;

        if (ccActive != null) ccActive.enabled = true;

        // Modelleri aç/kapat
        activeChar.SetActive(true);
        inactiveChar.SetActive(false);

        // --- GÜNCELLEMELER ÇAĞRILIYOR ---
        UpdateAllJumpPads();
        UpdateAllBreakablePlatforms();
        UpdateAllMovingIllusionPlatforms();
    }

    // --- YENİ EKLENDİ: TÜM EKİBİN CANINI FULLEME (CHECKPOINT/RESPAWN İÇİN) ---
    // --- YENİ EKLENDİ: KESİN ÇÖZÜMLÜ CAN FULLEME ---
    public void ResetAllHealth()
    {
        // 'true' parametresi, karakter o an inaktif (gizli) olsa bile onu bulmasını sağlar!
        DonMovement don = FindObjectOfType<DonMovement>(true);
        if (don != null)
        {
            don.currentHealth = don.maxHealth;

            // DİKKAT: Eğer ekranda bir Can Barı (UI) varsa, onu güncelleyen kodu buraya eklemelisin.
            // Örnek: don.UpdateHealthUI(); 
        }

        SanchoMovement sancho = FindObjectOfType<SanchoMovement>(true);
        if (sancho != null)
        {
            sancho.currentHealth = sancho.maxHealth;

            // DİKKAT: Eğer ekranda bir Can Barı (UI) varsa, onu güncelleyen kodu buraya eklemelisin.
            // Örnek: sancho.UpdateHealthUI();
        }

        Debug.Log("<color=green>💚 [SİSTEM] Sahnede gizli olan karakterler zorla bulundu ve canları 100 yapıldı!</color>");
    }

    // --- YENİ EKLENDİ (true): Sahnede gizli/kapalı olsa bile tüm JumpPad'leri bulur ---
    void UpdateAllJumpPads()
    {
        IllusionJumpPad[] jumpPads = FindObjectsOfType<IllusionJumpPad>(true);
        foreach (IllusionJumpPad pad in jumpPads)
        {
            pad.UpdatePerception(isDonActive);
        }
    }

    // --- YENİ EKLENDİ (true): Sahnede gizli/kapalı olsa bile tüm Kırılabilir Platformları bulur ---
    void UpdateAllBreakablePlatforms()
    {
        BreakableIllusionPlatform[] platforms = FindObjectsOfType<BreakableIllusionPlatform>(true);
        foreach (BreakableIllusionPlatform platform in platforms)
        {
            platform.UpdatePerception(isDonActive);
        }
    }

    // --- YENİ EKLENDİ (true): Sahnede gizli/kapalı olsa bile tüm İllüzyonlu Hareketli Platformları bulur ---
    void UpdateAllMovingIllusionPlatforms()
    {
        MovingIllusionPlatform[] movingPlatforms = FindObjectsOfType<MovingIllusionPlatform>(true);
        foreach (MovingIllusionPlatform platform in movingPlatforms)
        {
            platform.UpdatePerception(isDonActive);
        }
    }
}