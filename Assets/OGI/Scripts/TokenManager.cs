using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TokenManager : MonoBehaviour
{
    public static TokenManager Instance;

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
    public GameObject hudTokenPanel;
    public TextMeshProUGUI hudTokenText;
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
        // --- SAHNELER ARASI HAFIZA ENTEGRASYONU ---
        // Sahne ilk açıldığında CheckpointManager'da taşınan güncel token sayısını cüzdana çekiyoruz kanka kafa kafaya veriyorlar
        if (CheckpointManager.Instance != null)
        {
            totalTokensCollected = CheckpointManager.Instance.totalTokens;
        }
        else
        {
            // Eğer sahne bağımsız test ediliyorsa direkt hard diskten oku kanka patlamasın
            totalTokensCollected = PlayerPrefs.GetInt("Total_Tokens", 0);
        }

        // Başarımların durumunu da save dosyasından yükle kanka (Ertesi gün gelen oyuncu için)
        for (int i = 0; i < achievements.Count; i++)
        {
            achievements[i].isUnlocked = PlayerPrefs.GetInt("AchUnlocked_" + i, 0) == 1;
        }

        if (hudTokenPanel != null) hudTokenPanel.SetActive(false);
        ClearTexts();
        UpdatePauseMenuUI();
    }

    public void AddToken()
    {
        totalTokensCollected++;

        // --- SESTİM TAŞIMA: Toplanan yeni token'ı merkezi cüzdana da anlık paslıyoruz kanka ---
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.totalTokens = totalTokensCollected;

            // Checkpoint konumuyla beraber toplam token sayısını da anlık hard diske mühürlesin kanka
            CheckpointManager.Instance.UpdateCheckpoint(CheckpointManager.Instance.GetLastCheckpoint());
        }

        UpdatePauseMenuUI();

        if (currentDisplayCoroutine != null) StopCoroutine(currentDisplayCoroutine);

        bool isAchievementUnlocked = CheckAchievements();

        if (!isAchievementUnlocked)
        {
            if (achievementText != null) achievementText.text = "";
            currentDisplayCoroutine = StartCoroutine(ShowHudTokenRoutine());
        }
    }

    private void UpdatePauseMenuUI()
    {
        // Level Designer arkadaşının Pause Menü text'i varsa buraya bağlayabilirsin kanka
    }

    private IEnumerator ShowHudTokenRoutine()
    {
        if (hudTokenPanel == null || hudTokenText == null) yield break;

        hudTokenText.text = "+1 Token (" + totalTokensCollected + ")";
        hudTokenPanel.SetActive(true);

        yield return new WaitForSeconds(hudDisplayDuration);

        hudTokenPanel.SetActive(false);
        ClearTexts();
    }

    private bool CheckAchievements()
    {
        for (int i = 0; i < achievements.Count; i++)
        {
            AchievementData ach = achievements[i];

            if (totalTokensCollected == ach.targetTokenCount && !ach.isUnlocked)
            {
                ach.isUnlocked = true;

                // Başarım durumunu da kalıcı olarak diske yaz kanka
                PlayerPrefs.SetInt("AchUnlocked_" + i, 1);
                PlayerPrefs.Save();

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
        ClearTexts();
    }

    private void ClearTexts()
    {
        if (hudTokenText != null) hudTokenText.text = "";
        if (achievementText != null) achievementText.text = "";
    }
}