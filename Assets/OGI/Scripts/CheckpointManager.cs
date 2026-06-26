using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [Header("--- GELİŞTİRİCİ AYARI ---")]
    // 🛠️ BUNA TIKLARSAN KARAKTERLER KOYDUĞUN YERDE BAŞLAR KANKA!
    [Tooltip("İşaretliyken karakterler son checkpoint yerine sahnede koyduğun yerde doğar.")]
    public bool editorTestModu = false; 

    [Header("--- DATA DOSYALARI ---")]
    [SerializeField] private CharacterData donData;
    [SerializeField] private CharacterData sanchoData;
    [SerializeField] private GameProgressData progressData;

    public bool useInitialPositionAsCheckpoint = true;

    public int totalTokens { get { return progressData.totalTokens; } set { progressData.totalTokens = value; } }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        
        // 🛠️ Eğer test modundaysak ışınlamayı tamamen es geç
        if (editorTestModu) yield break;

        ApplyDataToSceneObjects();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") return; 

        // 🛠️ Eğer test modundaysak hiçbir şeyi ışınlama, diskten okuma
        if (editorTestModu) return;

        LoadDataFromDisk();
        ApplyDataToSceneObjects();
    }

    public void ApplyDataToSceneObjects()
    {
        // 🛠️ Güvenlik önlemi: Başka bir script arkadan çağırırsa yine engellesin
        if (editorTestModu) return;

        DonMovement don = FindObjectOfType<DonMovement>();
        if (don != null && donData != null)
        {
            don.currentHealth = donData.currentHealth;
            don.healthPotionCount = donData.healthPotionCount;
            don.slowPotionCount = donData.slowPotionCount;

            if (progressData.lastCheckpointPosition != Vector3.zero)
            {
                don.transform.position = progressData.lastCheckpointPosition;
            }
        }

        SanchoMovement sancho = FindObjectOfType<SanchoMovement>();
        if (sancho != null && sanchoData != null)
        {
            sancho.currentHealth = sanchoData.currentHealth;
            sancho.healthPotionCount = sanchoData.healthPotionCount;
            sancho.slowPotionCount = sanchoData.slowPotionCount;

            if (progressData.lastCheckpointPosition != Vector3.zero)
            {
                sancho.transform.position = progressData.lastCheckpointPosition + new Vector3(1f, 0f, 0f);
            }
        }

        UpdateAllUI();
    }

    public void UpdateCheckpoint(Vector3 newPos)
    {
        if (progressData == null || donData == null || sanchoData == null) return;

        progressData.lastCheckpointPosition = newPos;
        progressData.lastSavedSceneName = SceneManager.GetActiveScene().name;

        DonMovement don = FindObjectOfType<DonMovement>();
        if (don != null)
        {
            donData.currentHealth = don.currentHealth;
            donData.healthPotionCount = don.healthPotionCount;
            donData.slowPotionCount = don.slowPotionCount;
        }

        SanchoMovement sancho = FindObjectOfType<SanchoMovement>();
        if (sancho != null)
        {
            sanchoData.currentHealth = sancho.currentHealth;
            sanchoData.healthPotionCount = sancho.healthPotionCount;
            sanchoData.slowPotionCount = sancho.slowPotionCount;
        }

        SaveDataToDisk();
        Debug.Log($"<color=cyan>🚩 [Checkpoint] {progressData.lastSavedSceneName} sahnesi ve konum {newPos} kalıcı olarak kaydedildi!</color>");
    }

    public void RespawnResetStats()
    {
        if (donData == null || sanchoData == null) return;

        donData.currentHealth = donData.maxHealth;
        sanchoData.currentHealth = sanchoData.maxHealth;

        LoadDataFromDisk();
        ApplyDataToSceneObjects();

        Debug.Log("<color=red>💀 [Respawn] Oyuncu öldü! Canlar fullendi, envanter son checkpoint haline geri çekildi.</color>");
    }

    public void SaveDataToDisk()
    {
        PlayerPrefs.SetFloat("SO_CheckX", progressData.lastCheckpointPosition.x);
        PlayerPrefs.SetFloat("SO_CheckY", progressData.lastCheckpointPosition.y);
        PlayerPrefs.SetFloat("SO_CheckZ", progressData.lastCheckpointPosition.z);
        
        PlayerPrefs.SetString("SO_LastScene", progressData.lastSavedSceneName);

        PlayerPrefs.SetFloat("SO_DonH", donData.currentHealth);
        PlayerPrefs.SetInt("SO_DonHP", donData.healthPotionCount);
        PlayerPrefs.SetInt("SO_DonSP", donData.slowPotionCount);

        PlayerPrefs.SetFloat("SO_SanH", sanchoData.currentHealth);
        PlayerPrefs.SetInt("SO_SanHP", sanchoData.healthPotionCount);
        PlayerPrefs.SetInt("SO_SanSP", sanchoData.slowPotionCount);
        PlayerPrefs.SetInt("SO_SanArrows", sanchoData.arrowCount);

        PlayerPrefs.SetInt("SO_Tokens", progressData.totalTokens);
        
        PlayerPrefs.SetInt("HasSaveData", 1); 
        PlayerPrefs.Save();
    }

    public void LoadDataFromDisk()
    {
        if (PlayerPrefs.GetInt("HasSaveData", 0) == 1)
        {
            progressData.lastCheckpointPosition = new Vector3(
                PlayerPrefs.GetFloat("SO_CheckX"),
                PlayerPrefs.GetFloat("SO_CheckY"),
                PlayerPrefs.GetFloat("SO_CheckZ")
            );

            progressData.lastSavedSceneName = PlayerPrefs.GetString("SO_LastScene", "Level_1");

            donData.currentHealth = PlayerPrefs.GetFloat("SO_DonH");
            donData.healthPotionCount = PlayerPrefs.GetInt("SO_DonHP");
            donData.slowPotionCount = PlayerPrefs.GetInt("SO_DonSP");

            sanchoData.currentHealth = PlayerPrefs.GetFloat("SO_SanH");
            sanchoData.healthPotionCount = PlayerPrefs.GetInt("SO_SanHP");
            sanchoData.slowPotionCount = PlayerPrefs.GetInt("SO_SanSP");
            sanchoData.arrowCount = PlayerPrefs.GetInt("SO_SanArrows", sanchoData.maxArrowCount);

            progressData.totalTokens = PlayerPrefs.GetInt("SO_Tokens");
        }
    }

    public void UpdateAllUI()
    {
        if (HUDManager.Instance != null && donData != null && sanchoData != null)
        {
            HUDManager.Instance.UpdateDonQuixoteHealth(donData.currentHealth, donData.maxHealth);
            HUDManager.Instance.UpdateDonQuixotePotions(donData.healthPotionCount, donData.slowPotionCount);
            HUDManager.Instance.UpdateSanchoHealth(sanchoData.currentHealth, sanchoData.maxHealth);
            HUDManager.Instance.UpdateSanchoPotions(sanchoData.healthPotionCount, sanchoData.slowPotionCount);
        }
    }

    public Vector3 GetLastCheckpoint()
    {
        return progressData.lastCheckpointPosition;
    }
}