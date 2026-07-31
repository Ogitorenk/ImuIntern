using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Sahne Ayarları")]
    public string targetSceneName;

    [Header("Spesifik Doğma Ayarı")]
    public bool ozelKoordinataIsinla = false;
    public Vector3 hedefKoordinat;

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;

        if (other.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                isTransitioning = true;

                // Özel koordinat kutusu işaretliyse doğrudan CheckpointManager'a emri çakıyoruz
                if (ozelKoordinataIsinla)
                {
                    CheckpointManager.OverrideNextSpawn(hedefKoordinat);
                    Debug.Log($"<color=orange>🚀 [SceneChanger] Geçici doğma konumu set edildi: {hedefKoordinat}</color>");
                }

                if (LoadingManager.Instance != null)
                {
                    LoadingManager.Instance.LoadScene(targetSceneName);
                }
                else
                {
                    SceneManager.LoadScene(targetSceneName);
                }
            }
        }
    }

    private void OnDisable()
    {
        isTransitioning = false;
    }
}