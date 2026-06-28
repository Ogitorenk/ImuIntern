using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private GameProgressData gameProgressData; 
    [SerializeField] private CharacterData donData;
    [SerializeField] private CharacterData sanchoData;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject areYouSurePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;

    [Header("Scene Settings")]
    [SerializeField] private string defaultNewGameScene = "IntroScene"; // Yeni oyunun başlayacağı sahne

    void Start()
    {
        mainMenuPanel.SetActive(true);
        areYouSurePanel.SetActive(false);
        optionsPanel.SetActive(false);

        // Ortak kontrol anahtarı
        if (PlayerPrefs.HasKey("HasSaveData") && PlayerPrefs.GetInt("HasSaveData") == 1)
        {
            continueButton.interactable = true;
        }
        else
        {
            continueButton.interactable = false;
        }
    }

    public void OnNewGameClicked()
    {
        if (PlayerPrefs.HasKey("HasSaveData") && PlayerPrefs.GetInt("HasSaveData") == 1)
        {
            areYouSurePanel.SetActive(true);
        }
        else
        {
            // Eğer hiç kayıt yoksa doğrudan temiz bir başlangıç yap
            ClearAllSaveData(); 
            StartNewGame();
        }
    }

    public void OnAreYouSureYes()
    {
        ClearAllSaveData(); // Eski tüm verileri diskten ve RAM'den tamamen kazıyoruz
        StartNewGame();
    }

    public void OnAreYouSureNo()
    {
        areYouSurePanel.SetActive(false);
    }

    public void OnContinueClicked()
    {
        if (gameProgressData != null)
        {
            // Eşitlenmiş anahtarlarla diskten son veriyi çekiyoruz
            gameProgressData.LoadFromDisk();
            
            string targetScene = gameProgressData.lastSavedSceneName;
            LoadTargetScene(targetScene);
        }
    }

    private void StartNewGame()
    {
        if (gameProgressData != null)
        {
            // ScriptableObject'i fabrikasyon ayarlarına döndür
            gameProgressData.ResetToDefault();
            gameProgressData.lastSavedSceneName = defaultNewGameScene;
            
            // Diske bu temiz veriyi kaydet (Böylece CheckpointManager eskisini yükleyemez)
            gameProgressData.SaveToDisk();

            LoadTargetScene(defaultNewGameScene);
        }
    }

    private void ClearAllSaveData()
    {
        // 1. Tüm anahtarları tamamen siliyoruz ki çakışma yaşanmasın kanka
        PlayerPrefs.DeleteKey("HasSaveData");
        PlayerPrefs.DeleteKey("SO_LastScene");
        PlayerPrefs.DeleteKey("SO_CheckX");
        PlayerPrefs.DeleteKey("SO_CheckY");
        PlayerPrefs.DeleteKey("SO_CheckZ");
        PlayerPrefs.DeleteKey("SO_Tokens");
        
        PlayerPrefs.DeleteKey("SO_DonH");
        PlayerPrefs.DeleteKey("SO_DonHP");
        PlayerPrefs.DeleteKey("SO_DonSP");
        PlayerPrefs.DeleteKey("SO_SanH");
        PlayerPrefs.DeleteKey("SO_SanHP");
        PlayerPrefs.DeleteKey("SO_SanSP");
        PlayerPrefs.DeleteKey("SO_SanArrows");
        
        PlayerPrefs.Save();

        // 2. 🎯 [RAM TEMİZLİĞİ VE GARANTİ CAN AYARI] 
        // Editördeki veya ScriptableObject'teki hatalı 0 değerlerini ezmek için 100f değerini sertçe yazıyoruz.
        if (donData != null)
        {
            donData.maxHealth = 100f;
            donData.currentHealth = 100f;
            donData.healthPotionCount = 0;
            donData.slowPotionCount = 0;
        }

        if (sanchoData != null)
        {
            sanchoData.maxHealth = 100f;
            sanchoData.currentHealth = 100f;
            sanchoData.healthPotionCount = 0;
            sanchoData.slowPotionCount = 0;
            sanchoData.maxArrowCount = 30; // Maksimum ok kapasitesi
            sanchoData.arrowCount = 30;    // Yeni oyundaki mevcut ok sayısı
        }

        Debug.Log("<color=green>✨ [New Game] Disk sıfırlandı. Don ve Sancho 100 can ile pürüzsüzce hazırlandı!</color>");
    }

    private void LoadTargetScene(string sceneName)
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void OnOptionsClicked()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void OnOptionsReturnClicked()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OnExitClicked()
    {
        Application.Quit();
    }
}