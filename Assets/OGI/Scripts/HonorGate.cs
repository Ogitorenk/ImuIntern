using UnityEngine;
using UnityEngine.UI;
using TMPro; // --- YENÝ EKLENDÝ: Yazýyý deðiþtirmek için
using System.Collections;

public class HonorGate : MonoBehaviour
{
    [Header("Çift Kapý Ayarlarý")]
    [Tooltip("Sol kapýnýn menteþe objesi")]
    public Transform leftDoorHinge;
    [Tooltip("Sað kapýnýn menteþe objesi")]
    public Transform rightDoorHinge;

    [Tooltip("Sol kapý ne yöne açýlacak? (Genelde Y ekseninde -90)")]
    public Vector3 leftOpenRotation = new Vector3(0, -90f, 0);
    [Tooltip("Sað kapý ne yöne açýlacak? (Genelde Y ekseninde 90)")]
    public Vector3 rightOpenRotation = new Vector3(0, 90f, 0);
    public float doorOpenSpeed = 2f;

    [Header("Zorlanma (Clicker) Ayarlarý")]
    public float maxProgress = 100f;
    public float clickPower = 15f;
    public float decayRate = 25f;
    public KeyCode startKey = KeyCode.E; // Ýstersen Inspector'dan F yapabilirsin

    [Header("UI (Arayüz) Baðlantýlarý")]
    public GameObject miniGameCanvas;
    public Slider progressBar;
    // --- YENÝ EKLENDÝ: Canvas'taki o yazýyý buraya baðlayacaðýz ---
    public TextMeshProUGUI promptText;

    private bool isOpen = false;
    private bool isMiniGameActive = false;
    private bool playerInRange = false;
    private float currentProgress = 0f;

    private DonMovement donPlayer;

    void Start()
    {
        if (miniGameCanvas != null)
        {
            miniGameCanvas.SetActive(false);
        }

        if (progressBar != null)
        {
            progressBar.maxValue = maxProgress;
            progressBar.value = 0f;
        }
    }

    void Update()
    {
        if (isOpen) return;

        if (playerInRange && !isMiniGameActive && Input.GetKeyDown(startKey))
        {
            StartMiniGame();
        }

        if (isMiniGameActive)
        {
            // 1. Geri Düþme (Aðýrlýk)
            currentProgress -= decayRate * Time.deltaTime;

            // 2. Týklama Kontrolü (Sol Týk)
            if (Input.GetMouseButtonDown(0))
            {
                currentProgress += clickPower;
            }

            currentProgress = Mathf.Clamp(currentProgress, 0f, maxProgress);

            if (progressBar != null)
            {
                progressBar.value = currentProgress;
            }

            // 3. BAÞARI DURUMU
            if (currentProgress >= maxProgress)
            {
                FinishMiniGame();
            }
        }
    }

    private void StartMiniGame()
    {
        isMiniGameActive = true;
        currentProgress = 0f;

        if (donPlayer != null) donPlayer.enabled = false;

        // --- YAZIYI "SOL TIK SPAMLA" OLARAK DEÐÝÞTÝR ---
        if (promptText != null)
        {
            promptText.text = "SOL TIK SPAMLA!";
            promptText.color = Color.red;
        }

        if (miniGameCanvas != null) miniGameCanvas.SetActive(true);
    }

    private void FinishMiniGame()
    {
        isMiniGameActive = false;
        isOpen = true;

        // --- YAZIYI AÇILDI OLARAK DEÐÝÞTÝR ---
        if (promptText != null)
        {
            promptText.text = "KAPI PARCALANDI!";
            promptText.color = Color.green;
        }

        if (donPlayer != null) donPlayer.enabled = true;

        StartCoroutine(OpenDoubleDoorsRoutine());

        // Kapý açýldýktan 1.5 saniye sonra Canvas'ý tamamen kapat
        StartCoroutine(HideCanvasAfterDelay(1.5f));
    }

    private IEnumerator HideCanvasAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (miniGameCanvas != null) miniGameCanvas.SetActive(false);
    }

    private IEnumerator OpenDoubleDoorsRoutine()
    {
        Quaternion leftStartRot = leftDoorHinge != null ? leftDoorHinge.localRotation : Quaternion.identity;
        Quaternion rightStartRot = rightDoorHinge != null ? rightDoorHinge.localRotation : Quaternion.identity;

        Quaternion leftEndRot = Quaternion.Euler(leftOpenRotation);
        Quaternion rightEndRot = Quaternion.Euler(rightOpenRotation);

        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * doorOpenSpeed;

            if (leftDoorHinge != null)
                leftDoorHinge.localRotation = Quaternion.Slerp(leftStartRot, leftEndRot, elapsed);

            if (rightDoorHinge != null)
                rightDoorHinge.localRotation = Quaternion.Slerp(rightStartRot, rightEndRot, elapsed);

            yield return null;
        }

        if (leftDoorHinge != null) leftDoorHinge.localRotation = leftEndRot;
        if (rightDoorHinge != null) rightDoorHinge.localRotation = rightEndRot;

        this.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        DonMovement don = other.GetComponent<DonMovement>();
        if (don != null)
        {
            playerInRange = true;
            donPlayer = don;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<DonMovement>() != null)
        {
            playerInRange = false;
            donPlayer = null;

            if (isMiniGameActive)
            {
                isMiniGameActive = false;
                currentProgress = 0f;
                if (miniGameCanvas != null) miniGameCanvas.SetActive(false);
                if (donPlayer != null) donPlayer.enabled = true;
            }
        }
    }
}