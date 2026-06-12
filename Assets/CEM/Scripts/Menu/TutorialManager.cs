using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    
    // Tek bir Image yerine, Horizontal Layout Group içindeki Image'ları buraya dizi olarak atayacağız
    [SerializeField] private Image[] keyPromptImages; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        popupPanel.SetActive(false);
    }

    public void ShowTutorial(string message, Sprite[] keySprites)
    {
        tutorialText.text = message;

        // Önce tüm UI tuş resimlerini kapatıyoruz (temiz bir sayfa için)
        for (int i = 0; i < keyPromptImages.Length; i++)
        {
            keyPromptImages[i].gameObject.SetActive(false);
        }

        // Eğer tetikleyiciden tuş resmi gönderildiyse, sırayla aktar ve görünür yap
        if (keySprites != null && keySprites.Length > 0)
        {
            for (int i = 0; i < keySprites.Length; i++)
            {
                // Eğer müfettişteki (Inspector) tuş sayısı, UI'daki Image sayısından fazlaysa hata vermesin diye koruma
                if (i >= keyPromptImages.Length) break; 

                if (keySprites[i] != null)
                {
                    keyPromptImages[i].sprite = keySprites[i];
                    keyPromptImages[i].gameObject.SetActive(true);
                }
            }
        }

        popupPanel.SetActive(true);
    }

    public void HideTutorial()
    {
        popupPanel.SetActive(false);
    }
}