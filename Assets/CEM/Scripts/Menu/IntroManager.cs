using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroManager : MonoBehaviour
{
    // Müfettişte (Inspector) her sprite ve ona bağlı yazıları gruplamak için bir yapı
    [System.Serializable]
    public struct IntroSlide
    {
        public Sprite sprite; // Gösterilecek çizim
        [TextArea(2, 5)]
        public List<string> dialogueTexts; // Bu çizim ekrandayken sırayla çıkacak yazılar
    }

    [Header("UI Elementleri")]
    [SerializeField] private Image introImage;
    [SerializeField] private TextMeshProUGUI introText;

    [Header("Giriş İçeriği")]
    [SerializeField] private List<IntroSlide> introSlides; // Sprite ve yazı grupları

    [Header("Ayarlar")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float textSpeed = 0.05f;
    [SerializeField] private string firstGameplaySceneName;

    private bool canProceed = false;

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
        // Tıklama algılama
        if (canProceed && (Input.GetMouseButtonDown(0) || Input.anyKeyDown))
        {
            canProceed = false; 
        }
    }

    private IEnumerator PlayIntroRoutine()
    {
        foreach (IntroSlide slide in introSlides)
        {
            // 1. Resmi Hazırla ve Görünmez Yap
            introText.text = "";
            Color c = introImage.color;
            c.a = 0f;
            introImage.color = c;
            introImage.sprite = slide.sprite;

            // 2. Sadece Resim Değiştiğinde Fade-In Yap
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

            // 3. Bu Resme Ait Tüm Yazıları Sırayla Oynat
            foreach (string currentText in slide.dialogueTexts)
            {
                introText.text = ""; // Yeni yazı gelmeden önce eskiyi temizle

                // Daktilo Efekti
                for (int j = 0; j <= currentText.Length; j++)
                {
                    introText.text = currentText.Substring(0, j);
                    yield return new WaitForSeconds(textSpeed);
                }

                // Yazı bitti, oyuncunun tıklamasını bekle
                canProceed = true;
                yield return new WaitUntil(() => !canProceed);
            }
            
            // Bu sprite'a ait tüm yazılar bitti, döngü başa dönecek ve yeni sprite'a geçecek.
        }

        // Her şey bittiğinde yeni sahneye geç
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(firstGameplaySceneName);
    }
}