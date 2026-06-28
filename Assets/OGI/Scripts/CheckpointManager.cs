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

    // Şuan sahnede aktif olan checkpoint scriptini burada tutacağız kanka
    private Checkpoint activeCheckpointScript;

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
        if (editorTestModu) yield break;
        ApplyDataToSceneObjects();
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") return; 
        if (editorTestModu) return;

        // Sahne değiştiğinde eski referans çöp olacağı için temizliyoruz
        activeCheckpointScript = null;

        LoadDataFromDisk();
        ApplyDataToSceneObjects();
    }

    public void ApplyDataToSceneObjects()
    {
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

    // KODUNA EKLEDİĞİMİZ YENİ OVERLOAD METOD (Eski kodların patlamasın diye parametresiz halini de koruduk)
    public void UpdateCheckpoint(Vector3 newPos)
    {
        UpdateCheckpoint(newPos, null);
    }

    // Asıl işi yapan yeni fonksiyonumuz
    public void UpdateCheckpoint(Vector3 newPos, Checkpoint newCheckpointScript)
    {
        if (progressData == null || donData == null || sanchoData == null) return;

        // --- ESKİ BAYRAĞI İNDİRME MANTIĞI ---
        if (activeCheckpointScript != null && activeCheckpointScript != newCheckpointScript)
        {
            activeCheckpointScript.DeactivateCheckpoint();
        }

        // Yeni checkpoint scriptini hafızaya alıyoruz
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
        Debug.Log($"<color=cyan>🚩 [Checkpoint] {progressData.lastSavedSceneName} sahnesi, konum {newPos} kalıcı kaydedildi ve bayrak güncellendi!</color>");
    }

    public void RespawnResetStats()
    {
        if (donData == null || sanchoData == null) return;

        // 1. Önce diskteki envanter, token ve son checkpoint pozisyonu verilerini geri yüklüyoruz.
        LoadDataFromDisk();

        // 2. Diskten gelen eski/ölü can verisini zorla ezip karakterlerin canını maximuma çekiyoruz.
        donData.currentHealth = donData.maxHealth;
        sanchoData.currentHealth = sanchoData.maxHealth;

        // 3. Bu temiz ve fullenmiş canları hemen diske geri kaydediyoruz ki bir sonraki sahne açılışında 0 gelmesin.
        SaveDataToDisk();

        // 4. Şimdi sahnede canlı kanlı karakterlerin pozisyonunu ve canını verip UI'ı tazeleyebiliriz.
        ApplyDataToSceneObjects();

        Debug.Log("<color=red>💀 [Respawn] Oyuncu öldü! Canlar zorla fullendi, envanter son checkpoint haline geri çekildi ve kaydedildi.</color>");
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
            
            // 🎯 Eğer diskte daha önce kaydedilmiş bir can verisi bulunamazsa, 0 yerine direkt karakterlerin maksimum canını veriyoruz.
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
            // Eğer ilk defa sahne yükleniyorsa ve kayıt hiç yoksa canları maksimumda başlatıyoruz.
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