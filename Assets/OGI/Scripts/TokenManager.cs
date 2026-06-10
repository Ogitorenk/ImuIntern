using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TokenManager : MonoBehaviour
{
    public static TokenManager Instance; // Singleton yapısı

    [System.Serializable]
    public class AchievementData
    {
        [Tooltip("Kaçıncı tokenda bu başarım açılsın? (Örn: 1, 5, 15, 30)")]
        public int targetTokenCount;

        [Tooltip("Başarım açılınca ne yazsın?")]
        public string achievementTitle = "🏆 BAŞARIM ADI";

        [TextArea(2, 5)]
        public string achievementDescription = "(Açıklama...)";

        [HideInInspector] public bool isUnlocked = false;
    }

    [Header("--- INSPECTOR BAŞARIM AYARLARI ---")]
    public List<AchievementData> achievements = new List<AchievementData>();

    [Header("--- HUD VE PANEL BAĞLANTILARI ---")]
    [Tooltip("Kullandığın o ortak Collectable Panel objesini buraya at kanka")]
    public GameObject hudTokenPanel;

    [Tooltip("Panelin içindeki anlık +1 yazan TextMeshPro")]
    public TextMeshProUGUI hudTokenText; // Hatalı satır silindi, doğrusu burası kanka!

    [Tooltip("Panelin içindeki o büyük Başarım yazan TextMeshPro bileşeni")]
    public TextMeshProUGUI achievementText;

    [Header("--- SÜRE VE SES AYARLARI ---")]
    public float hudDisplayDuration = 2.5f;
    public AudioClip achievementSound;
    public float achievementDisplayDuration = 3.5f;

    private int totalTokensCollected = 0;
    private Coroutine currentDisplayCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (GetComponent<AudioSource>() == null) gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // Oyun başında paneli gizle ve içlerini temizle kanka
        if (hudTokenPanel != null) hudTokenPanel.SetActive(false);
        ClearTexts();
        UpdatePauseMenuUI();
    }

    public void AddToken()
    {
        totalTokensCollected++;
        UpdatePauseMenuUI();

        // Eğer aktif çalışan bir ekran süresi varsa önce onu zorla durdur kanka
        if (currentDisplayCoroutine != null) StopCoroutine(currentDisplayCoroutine);

        // Başarım kontrolü yapıyoruz
        bool isAchievementUnlocked = CheckAchievements();

        if (!isAchievementUnlocked)
        {
            // Eğer bu sayı (2, 3, 4) başarım sayısı DEĞİLSE, başarım yazısını ZORLA siliyoruz!
            if (achievementText != null) achievementText.text = "";

            currentDisplayCoroutine = StartCoroutine(ShowHudTokenRoutine());
        }
    }

    private void UpdatePauseMenuUI()
    {
        // Level Designer arkadaşının Pause Menü text'i varsa buraya bağlayabilirsin kanka, opsiyoneldir
    }

    private IEnumerator ShowHudTokenRoutine()
    {
        if (hudTokenPanel == null || hudTokenText == null) yield break;

        // Normal sayaç yazısını basıyoruz
        hudTokenText.text = "+1 Token (" + totalTokensCollected + ")";
        hudTokenPanel.SetActive(true);

        yield return new WaitForSeconds(hudDisplayDuration);

        hudTokenPanel.SetActive(false);
        ClearTexts(); // Panel kapanırken yazıları sıfırla kanka
    }

    private bool CheckAchievements()
    {
        for (int i = 0; i < achievements.Count; i++)
        {
            AchievementData ach = achievements[i];

            // Sayı tam eşitse ve açılmadıysa başarım şovunu başlat
            if (totalTokensCollected == ach.targetTokenCount && !ach.isUnlocked)
            {
                ach.isUnlocked = true;

                string finalMessage = ach.achievementTitle + "\n" + ach.achievementDescription;
                currentDisplayCoroutine = StartCoroutine(ShowAchievementRoutine(finalMessage));
                return true;
            }
        }
        return false;
    }

    private IEnumerator ShowAchievementRoutine(string message)
    {
        if (hudTokenPanel == null || hudTokenText == null || achievementText == null) yield break;

        // Başarım anında panelin üst kısmına normal sayacı, alt kısmına büyük başarım metnini bas kanka
        hudTokenText.text = "+1 Token (" + totalTokensCollected + ")";
        achievementText.text = message;

        hudTokenPanel.SetActive(true);

        AudioSource audio = GetComponent<AudioSource>();
        if (achievementSound != null && audio != null)
        {
            audio.PlayOneShot(achievementSound);
        }

        yield return new WaitForSeconds(achievementDisplayDuration);

        hudTokenPanel.SetActive(false);
        ClearTexts(); // İş bitince tertemiz yap
    }

    // Yazıların panel açıkken içeride asılı kalmasını önleyen temizlik fonksiyonu kanka
    private void ClearTexts()
    {
        if (hudTokenText != null) hudTokenText.text = "";
        if (achievementText != null) achievementText.text = "";
    }
}