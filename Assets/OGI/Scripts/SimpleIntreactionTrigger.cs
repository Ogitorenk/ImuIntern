using UnityEngine;
using TMPro; // Ekrana text bastıracağımız için kalmalı kanka
using System.Reflection; // Don veya Sancho scriptindeki 'isControlled' alanını bulmak için

public class SimpleInteractionTrigger : MonoBehaviour
{
    [Header("--- SENİN CANVAS BAĞLANTILARIN ---")]
    [Tooltip("Sahnede hazır duran o kapatıp açmak istediğin Canvas/Panel objesi")]
    public GameObject interactionPanel;
    [Tooltip("Panelin içindeki TextMeshPro text bileşeni")]
    public TextMeshProUGUI interactionText;

    [Header("--- İÇERİK AYARI ---")]
    [TextArea(3, 10)]
    [Tooltip("Bu kutunun içinden geçip tuşa basınca veya direkt girince ne yazsın kanka?")]
    public string messageToDisplay = "Buraya istediğin yazıyı yaz kanka...";

    [Header("--- AKTİVASYON MODU (YENİ) ---")]
    [Tooltip("Eğer bu tik açık olursa tuşa basınca açılır; kapatırsan alana girer girmez OTOMATİK açılır kanka!")]
    public bool useButtonAssignment = true;

    [Tooltip("Etkileşim tuşu (Yalnızca yukarıdaki tik açıkken çalışır)")]
    public KeyCode interactionKey = KeyCode.F;

    private bool isPlayerInside = false;
    private bool isUiActive = false;
    private MonoBehaviour currentPlayerScript = null; // Don veya Sancho'yu tutacak

    void Start()
    {
        // Oyun başında senin paneli zorla kapalı başlatıyoruz kanka
        if (interactionPanel != null) interactionPanel.SetActive(false);
    }

    void Update()
    {
        // Eğer tuş ataması aktifse klasik tuş kontrolünü yap kanka
        if (useButtonAssignment)
        {
            if (isPlayerInside && Input.GetKeyDown(interactionKey))
            {
                if (!isUiActive)
                {
                    OpenUI();
                }
                else
                {
                    CloseUI();
                }
            }
        }
        else
        {
            // Eğer tuş ataması KAPALIYSA ve UI şu an açık durumdaysa, oyuncu paneli kapatmak için yine tuşa basabilsin
            if (isPlayerInside && isUiActive && Input.GetKeyDown(interactionKey))
            {
                CloseUI();
            }
        }
    }

    private void OpenUI()
    {
        if (interactionPanel == null || interactionText == null) return;

        isUiActive = true;
        interactionText.text = messageToDisplay; // Yazıyı panele gömdük
        interactionPanel.SetActive(true);        // Paneli açtık

        SetPlayerControl(false); // Karakteri yazı bitene kadar dondurduk kanka
    }

    public void CloseUI()
    {
        if (interactionPanel == null) return;

        isUiActive = false;
        interactionPanel.SetActive(false); // Paneli kapattık

        SetPlayerControl(true); // Karakteri saldık, koşmaya devam!
    }

    private void SetPlayerControl(bool canControl)
    {
        if (currentPlayerScript == null) return;

        // System.Reflection kullanarak Don veya Sancho scriptindeki 'isControlled' alanını güvenle tetikliyoruz
        try
        {
            System.Type type = currentPlayerScript.GetType();
            FieldInfo field = type.GetField("isControlled");

            if (field != null)
            {
                field.SetValue(currentPlayerScript, canControl);
            }

            // Hız animasyonunda asılı kalmasınlar diye Animator kontrolü
            Animator anim = currentPlayerScript.GetComponentInChildren<Animator>();
            if (anim != null && !canControl)
            {
                anim.SetFloat("Speed", 0f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("🚨 Oyuncu kontrolü değiştirilirken hata oluştu kanka: " + e.Message);
        }
    }

    // --- TETİKLEYİCİ KONTROLLERİ ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;

            // İçeri giren Don mu Sancho mu hemen yakala
            var don = other.GetComponent<DonMovement>();
            if (don != null) currentPlayerScript = don;
            else
            {
                var sancho = other.GetComponent<SanchoMovement>();
                if (sancho != null) currentPlayerScript = sancho;
            }

            // --- YENİ EKLENDİ ---
            // Eğer tuş ataması İSTENMİYORSA, oyuncu kutuya adım attığı salise otomatik aç kanka!
            if (!useButtonAssignment && !isUiActive)
            {
                OpenUI();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;

            // Oyuncu yazıyı kapatmadan kutudan çıkarsa UI'ı zorla temizle kanka
            if (isUiActive)
            {
                CloseUI();
            }

            currentPlayerScript = null;
        }
    }

    // Editörde kutuyu rahat seçebilmemiz için tatlı mavi bir tel kafes çiziyoruz kanka
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}