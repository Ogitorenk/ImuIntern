using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    // Singleton Tanımı (Her yerden kolayca erişmek için)
    public static DialogueManager Instance { get; private set; }

    [Header("Don Quixote UI Elemanları")]
    [SerializeField] private GameObject donPanel;
    [SerializeField] private TextMeshProUGUI donText;

    [Header("Sancho Panza UI Elemanları")]
    [SerializeField] private GameObject sanchoPanel;
    [SerializeField] private TextMeshProUGUI sanchoText;

    private Coroutine currentDialogueCoroutine;

    private void Awake()
    {
        // Singleton kurulumu
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Yeni bir diyalog tetiklendiğinde çağrılacak ana fonksiyon
    public void StartDialogueSequence(List<DialogueLine> dialogueSequence, float typingSpeed, float delayBetweenLines)
    {
        // EĞER HALİHAZIRDA ÇALIŞAN BİR DİYALOG VARSA ANINDA DURDUR (Sorunu çözen kısım!)
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
        }

        // Panelleri temizle ve yeni seriyi başlat
        donPanel.SetActive(false);
        sanchoPanel.SetActive(false);
        
        currentDialogueCoroutine = StartCoroutine(PlayDialogueSequence(dialogueSequence, typingSpeed, delayBetweenLines));
    }

    private IEnumerator PlayDialogueSequence(List<DialogueLine> dialogueSequence, float typingSpeed, float delayBetweenLines)
    {
        foreach (DialogueLine line in dialogueSequence)
        {
            GameObject activePanel;
            TextMeshProUGUI activeText;

            if (line.speaker == CharacterType.DonQuixote)
            {
                activePanel = donPanel;
                activeText = donText;
                sanchoPanel.SetActive(false); 
            }
            else
            {
                activePanel = sanchoPanel;
                activeText = sanchoText;
                donPanel.SetActive(false); 
            }

            activeText.text = "";
            activePanel.SetActive(true);

            foreach (char letter in line.text.ToCharArray())
            {
                activeText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }

            yield return new WaitForSeconds(delayBetweenLines);
        }

        donPanel.SetActive(false);
        sanchoPanel.SetActive(false);
        currentDialogueCoroutine = null;
    }
}