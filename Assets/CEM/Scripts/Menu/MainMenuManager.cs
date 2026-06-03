using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject areYouSurePanel;
    [SerializeField] private GameObject optionsPanel; // Sürükleyip bırakman için yeni alan

    [Header("Buttons")]
    [SerializeField] private Button continueButton;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    void Start()
    {
        // Başlangıçta panelleri ayarla
        mainMenuPanel.SetActive(true);
        areYouSurePanel.SetActive(false);
        optionsPanel.SetActive(false); // Oyun başlarken options kapalı olsun

        // PlayerPrefs veya kendi save sistemini kontrol et
        if (PlayerPrefs.HasKey("HasSaveData") && PlayerPrefs.GetInt("HasSaveData") == 1)
        {
            continueButton.interactable = true; // Save varsa buton aktif
        }
        else
        {
            continueButton.interactable = false; // Save yoksa buton tıklanamaz ve soluk olur
        }
    }

    // NEW GAME BUTONU
    public void OnNewGameClicked()
    {
        // Eğer save varsa emin misin diye sor, yoksa direkt oyunu başlat
        if (PlayerPrefs.HasKey("HasSaveData") && PlayerPrefs.GetInt("HasSaveData") == 1)
        {
            areYouSurePanel.SetActive(true);
        }
        else
        {
            StartNewGame();
        }
    }

    // ARE YOU SURE -> YES BUTONU
    public void OnAreYouSureYes()
    {
        PlayerPrefs.DeleteKey("HasSaveData"); // Eski save'i sil (örnek olarak)
        StartNewGame();
    }

    // ARE YOU SURE -> NO BUTONU
    public void OnAreYouSureNo()
    {
        areYouSurePanel.SetActive(false);
    }

    // CONTINUE BUTONU (GÜNCELLENDİ)
    public void OnContinueClicked()
    {
        // Direkt sahne yüklemek yerine LoadingManager'ı tetikliyoruz
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(gameplaySceneName);
        }
        else
        {
            // Eğer sahnede test yaparken LoadingManager yoksa oyun donmasın diye güvenlik önlemi
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    // OPTIONS BUTONU (GÜNCELLENDİ)
    public void OnOptionsClicked()
    {
        mainMenuPanel.SetActive(false); // Ana menüyü kapat
        optionsPanel.SetActive(true);   // Options panelini aç
    }

    // OPTIONS -> RETURN/BACK BUTONU (YENİ EKLENDİ)
    public void OnOptionsReturnClicked()
    {
        optionsPanel.SetActive(false);   // Options panelini kapat
        mainMenuPanel.SetActive(true);  // Ana menüyü tekrar aç
    }

    // EXIT BUTONU
    public void OnExitClicked()
    {
        Debug.Log("Oyundan Çıkıldı");
        Application.Quit();
    }

    private void StartNewGame()
    {
        // Yeni oyun hazırlıkları...
        PlayerPrefs.SetInt("HasSaveData", 1); // Yeni bir save oluşturuldu işareti
        
        // Direkt sahne yüklemek yerine LoadingManager'ı tetikliyoruz
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(gameplaySceneName);
        }
        else
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}