using System.Collections.Generic;
using UnityEngine;
// UI elemanlarını kontrol etmek için gerekli kütüphane
using UnityEngine.UI; 

public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("NPC Diyalog Ayarları")]
    [SerializeField] private List<DialogueLine> dialogueSequence = new List<DialogueLine>();
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("Karakter Kontrolcü Etiketleri")]
    [SerializeField] private string donTag = "PlayerDon";
    [SerializeField] private string sanchoTag = "PlayerSancho";

    [Header("Etkileşim İpucu (UI)")]
    // Buraya NPC'nin üstünde duran E tuşu ikonunun bulunduğu Canvas'ı veya Image'ı sürükle.
    // Başlangıçta bu objeyi Inspector'da pasif yapmalısın.
    [SerializeField] private GameObject interactionHintUI; 

    private bool isPlayerInRange = false;

    private void Update()
    {
        // Oyuncu alandaysa ve Etkileşim tuşuna (E) basarsa
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (DialogueManager.Instance != null)
            {
                // Diyalog başladığında ipucunu gizle
                interactionHintUI.SetActive(false);

                // Diyaloğu başlat (Artık oyunu durduran interaktif fonksiyonu çağırıyor)
                DialogueManager.Instance.StartInteractiveDialogue(dialogueSequence, typingSpeed);
                
                // NPC ile sadece 1 kere konuşulsun istiyorsan alttaki satırı aktif et:
                // this.enabled = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Gelen objenin Don veya Sancho etiketli olup olmadığını kontrol et
        if (other.CompareTag(donTag) || other.CompareTag(sanchoTag))
        {
            isPlayerInRange = true;
            
            // Oyuncu alana girince E tuşu ipucunu göster
            if (interactionHintUI != null)
            {
                interactionHintUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Gelen objenin Don veya Sancho etiketli olup olmadığını kontrol et
        if (other.CompareTag(donTag) || other.CompareTag(sanchoTag))
        {
            isPlayerInRange = false;
            
            // Oyuncu alandan çıkınca E tuşu ipucunu gizle
            if (interactionHintUI != null)
            {
                interactionHintUI.SetActive(false);
            }
        }
    }
}