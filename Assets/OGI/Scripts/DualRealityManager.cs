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
        // TAB tuşuna basıldığında VE kilit açıkken (canSwitch == true) karakter değiştir
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (canSwitch)
            {
                SwitchCharacter(!isDonActive);
            }
            else
            {
                // Kutu iterken basarsa konsola uyarı atsın (oyun içinde de ses falan çalabilirsin ileride)
                Debug.Log("🚫 Şu an karakter değiştirilemez! (Sancho meşgul)");
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