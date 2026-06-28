using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Sahne Ayarları")]
    [Tooltip("Geçiş yapılacak sahnenin tam adını buraya yazın.")]
    public string targetSceneName;

    [Header("Spesifik Doğma Ayarı (Opsiyonel)")]
    public bool ozelKoordinataIsinla = false;
    [Tooltip("Oyuncunun hedef sahnede doğmasını istediğin X, Y, Z koordinatları.")]
    public Vector3 hedefKoordinat;

    // Diğer scriptlerin okuyabilmesi için statik (sabit) değişkenler
    public static bool ozelIsinlanmaAktif = false;
    public static Vector3 transferKoordinat;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                // Eğer özel koordinat kutucuğunu işaretlediysen, bilgileri statik hafızaya alıyoruz
                if (ozelKoordinataIsinla)
                {
                    ozelIsinlanmaAktif = true;
                    transferKoordinat = hedefKoordinat;
                    Debug.Log($"<color=orange>🚀 [Işınlanma Hazır] Hedef sahneye şu koordinat gönderildi: {hedefKoordinat}</color>");
                }

                Debug.Log(targetSceneName + " sahnesine LoadingManager ile pürüzsüz geçiş yapılıyor...");

                if (LoadingManager.Instance != null)
                {
                    LoadingManager.Instance.LoadScene(targetSceneName);
                }
                else
                {
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