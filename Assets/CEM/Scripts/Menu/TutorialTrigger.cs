using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial Settings")]
    [TextArea(3, 5)]
    [SerializeField] private string tutorialMessage;
    [SerializeField] private Sprite[] keyPromptSprites; 
    
    [Header("Logic")]
    [SerializeField] private bool showOnEnter = true;   // Alana girince gösterilsin mi?
    [SerializeField] private bool hideOnExit = true;    // Alandan çıkınca gizlensin mi?
    [SerializeField] private bool triggerOnlyOnce = true; // Sadece bir kez mi çalışsın?
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // Eğer sadece bir kez çalışacak şekilde ayarlandıysa ve zaten çalıştıysa engelle
            if (triggerOnlyOnce && hasTriggered) return;

            if (showOnEnter)
            {
                TutorialManager.Instance.ShowTutorial(tutorialMessage, keyPromptSprites);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // Oyuncu alandan çıktığında gizleme seçeneği aktifse UI'ı kapat
            if (hideOnExit)
            {
                TutorialManager.Instance.HideTutorial();
            }

            // Alandan çıkış yapıldığında, bu tetikleyicinin ilk kullanımı tamamlanmış sayılır
            if (triggerOnlyOnce)
            {
                hasTriggered = true;
            }
        }
    }
}