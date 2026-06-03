using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Mouse olayları için bu kütüphane şart!

[System.Serializable]
public struct MenuButtonElements
{
    public string buttonName;
    public Button button;
    public GameObject selectionIndicator; // Butonun üstündeki oklu boru görseli
}

public class MenuSelectionManager : MonoBehaviour
{
    [Header("Buton ve Ok Eslesmeleri")]
    [SerializeField] private MenuButtonElements[] menuButtons;

    void Start()
    {
        // Başlangıçta tüm okları kapat, sadece ilk butonun okunu açık bırak (isteğe bağlı)
        HighlightButton(0);

        // Her buton için dinamik olarak Mouse Hover (Üzerine Gelme) olayı tanımlıyoruz
        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i; // C# closure kuralı

            if (menuButtons[i].button != null)
            {
                // Buton nesnesine dinamik olarak EventTrigger bileşeni ekliyoruz
                EventTrigger trigger = menuButtons[i].button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = menuButtons[i].button.gameObject.AddComponent<EventTrigger>();
                }

                // PointerEnter (Mouse üzerine geldiğinde) olayını oluşturma
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener((data) => { OnMouseHover(index); });
                
                // Oluşturduğumuz olayı butona bağlıyoruz
                trigger.triggers.Add(entry);
            }
        }
    }

    // Mouse bir butonun üzerine geldiğinde tetiklenen fonksiyon
    private void OnMouseHover(int targetIndex)
    {
        HighlightButton(targetIndex);
    }

    // Okları açıp kapatan ana mantık
    public void HighlightButton(int targetIndex)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i].selectionIndicator != null)
            {
                // Eğer döngüdeki indeks, mouse'un üzerindeki buton ise oku aç, değilse kapat
                bool isHovered = (i == targetIndex);
                menuButtons[i].selectionIndicator.SetActive(isHovered);
            }
        }
    }
}