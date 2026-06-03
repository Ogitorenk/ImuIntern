using UnityEngine;
using System.Collections;

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
            // --- YENİ EKLENDİ: ZİPLİNE KONTROLÜ ---
            // Eğer oyunculardan biri zipline üzerindeyse geçişi engelle!
            if (ZiplinePrefab.isAnyPlayerZiplining)
            {
                Debug.Log("🚫 Zipline üzerindeyken karakter değiştirilemez!");
                return;
            }

            // ========================================================
            // --- GÜNCELLENDİ: EĞİLME / SÜRÜNME KONTROLÜ ---
            // ========================================================
            bool isActiveCharacterCrouching = false;

            if (isDonActive && donQuixote != null)
            {
                DonMovement don = donQuixote.GetComponent<DonMovement>();
                if (don != null && (don.isCrouchToggled || don.isCrawling))
                {
                    isActiveCharacterCrouching = true;
                }
            }
            else if (!isDonActive && sancho != null)
            {
                SanchoMovement sm = sancho.GetComponent<SanchoMovement>();
                if (sm != null && (sm.isCrouchToggled || sm.isCrawling))
                {
                    isActiveCharacterCrouching = true;
                }
            }

            if (isActiveCharacterCrouching)
            {
                Debug.Log("🚫 Karakter eğilirken, sürünürken veya ayağa kalkma beklemesindeyken değiştirilemez!");
                return; // Geçişi direkt iptal et
            }
            // ========================================================

            // --- SANCHO KUTU TUTUYOR MU KONTROLÜ ---
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

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.SwitchHUD(isDonActive);
        }

        // --- YENİ EKLENDİ: ARI / EJDERHA GERÇEKLİK DEĞİŞİM TETİKLEYİCİSİ ---
        UpdateAllFlyingEnemiesPerception();
    }

    // --- TÜM EKİBİN CANINI FULLEME (CHECKPOINT/RESPAWN İÇİN) ---
    public void ResetAllHealth()
    {
        DonMovement don = FindObjectOfType<DonMovement>(true);
        if (don != null)
        {
            don.currentHealth = don.maxHealth;
        }

        SanchoMovement sanchoScript = FindObjectOfType<SanchoMovement>(true);
        if (sanchoScript != null)
        {
            sanchoScript.currentHealth = sanchoScript.maxHealth;
        }

        Debug.Log("<color=green>💚 [SİSTEM] Sahnede gizli olan karakterler zorla bulundu ve canları 100 yapıldı!</color>");
    }

    // --- Sahnede gizli/kapalı olsa bile tüm JumpPad'leri bulur ---
    void UpdateAllJumpPads()
    {
        IllusionJumpPad[] jumpPads = FindObjectsOfType<IllusionJumpPad>(true);
        foreach (IllusionJumpPad pad in jumpPads)
        {
            pad.UpdatePerception(isDonActive);
        }
    }

    // --- Sahnede gizli/kapalı olsa bile tüm Kırılabilir Platformları bulur ---
    void UpdateAllBreakablePlatforms()
    {
        BreakableIllusionPlatform[] platforms = FindObjectsOfType<BreakableIllusionPlatform>(true);
        foreach (BreakableIllusionPlatform platform in platforms)
        {
            platform.UpdatePerception(isDonActive);
        }
    }

    // --- Sahnede gizli/kapalı olsa bile tüm İllüzyonlu Hareketli Platformları bulur ---
    void UpdateAllMovingIllusionPlatforms()
    {
        MovingIllusionPlatform[] movingPlatforms = FindObjectsOfType<MovingIllusionPlatform>(true);
        foreach (MovingIllusionPlatform platform in movingPlatforms)
        {
            platform.UpdatePerception(isDonActive);
        }
    }

    // ==============================================================================================
    // --- GÜNCELLENDİ: ARI VEYA EJDERHA MODELLERİNİN ÜST ÜSTE BİNMESİNİ ÖNLEYEN NET GEÇİŞ ---
    // ==============================================================================================
    void UpdateAllFlyingEnemiesPerception()
    {
        EnemyFlying[] flyingEnemies = FindObjectsOfType<EnemyFlying>(true);
        foreach (EnemyFlying enemy in flyingEnemies)
        {
            // Sadece animatorü değil, modelin doğrudan kendisini açıp kapatan metodu tetikliyoruz kanka!
            enemy.UpdateModelVisibility(isDonActive);
        }
    }
}