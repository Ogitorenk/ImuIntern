using UnityEngine;

[CreateAssetMenu(fileName = "NewProgressData", menuName = "Game Data/Progress Data")]
public class GameProgressData : ScriptableObject
{
    [Header("--- GENEL İLERLEME VERİLERİ ---")]
    public int totalTokens = 0;
    public Vector3 lastCheckpointPosition;
    public string lastSavedSceneName = "IntroScene"; // Varsayılan başlangıç sahneniz

    [Header("--- KALE KAPILARI VE BOSS DURUMLARI ---")]
    public bool isLeftBossDefeated = false;
    public bool isFirstIronGateOpen = false;
    public bool isRightBossDefeated = false;
    public bool isSecondIronGateOpen = false;

    public void ResetToDefault()
    {
        totalTokens = 0;
        lastCheckpointPosition = Vector3.zero;
        lastSavedSceneName = "IntroScene";

        // Kale ilerlemesini sıfırla
        isLeftBossDefeated = false;
        isFirstIronGateOpen = false;
        isRightBossDefeated = false;
        isSecondIronGateOpen = false;
    }

    // Diskten verileri ScriptableObject içine yükler
    public void LoadFromDisk()
    {
        if (PlayerPrefs.HasKey("HasSaveData"))
        {
            totalTokens = PlayerPrefs.GetInt("SO_Tokens", 0);
            lastSavedSceneName = PlayerPrefs.GetString("SO_LastScene", "IntroScene");
            
            float x = PlayerPrefs.GetFloat("SO_CheckX", 0f);
            float y = PlayerPrefs.GetFloat("SO_CheckY", 0f);
            float z = PlayerPrefs.GetFloat("SO_CheckZ", 0f);
            lastCheckpointPosition = new Vector3(x, y, z);

            // Kale verilerini diskten oku (0 = false, 1 = true)
            isLeftBossDefeated = PlayerPrefs.GetInt("SO_LeftBossDefeated", 0) == 1;
            isFirstIronGateOpen = PlayerPrefs.GetInt("SO_FirstIronGateOpen", 0) == 1;
            isRightBossDefeated = PlayerPrefs.GetInt("SO_RightBossDefeated", 0) == 1;
            isSecondIronGateOpen = PlayerPrefs.GetInt("SO_SecondIronGateOpen", 0) == 1;
        }
        else
        {
            ResetToDefault();
        }
    }

    // ScriptableObject'teki mevcut verileri diske kaydeder
    public void SaveToDisk()
    {
        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.SetInt("SO_Tokens", totalTokens);
        PlayerPrefs.SetString("SO_LastScene", lastSavedSceneName);
        
        PlayerPrefs.SetFloat("SO_CheckX", lastCheckpointPosition.x);
        PlayerPrefs.SetFloat("SO_CheckY", lastCheckpointPosition.y);
        PlayerPrefs.SetFloat("SO_CheckZ", lastCheckpointPosition.z);

        // Kale verilerini diske kaydet (Bool değerleri int olarak saklıyoruz)
        PlayerPrefs.SetInt("SO_LeftBossDefeated", isLeftBossDefeated ? 1 : 0);
        PlayerPrefs.SetInt("SO_FirstIronGateOpen", isFirstIronGateOpen ? 1 : 0);
        PlayerPrefs.SetInt("SO_RightBossDefeated", isRightBossDefeated ? 1 : 0);
        PlayerPrefs.SetInt("SO_SecondIronGateOpen", isSecondIronGateOpen ? 1 : 0);
        
        PlayerPrefs.Save();
    }
}