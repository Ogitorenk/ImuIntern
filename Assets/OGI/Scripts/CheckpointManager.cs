using UnityEngine;
using System.Collections;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

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
        ApplyDataToSceneObjects();
    }

    public void ApplyDataToSceneObjects()
    {
        DonMovement don = FindObjectOfType<DonMovement>();
        if (don != null && donData != null)
        {
            don.currentHealth = donData.currentHealth;
            don.healthPotionCount = donData.healthPotionCount;
            don.slowPotionCount = donData.slowPotionCount;
        }

        SanchoMovement sancho = FindObjectOfType<SanchoMovement>();
        if (sancho != null && sanchoData != null)
        {
            sancho.currentHealth = sanchoData.currentHealth;
            sancho.healthPotionCount = sanchoData.healthPotionCount;
            sancho.slowPotionCount = sanchoData.slowPotionCount;

            // Eğer SanchoMovement içinde ok sayısını tutan bir değişkenin varsa (Örn: arrowCount) burayı aç kanka:
            // sancho.arrowCount = sanchoData.arrowCount;
        }

        UpdateAllUI();
    }

    // 🚩 CHECKPOINT BAYRAĞINA DEĞDİĞİNDE: O anki canı, oku, her şeyi aynen kaydeder kanka.
    public void UpdateCheckpoint(Vector3 newPos)
    {
        if (progressData == null || donData == null || sanchoData == null) return;

        progressData.lastCheckpointPosition = newPos;

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

            // Sancho'nun sahnedeki güncel ok sayısını dataya çekiyoruz kanka
            // sanchoData.arrowCount = sancho.arrowCount;
        }

        SaveDataToDisk();
        Debug.Log("<color=cyan>🚩 [Checkpoint] Mevcut canlar ve ok sayısı kalıcı olarak kaydedildi!</color>");
    }

    // 💀 OYUNCU ÖLDÜĞÜNDE TETİKLENECEK KOD: Canları 100 yapar, pot ve okları son checkpoint durumuna çeker kanka!
    public void RespawnResetStats()
    {
        if (donData == null || sanchoData == null) return;

        // Ölünce 100 canla başlama kuralı kanka: Datadaki canları zorla fulle!
        donData.currentHealth = donData.maxHealth;
        sanchoData.currentHealth = sanchoData.maxHealth;

        // Potlar ve oklar zaten en son checkpoint'te diske ne yazıldıysa Load edilerek o güvenli sayıya geri dönecek (israf engelleme)
        LoadDataFromDisk();

        // Yenilenen datayı sahnedeki Don ve Sancho objelerine geri enjekte et
        ApplyDataToSceneObjects();

        Debug.Log("<color=red>💀 [Respawn] Oyuncu öldü! Canlar 100'e fullendi, envanter son checkpoint haline geri çekildi.</color>");
    }

    public void SaveDataToDisk()
    {
        PlayerPrefs.SetFloat("SO_CheckX", progressData.lastCheckpointPosition.x);
        PlayerPrefs.SetFloat("SO_CheckY", progressData.lastCheckpointPosition.y);
        PlayerPrefs.SetFloat("SO_CheckZ", progressData.lastCheckpointPosition.z);

        PlayerPrefs.SetFloat("SO_DonH", donData.currentHealth);
        PlayerPrefs.SetInt("SO_DonHP", donData.healthPotionCount);
        PlayerPrefs.SetInt("SO_DonSP", donData.slowPotionCount);

        PlayerPrefs.SetFloat("SO_SanH", sanchoData.currentHealth);
        PlayerPrefs.SetInt("SO_SanHP", sanchoData.healthPotionCount);
        PlayerPrefs.SetInt("SO_SanSP", sanchoData.slowPotionCount);

        // Ok sayısını hard diske kilitliyoruz kanka
        PlayerPrefs.SetInt("SO_SanArrows", sanchoData.arrowCount);

        PlayerPrefs.SetInt("SO_Tokens", progressData.totalTokens);
        PlayerPrefs.SetInt("HasSavedGame", 1);
        PlayerPrefs.Save();
    }

    public void LoadDataFromDisk()
    {
        if (PlayerPrefs.GetInt("HasSavedGame", 0) == 1)
        {
            progressData.lastCheckpointPosition = new Vector3(
                PlayerPrefs.GetFloat("SO_CheckX"),
                PlayerPrefs.GetFloat("SO_CheckY"),
                PlayerPrefs.GetFloat("SO_CheckZ")
            );

            // ÖLÜM DIŞINDA oyuna Continue deyip girildiğinde kaç canı varsa öyle başlasın diye diski okuyor kanka:
            donData.currentHealth = PlayerPrefs.GetFloat("SO_DonH");
            donData.healthPotionCount = PlayerPrefs.GetInt("SO_DonHP");
            donData.slowPotionCount = PlayerPrefs.GetInt("SO_DonSP");

            sanchoData.currentHealth = PlayerPrefs.GetFloat("SO_SanH");
            sanchoData.healthPotionCount = PlayerPrefs.GetInt("SO_SanHP");
            sanchoData.slowPotionCount = PlayerPrefs.GetInt("SO_SanSP");

            // Ok sayısını diskten geri yüklüyoruz kanka
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

            // Eğer HUDManager'da ok sayısını güncelleyen fonksiyonun varsa buraya çakabilirsin kanka:
            // HUDManager.Instance.UpdateSanchoArrows(sanchoData.arrowCount);
        }
    }

    public Vector3 GetLastCheckpoint()
    {
        return progressData.lastCheckpointPosition;
    }
}