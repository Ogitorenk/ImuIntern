using UnityEngine;
using UnityEngine.SceneManagement;

public class ProgressionTrigger : MonoBehaviour
{
    [SerializeField] private GameProgressData progressionData;
    [SerializeField] private string sceneToLoadAfterPull = "CastleEntrance";

    // Bu fonksiyonu şalterin UnityEvent'ine bağlayacağız
    public void SetFirstGateOpen()
    {
        if (progressionData != null)
        {
            progressionData.isFirstIronGateOpen = true;
            progressionData.SaveToDisk(); // Diske kalıcı olarak yaz
            
            Debug.Log("<color=green>💾 İlerleme Kaydedildi: İlk Demir Kapı Açık!</color>");
            
            // Oyuncuyu ana sahneye geri gönder
            SceneManager.LoadScene(sceneToLoadAfterPull);
        }
    }
}