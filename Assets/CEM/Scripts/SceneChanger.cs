using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için gerekli kütüphane

public class SceneChanger : MonoBehaviour
{
    [Header("Sahne Ayarları")]
    [Tooltip("Geçiş yapılacak sahnenin tam adını buraya yazın.")]
    public string targetSceneName;

    // Tetikleyiciye bir şey girdiğinde çalışır
    private void OnTriggerEnter(Collider other)
    {
        // Sadece 'Player' etiketli obje girdiğinde sahne değişsin
        if (other.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log(targetSceneName + " sahnesine LoadingManager ile pürüzsüz geçiş yapılıyor...");

                // 🎯 [GÜNCELLENDİ] Direkt yüklemek yerine LoadingManager'ı tetikliyoruz
                if (LoadingManager.Instance != null)
                {
                    LoadingManager.Instance.LoadScene(targetSceneName);
                }
                else
                {
                    // Güvenlik Önlemi: Eğer sahnede bağımsız test yapıyorsan ve LoadingManager yoksa oyun donmasın kanka
                    SceneManager.LoadScene(targetSceneName);
                }
            }
            else
            {
                Debug.LogWarning("Hedef sahne adı boş bırakılmış!");
            }
        }
    }
}