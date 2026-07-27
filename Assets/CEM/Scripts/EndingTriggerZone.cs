using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingTriggerZone : MonoBehaviour
{
    [Header("Sahne Ayarları")]
    [Tooltip("Geçiş yapılacak final sahnesinin adı")]
    [SerializeField] private string endingSceneName = "Ending_Scene";

    [Header("Karakter Etiketleri")]
    [SerializeField] private string donTag = "PlayerDon";
    [SerializeField] private string sanchoTag = "PlayerSancho";
    [SerializeField] private string playerTag = "Player";

    [Header("Ekran Karartma (Fade)")]
    [Tooltip("Siyah ekranı içeren CanvasGroup. Başlangıçta Alpha = 0 olmalı.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 2.0f;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Oyuncu alana girdiğinde ve geçiş henüz başlamadıysa
        if (!isTriggered && (other.CompareTag(donTag) || other.CompareTag(sanchoTag) || other.CompareTag(playerTag)))
        {
            isTriggered = true;
            StartCoroutine(StartEndingSequence());
        }
    }

    private IEnumerator StartEndingSequence()
    {
        Debug.Log("<color=magenta>🏆 Görünmez ending trigger'ı tetiklendi! Final sinematiğine geçiliyor...</color>");

        // 1. Oyuncunun hareketlerini ve boyut/karakter geçişini kilitle
        SetPlayerInputState(false);

        // 2. Ekranı Yavaşça Karart (Fade to Black)
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
            yield return new WaitForSeconds(0.5f); // Tam siyahta kısa bir es
        }

        // 3. Ending Sahnesini Yükle
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(endingSceneName);
        }
        else
        {
            SceneManager.LoadScene(endingSceneName);
        }
    }

    // Karakterlerin kontrolünü donduran yardımcı fonksiyon
    private void SetPlayerInputState(bool state)
    {
        // Sahnedeki "Player" etiketli veya hareket scripti taşıyan karakterleri bul
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player != null)
            {
                var donMove = player.GetComponent("DonMovement") as MonoBehaviour;
                if (donMove != null) donMove.enabled = state;

                var sanchoMove = player.GetComponent("SanchoMovement") as MonoBehaviour;
                if (sanchoMove != null) sanchoMove.enabled = state;

                var rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        // Dual Reality (karakter/boyut değiştirme) mekanizmasını kilitle
        if (DualRealityManager.Instance != null)
        {
            DualRealityManager.Instance.canSwitch = state;
        }
    }
}