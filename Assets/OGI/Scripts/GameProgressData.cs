using UnityEngine;

[CreateAssetMenu(fileName = "NewProgressData", menuName = "Game Data/Progress Data")]
public class GameProgressData : ScriptableObject
{
    [Header("--- GENEL İLERLEME VERİLERİ ---")]
    public int totalTokens = 0;
    public Vector3 lastCheckpointPosition;
    public string lastSavedSceneName = "IntroScene";

    [Header("--- KALE KAPILARI VE BOSS DURUMLARI ---")]
    public bool isLeftBossDefeated = false;
    public bool isFirstIronGateOpen = false;
    public bool isRightBossDefeated = false;
    public bool isSecondIronGateOpen = false;

    [Header("--- SAĞ BÖLÜM ŞALTERLERİ ---")]
    public bool isUpForestLeverPulled = false;
    public bool isMazeLeverPulled = false;
    public bool isPitLeverPulled = false;

    [Header("--- SAĞ BÖLÜM NPC DURUMU ---")]
    public bool isRightSectionNpcTalked = false;

    public void ResetToDefault()
    {
        totalTokens = 0;
        lastCheckpointPosition = Vector3.zero;
        lastSavedSceneName = "IntroScene";

        isLeftBossDefeated = false;
        isFirstIronGateOpen = false;
        isRightBossDefeated = false;
        isSecondIronGateOpen = false;

        // Sağ şalterleri sıfırla
        isUpForestLeverPulled = false;
        isMazeLeverPulled = false;
        isPitLeverPulled = false;

        // NPC sıfırlama
        isRightSectionNpcTalked = false;
    }

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

            isLeftBossDefeated = PlayerPrefs.GetInt("SO_LeftBossDefeated", 0) == 1;
            isFirstIronGateOpen = PlayerPrefs.GetInt("SO_FirstIronGateOpen", 0) == 1;
            isRightBossDefeated = PlayerPrefs.GetInt("SO_RightBossDefeated", 0) == 1;
            isSecondIronGateOpen = PlayerPrefs.GetInt("SO_SecondIronGateOpen", 0) == 1;

            // Sağ şalterleri diskten oku
            isUpForestLeverPulled = PlayerPrefs.GetInt("SO_UpForestLever", 0) == 1;
            isMazeLeverPulled = PlayerPrefs.GetInt("SO_MazeLever", 0) == 1;
            isPitLeverPulled = PlayerPrefs.GetInt("SO_PitLever", 0) == 1;

            isRightSectionNpcTalked = PlayerPrefs.GetInt("SO_RightNpcTalked", 0) == 1;
        }
        else
        {
            ResetToDefault();
        }
    }

    public void SaveToDisk()
    {
        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.SetInt("SO_Tokens", totalTokens);
        PlayerPrefs.SetString("SO_LastScene", lastSavedSceneName);
        
        PlayerPrefs.SetFloat("SO_CheckX", lastCheckpointPosition.x);
        PlayerPrefs.SetFloat("SO_CheckY", lastCheckpointPosition.y);
        PlayerPrefs.SetFloat("SO_CheckZ", lastCheckpointPosition.z);

        PlayerPrefs.SetInt("SO_LeftBossDefeated", isLeftBossDefeated ? 1 : 0);
        PlayerPrefs.SetInt("SO_FirstIronGateOpen", isFirstIronGateOpen ? 1 : 0);
        PlayerPrefs.SetInt("SO_RightBossDefeated", isRightBossDefeated ? 1 : 0);
        PlayerPrefs.SetInt("SO_SecondIronGateOpen", isSecondIronGateOpen ? 1 : 0);

        // Sağ şalterleri diske kaydet
        PlayerPrefs.SetInt("SO_UpForestLever", isUpForestLeverPulled ? 1 : 0);
        PlayerPrefs.SetInt("SO_MazeLever", isMazeLeverPulled ? 1 : 0);
        PlayerPrefs.SetInt("SO_PitLever", isPitLeverPulled ? 1 : 0);

        PlayerPrefs.SetInt("SO_RightNpcTalked", isRightSectionNpcTalked ? 1 : 0);
        
        PlayerPrefs.Save();
    }
}