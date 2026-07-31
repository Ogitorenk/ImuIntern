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
        DynamicGI.UpdateEnvironment();
        
        if (transitionOverlay != null)
        {
            Color c = transitionOverlay.color;
            c.a = 0f;
            transitionOverlay.color = c;
        }

        SwitchCharacter(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !isTransitioning)
        {
            if (ZiplinePrefab.isAnyPlayerZiplining)
            {
                Debug.Log("🚫 Zipline üzerindeyken karakter değiştirilemez!");
                return;
            }

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
                return;
            }

            bool isSanchoHoldingBox = false;
            if (!isDonActive && sancho != null)
            {
                SanchoMovement sm = sancho.GetComponent<SanchoMovement>();
                if (sm != null && sm.isHoldingBox)
                {
                    isSanchoHoldingBox = true;
                }
            }

            if (canSwitch && !isSanchoHoldingBox)
            {
                StartCoroutine(SwitchWithFadeRoutine(!isDonActive));
            }
            else
            {
                Debug.Log("🚫 Şu an karakter değiştirilemez! (Geçiş kilitli veya Sancho kutu tutuyor)");
            }
        }
    }

    private IEnumerator SwitchWithFadeRoutine(bool toDon)
    {
        isTransitioning = true;

        if (transitionOverlay != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                Color c = transitionOverlay.color;
                c.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                transitionOverlay.color = c;
                yield return null;
            }

            Color finalBlack = transitionOverlay.color;
            finalBlack.a = 1f;
            transitionOverlay.color = finalBlack;
        }

        SwitchCharacter(toDon);

        yield return new WaitForSeconds(holdDuration);

        if (transitionOverlay != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                Color c = transitionOverlay.color;
                c.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                transitionOverlay.color = c;
                yield return null;
            }

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

        // Script referanslarını çekelim kanka
        DonMovement donScript = donQuixote.GetComponent<DonMovement>();
        SanchoMovement sanchoScript = sancho.GetComponent<SanchoMovement>();

        // Havada geçiş momentumunu saklamak için değişken
        Vector3 preservedVelocity = Vector3.zero;

        // 1. ÖNCE ESKİ KARAKTERİN DURUMUNU RESETLE VE HIZINI KOPYALA
        if (isDonActive) // Sancho'dan Don'a geçiyoruz
        {
            if (sanchoScript != null)
            {
                preservedVelocity = sanchoScript.CurrentVelocity; // Havada uçuş hızını kaydet kanka
                sanchoScript.ResetCharacterStates();
                sanchoScript.isControlled = false;
            }
        }
        else // Don'dan Sancho'ya geçiyoruz
        {
            if (donScript != null)
            {
                preservedVelocity = donScript.CurrentVelocity;
                donScript.ResetCharacterStates();
                donScript.isControlled = false;
            }
        }

        // Pozisyon ve rotasyon eşitleme
        CharacterController ccActive = activeChar.GetComponent<CharacterController>();
        if (ccActive != null) ccActive.enabled = false;

        activeChar.transform.position = inactiveChar.transform.position;
        activeChar.transform.rotation = inactiveChar.transform.rotation;

        if (ccActive != null) ccActive.enabled = true;

        // Modelleri aç/kapat
        activeChar.SetActive(true);
        inactiveChar.SetActive(false);

        // 2. YENİ KARAKTERİ SIFIRLA VE SAKLANAN HAVADAKİ IVMEYİ YEDİR KANKA
        if (isDonActive)
        {
            if (donScript != null)
            {
                donScript.ResetCharacterStates();
                donScript.CurrentVelocity = preservedVelocity; // Sancho'nun zıplama hızı Don'a geçti!
                donScript.isControlled = true;
            }
        }
        else
        {
            if (sanchoScript != null)
            {
                sanchoScript.ResetCharacterStates();
                sanchoScript.CurrentVelocity = preservedVelocity; // Don'un hızı Sancho'ya geçti!
                sanchoScript.isControlled = true;
            }
        }

        // Çevre algı güncellemeleri
        UpdateAllJumpPads();
        UpdateAllBreakablePlatforms();
        UpdateAllMovingIllusionPlatforms();

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.SwitchHUD(isDonActive);
        }

        UpdateAllFlyingEnemiesPerception();
    }

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

    void UpdateAllJumpPads()
    {
        IllusionJumpPad[] jumpPads = FindObjectsOfType<IllusionJumpPad>(true);
        foreach (IllusionJumpPad pad in jumpPads)
        {
            pad.UpdatePerception(isDonActive);
        }
    }

    void UpdateAllBreakablePlatforms()
    {
        BreakableIllusionPlatform[] platforms = FindObjectsOfType<BreakableIllusionPlatform>(true);
        foreach (BreakableIllusionPlatform platform in platforms)
        {
            platform.UpdatePerception(isDonActive);
        }
    }

    void UpdateAllMovingIllusionPlatforms()
    {
        MovingIllusionPlatform[] movingPlatforms = FindObjectsOfType<MovingIllusionPlatform>(true);
        foreach (MovingIllusionPlatform platform in movingPlatforms)
        {
            platform.UpdatePerception(isDonActive);
        }
    }

    void UpdateAllFlyingEnemiesPerception()
    {
        EnemyFlying[] flyingEnemies = FindObjectsOfType<EnemyFlying>(true);
        foreach (EnemyFlying enemy in flyingEnemies)
        {
            enemy.UpdateModelVisibility(isDonActive);
        }
    }
}