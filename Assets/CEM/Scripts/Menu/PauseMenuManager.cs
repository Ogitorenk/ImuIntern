using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Cinemachine; 

[System.Serializable]
public struct PauseButtonElements
{
    public string buttonName;
    public Button button;
    public GameObject selectionIndicator; // Oklu boru görseli
}

public class PauseMenuManager : MonoBehaviour
{
    [Header("Data Reference")]
    // 🎯 [YENİ] En son kaydedilen sahne adını diskten okumak için ScriptableObject'imizi bağlıyoruz kanka
    [SerializeField] private GameProgressData gameProgressData;

    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject areYouSurePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Buton ve Ok Eslesmeleri (Gelişmiş)")]
    [SerializeField] private PauseButtonElements[] menuButtons;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Cinemachine Kamera Ayarı")]
    [SerializeField] private CinemachineFreeLook playerCamera;

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

    // === CONTINUE/RESUME BUTONU === (Mevcut oyunu kaldığı yerden devam ettirir)
    public void OnContinueClicked()
    {
        ResumeGame();
    }

    // === LOAD LAST CHECKPOINT BUTONU === (Eski New Game Butonu yerine bu tetiklenecek kanka)
    public void OnLoadLastCheckpointClicked()
    {
        if (areYouSurePanel != null) areYouSurePanel.SetActive(true);
    }

    // ARE YOU SURE -> YES BUTONU (En son kayda dönmeyi onayladığında)
    public void OnAreYouSureYes()
    {
        // 🎯 [KRİTİK] Zamanı normal akışına döndürüyoruz yoksa yeni sahne donuk başlar!
        Time.timeScale = 1f; 

        if (gameProgressData != null)
        {
            // Diskten en güncel veriyi (en son mühürlenen sahneyi) çekiyoruz
            gameProgressData.LoadFromDisk();
            string targetScene = gameProgressData.lastSavedSceneName;

            Debug.Log($"<color=yellow>⏳ Son checkpointe dönülüyor. Yüklenen Sahne: {targetScene}</color>");

            // LoadingManager varsa onunla, yoksa düz yükle
            if (LoadingManager.Instance != null)
            {
                LoadingManager.Instance.LoadScene(targetScene);
            }
            else
            {
                SceneManager.LoadScene(targetScene);
            }
        }
        else
        {
            // Eğer referans unutulduysa oyun kilitlenmesin diye mevcut sahneyi yeniden başlatır
            Debug.LogError("PauseMenuManager içinde GameProgressData referansı eksik!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
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

        Time.timeScale = 0f;

        if (playerCamera != null)
        {
            playerCamera.m_XAxis.m_InputAxisName = "";
            playerCamera.m_YAxis.m_InputAxisName = "";
            playerCamera.m_XAxis.m_InputAxisValue = 0f;
            playerCamera.m_YAxis.m_InputAxisValue = 0f;
        }

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

        Time.timeScale = 1f;

        if (playerCamera != null)
        {
            playerCamera.m_XAxis.m_InputAxisName = "Mouse X";
            playerCamera.m_YAxis.m_InputAxisName = "Mouse Y";
        }

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