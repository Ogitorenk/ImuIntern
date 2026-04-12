using UnityEngine;
using TMPro;
using System.Collections;

public class LeverFixing : MonoBehaviour
{
    [Header("Tamir Ayarları")]
    public int requiredSuccesses = 5; // Kaç kere doğru tuşa basması lazım?
    public float timePerKey = 1.5f;   // Her tuş için kaç saniyesi var?
    public KeyCode startKey = KeyCode.E; // Tamiri başlatma tuşu

    [Header("UI (Arayüz) Bağlantıları")]
    public GameObject miniGameCanvas;
    public TextMeshProUGUI promptText;

    [Header("Görsel Efektler")]
    [Tooltip("Bozuk haldeyken duman çıkaracak Particle System objesini buraya sürükle")]
    public ParticleSystem smokeParticles;

    private UniversalLever originalLever;
    private SanchoMovement sanchoPlayer;

    private bool isBroken = true;
    private bool miniGameActive = false;
    private bool playerInRange = false;

    private int currentSuccesses = 0;
    private KeyCode currentTargetKey;
    private bool isWaitingForKey = false;

    private KeyCode[] possibleKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    private string[] possibleKeyTexts = { "W", "A", "S", "D" };

    void Start()
    {
        originalLever = GetComponent<UniversalLever>();
        if (originalLever != null && isBroken)
        {
            originalLever.enabled = false;
        }

        if (miniGameCanvas != null)
        {
            miniGameCanvas.SetActive(false);
        }

        if (isBroken && smokeParticles != null)
        {
            smokeParticles.Play();
        }
    }

    void Update()
    {
        if (isBroken && playerInRange && !miniGameActive && Input.GetKeyDown(startKey))
        {
            StartCoroutine(StartMiniGame());
        }

        if (miniGameActive && isWaitingForKey)
        {
            CheckPlayerInput();
        }
    }

    private IEnumerator StartMiniGame()
    {
        miniGameActive = true;
        currentSuccesses = 0;

        if (sanchoPlayer != null)
        {
            // --- YENİ EKLENDİ: KARAKTERİ IDLE'A ZORLA ---
            // Fişi çekmeden önce Animator'a "Hızımız sıfır, dur!" diyoruz ki moonwalk yapmasın.
            Animator sanchoAnim = sanchoPlayer.GetComponentInChildren<Animator>();
            if (sanchoAnim != null)
            {
                sanchoAnim.SetFloat("Speed", 0f);
            }

            // Şimdi fişi çekip karakteri yerine çivileyebiliriz
            sanchoPlayer.enabled = false;
        }

        if (miniGameCanvas != null) miniGameCanvas.SetActive(true);

        promptText.text = "TAMIR BASLIYOR!";
        promptText.color = Color.yellow;
        yield return new WaitForSeconds(1.5f);

        while (currentSuccesses < requiredSuccesses)
        {
            int randomIndex = Random.Range(0, possibleKeys.Length);
            currentTargetKey = possibleKeys[randomIndex];

            promptText.text = possibleKeyTexts[randomIndex];
            promptText.color = Color.white;

            isWaitingForKey = true;

            float timer = 0f;
            while (timer < timePerKey && isWaitingForKey)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (!miniGameActive)
            {
                yield break;
            }

            if (isWaitingForKey)
            {
                FailMiniGame("COK YAVAS!");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        FinishTamir();
    }

    private void CheckPlayerInput()
    {
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(currentTargetKey))
            {
                currentSuccesses++;
                promptText.text = "GUZEL!";
                promptText.color = Color.green;
                isWaitingForKey = false;
            }
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
                     Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
            {
                FailMiniGame("YANLIS TUS!");
            }
        }
    }

    private void FailMiniGame(string reason)
    {
        isWaitingForKey = false;
        miniGameActive = false;

        promptText.text = reason + "\nBASA SARDIN!";
        promptText.color = Color.red;

        if (sanchoPlayer != null) sanchoPlayer.enabled = true;

        StartCoroutine(HideCanvasAfterDelay(2f));
    }

    private void FinishTamir()
    {
        isWaitingForKey = false;
        miniGameActive = false;
        isBroken = false;

        promptText.text = "SALTER TAMIR EDILDI!";
        promptText.color = Color.green;

        if (smokeParticles != null)
        {
            smokeParticles.Stop();
        }

        if (sanchoPlayer != null) sanchoPlayer.enabled = true;

        if (originalLever != null)
        {
            originalLever.enabled = true;
        }

        StartCoroutine(HideCanvasAfterDelay(2f));

        this.enabled = false;
    }

    private IEnumerator HideCanvasAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (miniGameCanvas != null) miniGameCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        SanchoMovement sancho = other.GetComponent<SanchoMovement>();
        if (sancho != null)
        {
            playerInRange = true;
            sanchoPlayer = sancho;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<SanchoMovement>() != null)
        {
            playerInRange = false;
            sanchoPlayer = null;
        }
    }
}