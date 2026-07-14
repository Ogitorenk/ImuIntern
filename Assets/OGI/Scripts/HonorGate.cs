using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class HonorGate : MonoBehaviour
{
    [Header("Çift Kapı Ayarları")]
    [Tooltip("Sol kapının menteşe objesi")]
    public Transform leftDoorHinge;
    [Tooltip("Sağ kapının menteşe objesi")]
    public Transform rightDoorHinge;

    [Tooltip("Sol kapı ne yöne açılacak? (Genelde Y ekseninde -90)")]
    public Vector3 leftOpenRotation = new Vector3(0, -90f, 0);
    [Tooltip("Sağ kapı ne yöne açılacak? (Genelde Y ekseninde 90)")]
    public Vector3 rightOpenRotation = new Vector3(0, 90f, 0);
    public float doorOpenSpeed = 2f;

    [Header("Zorlanma (Clicker) Ayarları")]
    public float maxProgress = 100f;
    public float clickPower = 15f;
    public float decayRate = 25f;
    public KeyCode startKey = KeyCode.E; 

    [Header("Metin Ayarları (Text Settings)")]
    public string textSpam = "SPAM LEFT CLICK!";
    public string textSuccess = "GATE BROKEN!";

    [Header("Görsel Ayarlar (Icon Settings)")]
    [Tooltip("Ekranda basılıp basılmadığını gösterecek Image objesi")]
    public Image clickFeedbackImage;
    [Tooltip("Sol tık BASILMADIĞINDA görünecek Sprite")]
    public Sprite unpressedSprite;
    [Tooltip("Sol tık BASILDIĞINDA görünecek Sprite")]
    public Sprite pressedSprite;

    [Header("UI (Arayüz) Bağlantıları")]
    public GameObject miniGameCanvas;
    public Slider progressBar;
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
            // 1. Geri Düşme (Ağırlık)
            currentProgress -= decayRate * Time.deltaTime;

            // 2. Tıklama Kontrolü ve Sprite Değişimi
            if (Input.GetMouseButtonDown(0))
            {
                currentProgress += clickPower;
                
                // Tıklandığı an basılı sprite'a geç
                if (clickFeedbackImage != null && pressedSprite != null)
                {
                    clickFeedbackImage.sprite = pressedSprite;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                // Tıklama bırakıldığında normal sprite'a dön
                if (clickFeedbackImage != null && unpressedSprite != null)
                {
                    clickFeedbackImage.sprite = unpressedSprite;
                }
            }

            currentProgress = Mathf.Clamp(currentProgress, 0f, maxProgress);

            if (progressBar != null)
            {
                progressBar.value = currentProgress;
            }

            // 3. BAŞARI DURUMU
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

        if (promptText != null)
        {
            promptText.text = textSpam;
            // RENKLENDİRME KALDIRILDI (Eski satır: promptText.color = Color.red;)
        }

        // Mini oyun başlarken ikonun varsayılan (basılmamış) halini ayarla
        if (clickFeedbackImage != null && unpressedSprite != null)
        {
            clickFeedbackImage.sprite = unpressedSprite;
        }

        if (miniGameCanvas != null) miniGameCanvas.SetActive(true);
    }

    private void FinishMiniGame()
    {
        isMiniGameActive = false;
        isOpen = true;

        if (promptText != null)
        {
            promptText.text = textSuccess;
            // RENKLENDİRME KALDIRILDI (Eski satır: promptText.color = Color.green;)
        }

        // Başarılı olduğunda basılmamış sprite'a geri döndür (takılı kalmasın)
        if (clickFeedbackImage != null && unpressedSprite != null)
        {
            clickFeedbackImage.sprite = unpressedSprite;
        }

        if (donPlayer != null) donPlayer.enabled = true;

        StartCoroutine(OpenDoubleDoorsRoutine());

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