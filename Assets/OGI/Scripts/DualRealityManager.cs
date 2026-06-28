using UnityEngine;
using System.Collections;
using UnityEngine.UI; // --- YENİ EKLENDİ: UI Image kontrolü için ---

public class DualRealityManager : MonoBehaviour
{
    public static DualRealityManager Instance;

    [Header("Karakter Prefabları")]
    public GameObject donQuixote;
    public GameObject sancho;

    [HideInInspector] public bool isDonActive = true;

    // --- YENİ EKLENDİ: SWITCH KİLİDİ ---
    [HideInInspector] public bool canSwitch = true;

    // ========================================================
    // --- YENİ EKLENDİ: GEÇİŞ EFEKTİ PARAMETRELERİ ---
    // ========================================================
    [Header("Geçiş Efekti Ayarları")]
    [SerializeField] private Image transitionOverlay; // Canvas altındaki siyah Image
    [SerializeField] private float fadeDuration = 0.12f; // Ekranın kararma ve açılma süresi
    [SerializeField] private float holdDuration = 0.04f; // Tam karanlıkta bekleme süresi
    private bool isTransitioning = false; // Üst üste geçiş tetiklenmesini önleyen kilit
    // ========================================================

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // UI Image başlangıçta şeffaf olsun
        if (transitionOverlay != null)
        {
            Color c = transitionOverlay.color;
            c.a = 0f;
            transitionOverlay.color = c;
        }

        // Oyun başlarken Don'u aç, Sancho'yu kapat
        SwitchCharacter(true);
    }

    void Update()
    {
        // TAB tuşuna basıldığında (Efekt oynatılıyorsa yeni geçişi engelle)
        if (Input.GetKeyDown(KeyCode.Tab) && !isTransitioning)
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

            // --- SANCHO KUTU TUTAÇI MU KONTROLÜ ---
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
                // --- GÜNCELLENDİ: Doğrudan geçiş yerine Coroutine tetikleniyor ---
                StartCoroutine(SwitchWithFadeRoutine(!isDonActive));
            }
            else
            {
                // Kutu iterken veya canSwitch false iken basarsa konsola uyarı atsın
                Debug.Log("🚫 Şu an karakter değiştirilemez! (Geçiş kilitli veya Sancho kutu tutuyor)");
            }
        }
    }

    // ========================================================
    // --- YENİ EKLENDİ: GEÇİŞ COROUTINE YAPISI ---
    // ========================================================
    private IEnumerator SwitchWithFadeRoutine(bool toDon)
    {
        isTransitioning = true;

        // transitionOverlay atanmadıysa hata vermemesi için güvenlik kontrolü
        if (transitionOverlay != null)
        {
            // 1. Ekran Kararıyor (Fade In)
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                Color c = transitionOverlay.color;
                c.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                transitionOverlay.color = c;
                yield return null;
            }
            
            // Tamamen siyah olduğundan emin olalım
            Color finalBlack = transitionOverlay.color;
            finalBlack.a = 1f;
            transitionOverlay.color = finalBlack;
        }

        // 2. Tam ekran kapkarayken orijinal geçiş mantığını çalıştırıyoruz
        SwitchCharacter(toDon);

        // Minik bir göz kırpma/bekleme süresi
        yield return new WaitForSeconds(holdDuration);

        if (transitionOverlay != null)
        {
            // 3. Ekran Açılıyor (Fade Out)
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                Color c = transitionOverlay.color;
                c.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                transitionOverlay.color = c;
                yield return null;
            }

            // Tamamen şeffaf yapalım
            Color finalClear = transitionOverlay.color;
            finalClear.a = 0f;
            transitionOverlay.color = finalClear;
        }

        isTransitioning = false;
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
            enemy.UpdateModelVisibility(isDonActive);
        }
    }
}