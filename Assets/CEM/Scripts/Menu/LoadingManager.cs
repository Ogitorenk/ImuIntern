using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private CanvasGroup loadingCanvasGroup; // 👈 Müdahale edeceğimiz Canvas Group bileşeni
    [SerializeField] private GameObject quillIcon; 

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f; // 👈 Geçiş efektinin kaç saniye süreceği

    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject); 

            // İlk sahne açıldığında eğer panel görünüyorsa yumuşakça açılmasını tetikle
            if (loadingCanvasGroup != null && loadingScreenPanel != null)
            {
                loadingScreenPanel.SetActive(true);
                StartCoroutine(FadeIn());
            }
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // NOT: Sahne yüklendiğinde artık paneli küt diye kapatmıyoruz!
        // LoadSceneAsync coroutine'i sahne açıldıktan sonra zaten yumuşakça kapatacak (FadeIn).
        // Ama bir aksilik olur da coroutine yarıda kalırsa diye bir güvenlik önlemi:
        if (loadingScreenPanel != null && loadingCanvasGroup == null)
        {
            loadingScreenPanel.SetActive(false);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 1. Ekranı yavaşça karart (Şeffaftan mor ekrana geçiş)
        if (loadingScreenPanel != null) loadingScreenPanel.SetActive(true);
        yield return StartCoroutine(FadeOut());

        // 2. Tüy ikonunu aktif et
        if (quillIcon != null) quillIcon.SetActive(true);
        
        // Ağır yükleme başlamadan önce ikon 1 saniye pürüzsüzce dönsün
        yield return new WaitForSecondsRealtime(1.0f);

        // 3. YÜKLEMEYİ BAŞLAT (Arka plan asenkron yüklemesi)
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // 4. KRİTİK HİLE: İkon donmasın diye kapatıyoruz
        if (quillIcon != null) quillIcon.SetActive(false);
        yield return null; 

        // 5. Yeni sahneyi aktifleştir (Sahneler arka planda uyanacak)
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        // 6. Yeni sahne tamamen hazır! Şimdi mor ekranı yumuşakça kaldırıyoruz (Mordan şeffafa)
        yield return StartCoroutine(FadeIn());

        // Tamamen şeffaf olunca paneli kapat ki arkadaki nesnelere tıklanabilsin
        if (loadingScreenPanel != null) loadingScreenPanel.SetActive(false);
    }

    // --- FADE COROUTINE'LERİ ---

    // Ekranı açar (Mor ekrandan oyuna yumuşak geçiş)
    private IEnumerator FadeIn()
    {
        if (loadingCanvasGroup == null) yield break;

        loadingCanvasGroup.blocksRaycasts = true; // Geçiş bitene kadar tıklamaları engelle
        loadingCanvasGroup.alpha = 1f;

        while (loadingCanvasGroup.alpha > 0f)
        {
            loadingCanvasGroup.alpha -= Time.deltaTime / fadeDuration;
            yield return null;
        }

        loadingCanvasGroup.alpha = 0f;
        loadingCanvasGroup.blocksRaycasts = false; // Oyun başlayınca tıklamaları aç
    }

    // Ekranı karartır (Oyundan mor ekrana yumuşak geçiş)
    private IEnumerator FadeOut()
    {
        if (loadingCanvasGroup == null) yield break;

        loadingCanvasGroup.blocksRaycasts = true; // Kararırken arkadaki butonları kilitle
        loadingCanvasGroup.alpha = 0f;

        while (loadingCanvasGroup.alpha < 1f)
        {
            loadingCanvasGroup.alpha += Time.deltaTime / fadeDuration;
            yield return null;
        }

        loadingCanvasGroup.alpha = 1f;
    }
}