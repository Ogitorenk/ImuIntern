using UnityEngine;

[CreateAssetMenu(fileName = "NewProgressData", menuName = "Game Data/Progress Data")]
public class GameProgressData : ScriptableObject
{
    [Header("--- İLERLEME VERİLERİ ---")]
    public int totalTokens = 0;
    public Vector3 lastCheckpointPosition;
    public string lastSavedSceneName = "IntroScene"; // Varsayılan başlangıç sahneniz

    public void ResetToDefault()
    {
        totalTokens = 0;
        lastCheckpointPosition = Vector3.zero;
        lastSavedSceneName = "IntroScene";
    }

    // Diskten verileri ScriptableObject içine yükler (CheckpointManager ile eşitlendi)
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
        }
        else
        {
            ResetToDefault();
        }
    }

    // ScriptableObject'teki mevcut verileri diske kaydeder (CheckpointManager ile eşitlendi)
    public void SaveToDisk()
    {
        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.SetInt("SO_Tokens", totalTokens);
        PlayerPrefs.SetString("SO_LastScene", lastSavedSceneName);
        
        PlayerPrefs.SetFloat("SO_CheckX", lastCheckpointPosition.x);
        PlayerPrefs.SetFloat("SO_CheckY", lastCheckpointPosition.y);
        PlayerPrefs.SetFloat("SO_CheckZ", lastCheckpointPosition.z);
        
        PlayerPrefs.Save();
    }
}