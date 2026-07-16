using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightSectionNPC : MonoBehaviour
{
    [Header("Gereksinimler")]
    [SerializeField] private GameProgressData progressionData;
    [SerializeField] private RightSectionManager sectionManager;
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("1. DURUM: Şalterler Tamamlanmadan Önceki Diyalog")]
    [SerializeField] private List<DialogueLine> defaultDialogueSequence = new List<DialogueLine>();

    [Header("2. DURUM: Şalterler Tamamlandıktan Sonraki İlk Diyalog (Portal Açacak)")]
    [SerializeField] private List<DialogueLine> successDialogueSequence = new List<DialogueLine>();

    [Header("3. DURUM: Portal Açıldıktan Sonraki Tekrar Diyaloğu")]
    [SerializeField] private List<DialogueLine> repeatDialogueSequence = new List<DialogueLine>();

    [Header("Karakter Kontrolcü Etiketleri")]
    [SerializeField] private string donTag = "PlayerDon";
    [SerializeField] private string sanchoTag = "PlayerSancho";

    [Header("Etkileşim İpucu (UI)")]
    // NPC üzerindeki E tuşu ikonu/Canvas'ı
    [SerializeField] private GameObject interactionHintUI; 

    private bool isPlayerInRange = false;
    private bool isDialogueRunning = false;

    private void Update()
    {
        // Oyuncu alandaysa, etkileşim tuşuna (E) basarsa ve şu an aktif bir diyalog yoksa
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && !isDialogueRunning)
        {
            if (DialogueManager.Instance != null)
            {
                // Diyalog başladığı an etkileşim ipucunu (E tuşunu) gizle
                if (interactionHintUI != null)
                {
                    interactionHintUI.SetActive(false);
                }

                // Koşulları kontrol et ve uygun diyaloğu başlat
                StartDynamicDialogue();
            }
        }
    }

    private void StartDynamicDialogue()
    {
        // Konuşma başında güncel ilerleme durumunu diskten oku
        progressionData.LoadFromDisk();

        // Üç şalterin de çekilip çekilmediğini kontrol et
        bool allLeversPulled = progressionData.isUpForestLeverPulled && 
                               progressionData.isMazeLeverPulled && 
                               progressionData.isPitLeverPulled;

        List<DialogueLine> selectedSequence;
        bool isFirstTimeSuccess = false;

        if (allLeversPulled)
        {
            if (progressionData.isRightSectionNpcTalked)
            {
                // DURUM 3: Şalterler bitti, portal çoktan açıldı. Tekrar konuşması.
                selectedSequence = repeatDialogueSequence;
            }
            else
            {
                // DURUM 2: Şalterler yeni bitti ve ilk kez konuşuluyor. Portal açılacak!
                selectedSequence = successDialogueSequence;
                isFirstTimeSuccess = true;
            }
        }
        else
        {
            // DURUM 1: Şalterler henüz bitmedi. Oyuncuyu şalterlere yönlendir.
            selectedSequence = defaultDialogueSequence;
        }

        // Seçilen diyalog listesini DialogueManager'a göndererek başlat
        if (selectedSequence != null && selectedSequence.Count > 0)
        {
            isDialogueRunning = true;
            DialogueManager.Instance.StartInteractiveDialogue(selectedSequence, typingSpeed);
            
            // Diyaloğun bitmesini arka planda takip eden Coroutine'i başlatıyoruz
            StartCoroutine(WaitForDialogueEnd(isFirstTimeSuccess));
        }
    }

    // Diyaloğun bitişini izleyen takip mekanizması
    private IEnumerator WaitForDialogueEnd(bool triggerPortalAndSave)
    {
        // DialogueManager'ın durum değişkenini güncellemesi için 1 kare bekliyoruz
        yield return null;

        // DialogueManager'daki interaktif diyalog penceresi kapanana kadar burada bekle
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsInteractiveDialogueActive)
        {
            yield return null;
        }

        // Diyalog tamamen bitti ve arayüz kapandı!
        isDialogueRunning = false;

        // Eğer 2. Durum (Başarı) diyaloğu bittiyse portalları ve kaydı tetikle
        if (triggerPortalAndSave)
        {
            if (progressionData != null)
            {
                progressionData.isRightSectionNpcTalked = true;
                progressionData.SaveToDisk();
            }

            if (sectionManager != null)
            {
                sectionManager.SpawnPortal();
            }
        }

        // Oyuncu hala NPC'nin yanındaysa etkileşim ipucunu (E tuşunu) tekrar aç
        if (isPlayerInRange && interactionHintUI != null)
        {
            interactionHintUI.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(donTag) || other.CompareTag(sanchoTag))
        {
            isPlayerInRange = true;
            
            // Eğer o esnada aktif bir diyalog yürütülmüyorsa E tuşu ipucunu göster
            if (interactionHintUI != null && !isDialogueRunning)
            {
                interactionHintUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(donTag) || other.CompareTag(sanchoTag))
        {
            isPlayerInRange = false;
            
            if (interactionHintUI != null)
            {
                interactionHintUI.SetActive(false);
            }
        }
    }
}