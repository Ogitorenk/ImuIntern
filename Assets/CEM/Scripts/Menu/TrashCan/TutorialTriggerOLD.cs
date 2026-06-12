using UnityEngine;

public class TutorialTriggerOLD : MonoBehaviour
{
    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;

    [Header("Settings")]
    [SerializeField] private bool showOnEnter = true;
    [SerializeField] private bool hideOnExit = false;
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (triggerOnlyOnce && hasTriggered)
            return;

        if (showOnEnter && tutorialPanel != null)
            tutorialPanel.SetActive(true);

        hasTriggered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (hideOnExit && tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }
}