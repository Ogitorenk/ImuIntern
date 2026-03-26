using UnityEngine;

public class DualRealityManager : MonoBehaviour
{
    public static DualRealityManager Instance;

    [Header("Karakter Prefablarý")]
    public GameObject donQuixote;
    public GameObject sancho;

    [HideInInspector] public bool isDonActive = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Oyun baþlarken Don'u aç, Sancho'yu kapat
        SwitchCharacter(true);
    }

    void Update()
    {
        // TAB tuþuna basýldýðýnda karakter deðiþtir
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchCharacter(!isDonActive);
        }
    }

    void SwitchCharacter(bool toDon)
    {
        isDonActive = toDon;

        GameObject activeChar = isDonActive ? donQuixote : sancho;
        GameObject inactiveChar = isDonActive ? sancho : donQuixote;

        // Ýnaktif karakterin pozisyonunu, aktif karaktere kopyala
        CharacterController ccActive = activeChar.GetComponent<CharacterController>();

        if (ccActive != null) ccActive.enabled = false;

        activeChar.transform.position = inactiveChar.transform.position;
        activeChar.transform.rotation = inactiveChar.transform.rotation;

        if (ccActive != null) ccActive.enabled = true;

        // Modelleri aç/kapat
        activeChar.SetActive(true);
        inactiveChar.SetActive(false);

        // --- GÜNCELLEMELER ÇAÐRILIYOR ---
        UpdateAllJumpPads();
        UpdateAllBreakablePlatforms();
        UpdateAllMovingIllusionPlatforms();
    }

    // --- YENÝ EKLENDÝ (true): Sahnede gizli/kapalý olsa bile tüm JumpPad'leri bulur ---
    void UpdateAllJumpPads()
    {
        IllusionJumpPad[] jumpPads = FindObjectsOfType<IllusionJumpPad>(true);
        foreach (IllusionJumpPad pad in jumpPads)
        {
            pad.UpdatePerception(isDonActive);
        }
    }

    // --- YENÝ EKLENDÝ (true): Sahnede gizli/kapalý olsa bile tüm Kýrýlabilir Platformlarý bulur ---
    void UpdateAllBreakablePlatforms()
    {
        BreakableIllusionPlatform[] platforms = FindObjectsOfType<BreakableIllusionPlatform>(true);
        foreach (BreakableIllusionPlatform platform in platforms)
        {
            platform.UpdatePerception(isDonActive);
        }
    }

    // --- YENÝ EKLENDÝ (true): Sahnede gizli/kapalý olsa bile tüm Ýllüzyonlu Hareketli Platformlarý bulur ---
    void UpdateAllMovingIllusionPlatforms()
    {
        MovingIllusionPlatform[] movingPlatforms = FindObjectsOfType<MovingIllusionPlatform>(true);
        foreach (MovingIllusionPlatform platform in movingPlatforms)
        {
            platform.UpdatePerception(isDonActive);
        }
    }
}