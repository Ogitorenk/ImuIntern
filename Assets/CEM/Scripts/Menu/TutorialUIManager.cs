using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image keyIcon;
    [SerializeField] private TMP_Text descriptionText;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(Sprite keySprite, string description)
    {
        keyIcon.sprite = keySprite;
        descriptionText.text = description;

        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}