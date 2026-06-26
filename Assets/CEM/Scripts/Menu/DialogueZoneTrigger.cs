using System.Collections.Generic;
using UnityEngine;

// Eğer eski scriptinde enum ve struct duruyorsa kalabilir, yoksa buraya ekle:
public enum CharacterType { DonQuixote, SanchoPanza }

[System.Serializable]
public struct DialogueLine
{
    public CharacterType speaker;
    [TextArea(3, 5)] public string text;
}

public class DialogueZoneTrigger : MonoBehaviour
{
    [Header("Diyalog Ayarları")]
    [SerializeField] private List<DialogueLine> dialogueSequence = new List<DialogueLine>();
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float delayBetweenLines = 3.0f;

    [Header("Karakter Kontrolcü Etiketleri")]
    [SerializeField] private string donTag = "PlayerDon";
    [SerializeField] private string sanchoTag = "PlayerSancho";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(donTag) || other.CompareTag(sanchoTag))
        {
            hasTriggered = true; // Bu alanın bir daha tetiklenmesini engeller

            // Merkezi yöneticiye "Eskisini sil, benim diyaloglarımı oynat" emri veriyoruz
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogueSequence(dialogueSequence, typingSpeed, delayBetweenLines);
            }
        }
    }
}