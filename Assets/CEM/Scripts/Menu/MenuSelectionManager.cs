using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public struct MenuButtonElements
{
    public string buttonName;
    public Button button;
    public GameObject selectionIndicator;
}

public class MenuSelectionManager : MonoBehaviour
{
    [Header("Buton ve Ok Eslesmeleri")]
    [SerializeField] private MenuButtonElements[] menuButtons;

    void Start()
    {
        // Dinamik EventTrigger atamaları
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

                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener((data) => { OnMouseHover(index); });
                
                trigger.triggers.Add(entry);
            }
        }

        // İlk açılış vurgulaması
        ResetSelection();
    }

    // Panel her SetActive(true) olduğunda tetiklenir
    private void OnEnable()
    {
        ResetSelection();
    }

    private void OnMouseHover(int targetIndex)
    {
        HighlightButton(targetIndex);
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

    // YENİ: Menü sıfırlandığında veya paneller arası geçişte ilk butonu seçmek için
    public void ResetSelection()
    {
        if (menuButtons != null && menuButtons.Length > 0)
        {
            HighlightButton(0);
        }
    }
}