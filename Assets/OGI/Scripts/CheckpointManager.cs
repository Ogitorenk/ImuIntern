using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [Header("--- GELİŞTİRİCİ AYARI ---")]
    public bool editorTestModu = false; 

    [Header("--- DATA DOSYALARI ---")]
    [SerializeField] private CharacterData donData;
    [SerializeField] private CharacterData sanchoData;
    [SerializeField] private GameProgressData progressData;

    public bool useInitialPositionAsCheckpoint = true;
    private Checkpoint activeCheckpointScript;

    // --- KAPIDAN GELEN GEÇİCİ IŞINLANMA EMRI (STATİK) ---
    private static bool manualSpawnOverrideActive = false;
    private static Vector3 manualSpawnPosition;

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

    // SceneChanger veya ProgressionTrigger'ın çağıracağı statik emir metodu
    public static void OverrideNextSpawn(Vector3 targetPos)
    {
        manualSpawnOverrideActive = true;
        manualSpawnPosition = targetPos;
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" || editorTestModu) return;

        activeCheckpointScript = null;

        LoadDataFromDisk();
        ApplyDataToSceneObjects();
    }

    public void ApplyDataToSceneObjects()
    {
        if (editorTestModu) return;

        // Sahnedeki diğer objelerin Start() metodlarının bitmesini beklemek için coroutine başlatıyoruz
        StartCoroutine(ApplyDataRoutine());
    }

    private IEnumerator ApplyDataRoutine()
    {
        // Karakterlerin Start() kodlarının çalışıp bitmesini ve pozisyonu ezmesini engellemek için 1 frame bekle
        yield return new WaitForEndOfFrame();

        string currentSceneName = SceneManager.GetActiveScene().name;

        DonMovement don = FindObjectOfType<DonMovement>();
        SanchoMovement sancho = FindObjectOfType<SanchoMovement>();

        // -------------------------------------------------------------
        // 1. ÖNCELİK: KAPIDAN GELDİK VE ÖZEL KOORDİNAT İSTENDİ
        // -------------------------------------------------------------
        if (manualSpawnOverrideActive)
        {
            Vector3 targetPos = manualSpawnPosition;

            if (don != null) TeleportObject(don.gameObject, targetPos);
            if (sancho != null) TeleportObject(sancho.gameObject, targetPos + new Vector3(1f, 0f, 0f));

            Debug.Log($"<color=green>⚡ [CheckpointManager] KESİN EMİR: Kapı özel koordinatında spawn yapıldı: {targetPos}</color>");

            // Emri kullandık, bayrağı indiriyoruz
            manualSpawnOverrideActive = false;
        }
        // -------------------------------------------------------------
        // 2. ÖNCELİK: KAPI EMRİ YOK, BU SAHNEYE AİT CHECKPOINT VAR
        // -------------------------------------------------------------
        else if (progressData.lastCheckpointPosition != Vector3.zero && progressData.lastSavedSceneName == currentSceneName)
        {
            Vector3 checkPos = progressData.lastCheckpointPosition;

            if (don != null) TeleportObject(don.gameObject, checkPos);
            if (sancho != null) TeleportObject(sancho.gameObject, checkPos + new Vector3(1f, 0f, 0f));

            Debug.Log($"<color=cyan>🚩 [CheckpointManager] Sahne Checkpoint'inde Spawn Yapıldı: {checkPos}</color>");
        }
        // -------------------------------------------------------------
        // 3. ÖNCELİK: NE KAPI EMRİ VAR NE CHECKPOINT (İLK DEFA GİRİLİYOR)
        // -------------------------------------------------------------
        else
        {
            Debug.Log($"<color=yellow>🏠 [CheckpointManager] Var olan checkpoint bu sahneye ait değil. Varsayılan konuma dokunulmadı.</color>");
        }

        // --- CAN VE POT YÜKLEMESİ ---
        if (don != null && donData != null)
        {
            don.currentHealth = donData.currentHealth;
            don.healthPotionCount = donData.healthPotionCount;
            don.slowPotionCount = donData.slowPotionCount;
        }

        if (sancho != null && sanchoData != null)
        {
            sancho.currentHealth = sanchoData.currentHealth;
            sancho.healthPotionCount = sanchoData.healthPotionCount;
            sancho.slowPotionCount = sanchoData.slowPotionCount;
        }

        UpdateAllUI();
    }

    private void TeleportObject(GameObject obj, Vector3 targetPos)
    {
        // CharacterController varsa fiziği geçici kilitliyoruz
        CharacterController cc = obj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // NavMeshAgent varsa durduruyoruz
        UnityEngine.AI.NavMeshAgent agent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // Pozisyonu zorla çakıyoruz
        obj.transform.position = targetPos;
        Physics.SyncTransforms();

        // Component'leri geri açıyoruz
        if (cc != null) cc.enabled = true;
        if (agent != null) 
        {
            agent.enabled = true;
            agent.Warp(targetPos);
        }
    }

    public void UpdateCheckpoint(Vector3 newPos)
    {
        UpdateCheckpoint(newPos, null);
    }

    public void UpdateCheckpoint(Vector3 newPos, Checkpoint newCheckpointScript)
    {
        if (progressData == null || donData == null || sanchoData == null) return;

        if (activeCheckpointScript != null && activeCheckpointScript != newCheckpointScript)
        {
            activeCheckpointScript.DeactivateCheckpoint();
        }

        activeCheckpointScript = newCheckpointScript;

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
        Debug.Log($"<color=cyan>🚩 [Checkpoint] {progressData.lastSavedSceneName} sahnesi, konum {newPos} kalıcı kaydedildi!</color>");
    }

    public void RespawnResetStats()
    {
        if (donData == null || sanchoData == null) return;

        LoadDataFromDisk();

        donData.currentHealth = donData.maxHealth;
        sanchoData.currentHealth = sanchoData.maxHealth;

        SaveDataToDisk();
        ApplyDataToSceneObjects();
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
            
            donData.currentHealth = PlayerPrefs.GetFloat("SO_DonH", donData != null ? donData.maxHealth : 100f);
            donData.healthPotionCount = PlayerPrefs.GetInt("SO_DonHP", 0);
            donData.slowPotionCount = PlayerPrefs.GetInt("SO_DonSP", 0);

            sanchoData.currentHealth = PlayerPrefs.GetFloat("SO_SanH", sanchoData != null ? sanchoData.maxHealth : 100f);
            sanchoData.healthPotionCount = PlayerPrefs.GetInt("SO_SanHP", 0);
            sanchoData.slowPotionCount = PlayerPrefs.GetInt("SO_SanSP", 0);
            sanchoData.arrowCount = PlayerPrefs.GetInt("SO_SanArrows", sanchoData != null ? sanchoData.maxArrowCount : 20);
            
            progressData.totalTokens = PlayerPrefs.GetInt("SO_Tokens", 0);
        }
        else
        {
            if (donData != null) donData.currentHealth = donData.maxHealth;
            if (sanchoData != null) sanchoData.currentHealth = sanchoData.maxHealth;
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

    public Vector3 GetLastCheckpoint() { return progressData.lastCheckpointPosition; }
}