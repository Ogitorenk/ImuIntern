using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum CharacterType { DonQuixote, SanchoPanza, NPC }

[System.Serializable]
public struct DialogueLine
{
    public CharacterType speaker;
    [TextArea(3, 5)] public string text;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Don Quixote UI Elemanları")]
    [SerializeField] private GameObject donPanel;
    [SerializeField] private TextMeshProUGUI donText;

    [Header("Sancho Panza UI Elemanları")]
    [SerializeField] private GameObject sanchoPanel;
    [SerializeField] private TextMeshProUGUI sanchoText;

    [Header("NPC UI Elemanları")]
    [SerializeField] private GameObject npcPanel;
    [SerializeField] private TextMeshProUGUI npcText;

    [Header("Karakter Etiketleri (Hareketi Durdurmak İçin)")]
    [SerializeField] private string donTag = "Player";
    [SerializeField] private string sanchoTag = "Player";

    private Coroutine currentDialogueCoroutine;
    public bool IsInteractiveDialogueActive => isInteractiveDialogueActive;
    private bool isInteractiveDialogueActive = false;
    private bool isTyping = false;
    private bool skipTyping = false;
    private bool continueToNextLine = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (isInteractiveDialogueActive)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (isTyping)
                {
                    skipTyping = true;
                }
                else
                {
                    continueToNextLine = true;
                }
            }
        }
    }

    // ESKİ SİSTEM: Otomatik diyaloglar
    public void StartDialogueSequence(List<DialogueLine> dialogueSequence, float typingSpeed, float delayBetweenLines)
    {
        if (currentDialogueCoroutine != null) StopCoroutine(currentDialogueCoroutine);

        HideAllPanels();
        currentDialogueCoroutine = StartCoroutine(PlayDialogueSequence(dialogueSequence, typingSpeed, delayBetweenLines));
    }

    // YENİ SİSTEM: Etkileşimli diyaloglar (Burada hareketleri kapatıyoruz)
    public void StartInteractiveDialogue(List<DialogueLine> dialogueSequence, float typingSpeed)
    {
        if (currentDialogueCoroutine != null) StopCoroutine(currentDialogueCoroutine);

        HideAllPanels();

        // HAREKETLERİ DURDUR
        SetCharactersMovementState(false);
        if (DualRealityManager.Instance != null)
        {
            DualRealityManager.Instance.canSwitch = false;
        }

        isInteractiveDialogueActive = true;
        currentDialogueCoroutine = StartCoroutine(PlayInteractiveSequence(dialogueSequence, typingSpeed));
    }

    private IEnumerator PlayDialogueSequence(List<DialogueLine> dialogueSequence, float typingSpeed, float delayBetweenLines)
    {
        foreach (DialogueLine line in dialogueSequence)
        {
            GameObject activePanel = GetPanelForCharacter(line.speaker);
            TextMeshProUGUI activeText = GetTextForCharacter(line.speaker);

            HideAllPanels();
            activeText.text = "";
            activePanel.SetActive(true);

            foreach (char letter in line.text.ToCharArray())
            {
                activeText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }

            yield return new WaitForSeconds(delayBetweenLines);
        }

        HideAllPanels();
        currentDialogueCoroutine = null;
    }

    private IEnumerator PlayInteractiveSequence(List<DialogueLine> dialogueSequence, float typingSpeed)
    {
        foreach (DialogueLine line in dialogueSequence)
        {
            GameObject activePanel = GetPanelForCharacter(line.speaker);
            TextMeshProUGUI activeText = GetTextForCharacter(line.speaker);

            HideAllPanels();
            activeText.text = "";
            activePanel.SetActive(true);

            isTyping = true;
            skipTyping = false;
            continueToNextLine = false;

            foreach (char letter in line.text.ToCharArray())
            {
                if (skipTyping)
                {
                    activeText.text = line.text;
                    break;
                }
                activeText.text += letter;
                // Artık oyun durmadığı için normal WaitForSeconds kullanabiliriz
                yield return new WaitForSeconds(typingSpeed); 
            }

            isTyping = false;
            skipTyping = false;

            yield return new WaitUntil(() => continueToNextLine);
        }

        HideAllPanels();
        
        // HAREKETLERİ TEKRAR AÇ
        SetCharactersMovementState(true);
        if (DualRealityManager.Instance != null)
        {
            DualRealityManager.Instance.canSwitch = true;
        }

        isInteractiveDialogueActive = false;
        currentDialogueCoroutine = null;
    }

    // --- Karakter Hareketlerini Kapatıp Açan Fonksiyon ---
    private void SetCharactersMovementState(bool state)
{
    // Sahnedeki "Player" etiketine sahip TÜM objeleri bulur (Don ve Sancho'yu)
    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

    foreach (GameObject player in players)
    {
        if (player != null)
        {
            // Don'un hareket scriptini kapatmayı dene
            var donMove = player.GetComponent("DonMovement") as MonoBehaviour;
            if (donMove != null) donMove.enabled = state;

            // Sancho'un hareket scriptini kapatmayı dene
            var sanchoMove = player.GetComponent("SanchoMovement") as MonoBehaviour;
            if (sanchoMove != null) sanchoMove.enabled = state;

            // Fiziksel hareketleri sıfırla
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}

    private GameObject GetPanelForCharacter(CharacterType type)
    {
        if (type == CharacterType.DonQuixote) return donPanel;
        if (type == CharacterType.SanchoPanza) return sanchoPanel;
        return npcPanel;
    }

    private TextMeshProUGUI GetTextForCharacter(CharacterType type)
    {
        if (type == CharacterType.DonQuixote) return donText;
        if (type == CharacterType.SanchoPanza) return sanchoText;
        return npcText;
    }

    private void HideAllPanels()
    {
        donPanel.SetActive(false);
        sanchoPanel.SetActive(false);
        if (npcPanel) npcPanel.SetActive(false);
    }
}