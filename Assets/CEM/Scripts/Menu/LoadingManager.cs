using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject quillIcon; // Tasarımcının ikonunu buraya bağla

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 1. Mor ekranı aç ve ikonu aktif et
        loadingScreenPanel.SetActive(true);
        if(quillIcon != null) quillIcon.SetActive(true);
        
        // Ağır yükleme başlamadan önce ikon 1 saniye pürüzsüzce dönsün
        yield return new WaitForSecondsRealtime(1.0f);

        // 2. YÜKLEMEYİ BAŞLAT (Arka plan yüklemesi)
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // 3. KRİTİK HİLE: Yükleme bitti, şimdi sahneleri aktifleştireceğiz (Kilitlenme burada yaşanacak)
        // Kilitlenme başlamadan HEMEN ÖNCE ikonu kapatıyoruz ki donmuş bir ikon çirkinliği yaratmasın
        if(quillIcon != null) quillIcon.SetActive(false);
        yield return null; // İkonun kapandığını çizmesi için 1 kare bekle

        // 4. Sahneyi tetikle (Tüm Awake/Start lojikleri burada kilitlenecek ama ekranda sadece temiz mor zemin var)
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        // 5. Yeni sahne tamamen uyanınca mor ekranı kapat
        loadingScreenPanel.SetActive(false);
    }
}