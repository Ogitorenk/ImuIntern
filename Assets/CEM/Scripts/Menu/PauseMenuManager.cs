using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct PauseButtonElements
{
    public string buttonName;
    public Button button;
    public GameObject selectionIndicator; // Oklu boru görseli
}

public class PauseMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuPanel;   
    [SerializeField] private GameObject areYouSurePanel;   
    [SerializeField] private GameObject optionsPanel;      

    [Header("Buton ve Ok Eslesmeleri (Gelişmiş)")]
    [SerializeField] private PauseButtonElements[] menuButtons;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; 

    private bool isPaused = false;

    void Start()
    {
        // Dinamik olarak butonlara Mouse Hover olaylarını bağlıyoruz
        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i;
            if (menuButtons[i].button != null)
            {
                EventTrigger trigger = menuButtons[i].button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = menuButtons[i].button.gameObject.AddComponent<EventTrigger>();
                }

                trigger.triggers.Clear();

                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener((data) => { HighlightButton(index); });
                trigger.triggers.Add(entry);
            }
        }

        // --- GÜVENLİK GÜNCELLEMESİ ---
        // Sahnede panel açık unutulduysa kapat ama Time.timeScale'i bozma kanka
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (areYouSurePanel != null) areYouSurePanel.SetActive(false);
        
        isPaused = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                if (optionsPanel != null && optionsPanel.activeSelf)
                {
                    OnOptionsReturnClicked();
                }
                else if (areYouSurePanel != null && areYouSurePanel.activeSelf)
                {
                    OnAreYouSureNo();
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    // === CONTINUE BUTONU ===
    public void OnContinueClicked()
    {
        ResumeGame();
    }

    public void OnNewGameClicked()
    {
        if (areYouSurePanel != null) areYouSurePanel.SetActive(true);
    }

    public void OnAreYouSureYes()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnAreYouSureNo()
    {
        if (areYouSurePanel != null) areYouSurePanel.SetActive(false);
        ResetSelection();
    }

    public void OnOptionsClicked()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void OnOptionsReturnClicked()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        ResetSelection();
    }

    public void OnExitClicked()
    {
        Time.timeScale = 1f; 
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        
        // ZAMANI ZINK DİYE DURDURUYORUZ
        Time.timeScale = 0f; 
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResetSelection();
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (areYouSurePanel != null) areYouSurePanel.SetActive(false);
        
        // ZAMANI NORMALE DÖNDÜRÜYORUZ
        Time.timeScale = 1f; 

        // --- GÜNCELLEME: Oyuna dönünce imleci geri kilitle kanka ---
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void HighlightButton(int targetIndex)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i].selectionIndicator != null)
            {
                bool isHovered = (i == targetIndex);
                menuButtons[i].selectionIndicator.SetActive(isHovered);
            }
        }
    }

    public void ResetSelection()
    {
        if (menuButtons != null && menuButtons.Length > 0)
        {
            HighlightButton(0); 
        }
    }
}