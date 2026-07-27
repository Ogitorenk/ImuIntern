using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsManager : MonoBehaviour
{
    [Header("Sahne Geçişi")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Jenerik (Credits) Kaydırma Ayarları")]
    [Tooltip("Yazıların/Görsellerin bulunduğu UI Paneli (RectTransform)")]
    [SerializeField] private RectTransform creditsContainer; 
    [SerializeField] private float scrollSpeed = 60f;        // Normal kayma hızı
    [SerializeField] private float fastForwardSpeed = 240f;  // Tuşa basılı tutunca hızlanma çarpanı
    [Tooltip("Panelin Y koordinatı bu değere ulaştığında jenerik biter.")]
    [SerializeField] private float targetYPosition = 2500f;  

    [Header("Kararma (Fade) Ayarları")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1.5f;

    private bool isEnding = false;

    private void Start()
    {
        // Başlangıçta ekranı siyah yapıp yavaşça açıyoruz (Fade In)
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            StartCoroutine(FadeInRoutine());
        }
    }

    private void Update()
    {
        if (isEnding) return;

        // 1. Kayma Hızını Belirle (Space, Sol Tık veya E'ye basılı tutulursa hızlı akar)
        float currentSpeed = scrollSpeed;
        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0) || Input.GetKey(KeyCode.E))
        {
            currentSpeed = fastForwardSpeed;
        }

        // 2. Credits panelini yukarı doğru yavaşça kaydır
        if (creditsContainer != null)
        {
            creditsContainer.anchoredPosition += Vector2.up * currentSpeed * Time.deltaTime;

            // Belirlenen tepe noktasına ulaştıysa jeneriği bitir
            if (creditsContainer.anchoredPosition.y >= targetYPosition)
            {
                EndCredits();
            }
        }

        // 3. ESC tuşuna basılırsa direkt atla (Skip)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndCredits();
        }
    }

    public void EndCredits()
    {
        if (isEnding) return;
        isEnding = true;
        StartCoroutine(FadeOutAndLoadMenuRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
    }

    private IEnumerator FadeOutAndLoadMenuRoutine()
    {
        // Ekranı yavaşça karart (Fade Out)
        if (fadeCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(0.5f);

        // Main Menu'ye pürüzsüz geçiş yap
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}