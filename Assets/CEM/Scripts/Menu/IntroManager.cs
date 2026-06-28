using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [System.Serializable]
    public struct IntroSlide
    {
        public Sprite sprite;
        [TextArea(2, 5)]
        public List<string> dialogueTexts;
    }

    [Header("UI Elementleri")]
    [SerializeField] private Image introImage;
    [SerializeField] private TextMeshProUGUI introText;

    [Header("Giriş İçeriği")]
    [SerializeField] private List<IntroSlide> introSlides;

    [Header("Ayarlar")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float textSpeed = 0.05f;
    [SerializeField] private string firstGameplaySceneName;

    private bool isTextWriting = false; // Yazının şu an yazılmakta olduğunu takip eder
    private bool skipRequested = false;  // Oyuncunun hızlı geçmek isteyip istemediğini tutar
    private bool canProceed = false;     // Sonraki yazıya geçiş izni

    private void Start()
    {
        if (introSlides != null && introSlides.Count > 0 && introImage != null)
        {
            StartCoroutine(PlayIntroRoutine());
        }
        else
        {
            LoadNextScene();
        }
    }

    private void Update()
    {
        // Sol tık veya herhangi bir tuş basıldıysa
        if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
        {
            if (isTextWriting)
            {
                // 1. Durum: Yazı hala yazılıyorsa, yazmayı hızlandır/atla
                skipRequested = true;
            }
            else if (canProceed)
            {
                // 2. Durum: Yazı tamamen bittiyse, sonraki cümleye geç
                canProceed = false;
            }
        }
    }

    private IEnumerator PlayIntroRoutine()
    {
        foreach (IntroSlide slide in introSlides)
        {
            introText.text = "";
            Color c = introImage.color;
            c.a = 0f;
            introImage.color = c;
            introImage.sprite = slide.sprite;

            // Fade-In
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                introImage.color = c;
                yield return null;
            }
            c.a = 1f;
            introImage.color = c;

            // Slayttaki yazıları sırayla oynat
            foreach (string currentText in slide.dialogueTexts)
            {
                introText.text = "";
                isTextWriting = true;
                skipRequested = false;

                // Daktilo Efekti Döngüsü
                for (int j = 0; j <= currentText.Length; j++)
                {
                    // Eğer oyuncu tıkladıysa döngüden çık ve metnin tamamını yaz
                    if (skipRequested)
                    {
                        introText.text = currentText;
                        break;
                    }

                    introText.text = currentText.Substring(0, j);
                    yield return new WaitForSeconds(textSpeed);
                }

                // Yazma işlemi bitti
                isTextWriting = false;
                
                // Oyuncunun bir sonraki adıma geçmek için tekrar tıklamasını bekle
                yield return new WaitedFrameFixed(); // Tıklamanın hemen algılanıp sonraki yazıyı da geçmemesi için küçük bir güvenlik payı
                canProceed = true;
                yield return new WaitUntil(() => !canProceed);
            }
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(firstGameplaySceneName);
    }
}

// Küçük bir senkronizasyon yardımı (Aynı karede iki tıklama algılanmasın diye)
public class WaitedFrameFixed : CustomYieldInstruction
{
    private int targetFrame;
    public override bool keepWaiting => Time.frameCount < targetFrame;
    public WaitedFrameFixed() => targetFrame = Time.frameCount + 2;
}