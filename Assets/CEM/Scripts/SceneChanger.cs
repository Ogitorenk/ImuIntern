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
        // (Eğer karakterinin Tag'i 'Player' değilse kontrol etmelisin)
        if (other.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log(targetSceneName + " sahnesine geçiliyor...");
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogWarning("Hedef sahne adı boş bırakılmış!");
            }
        }
    }
}