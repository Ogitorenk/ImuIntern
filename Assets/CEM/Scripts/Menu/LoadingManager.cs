using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 👈 Sahne geçiş dinleyicileri için bu kütüphane şart kanka

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject quillIcon; // Tasarımcının ikonunu buraya bağlıyoruz

    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    // 🔄 Sahne yüklendiğinde tetiklenecek olan event'leri (dinleyicileri) aktif ediyoruz
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 🎯 Hangi sahne yüklenirse yüklensin, o sahne tamamen ayağa kalktığında bu fonksiyon otomatik çalışır
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Yeni sahneye geçtiğimizde mor ekranın kapalı olduğundan kesin emin oluyoruz kanka
        if (loadingScreenPanel != null)
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
        // 1. Mor ekranı aç ve tüy ikonunu aktif et
        if (loadingScreenPanel != null) loadingScreenPanel.SetActive(true);
        if (quillIcon != null) quillIcon.SetActive(true);
        
        // Ağır yükleme başlamadan önce ikon 1 saniye pürüzsüzce dönsün (Görsel pürüzsüzlük hilesi)
        yield return new WaitForSecondsRealtime(1.0f);

        // 2. YÜKLEMEYİ BAŞLAT (Arka plan asenkron yüklemesi)
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // 3. KRİTİK HİLE: Yükleme bitti, şimdi sahneleri aktifleştireceğiz (Kilitlenme burada yaşanacak)
        // Kilitlenme başlamadan HEMEN ÖNCE ikonu kapatıyoruz ki donmuş bir ikon çirkinliği yaratmasın
        if (quillIcon != null) quillIcon.SetActive(false);
        yield return null; // İkonun kapandığını ekrana çizmesi için 1 kare bekle

        // 4. Sahneyi tetikle (Tüm Don Quixote / Sancho Awake ve Start lojikleri burada kilitlenecek ama ekranda temiz mor zemin var)
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        // 5. Yeni sahne tamamen uyanınca mor ekranı kapat (OnSceneLoaded fonksiyonu da bunu garantiye alacak)
        if (loadingScreenPanel != null) loadingScreenPanel.SetActive(false);
    }
}