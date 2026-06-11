using UnityEngine;

[CreateAssetMenu(fileName = "NewProgressData", menuName = "Game Data/Progress Data")]
public class GameProgressData : ScriptableObject
{
    [Header("--- ÝLERLEME VERÝLERÝ ---")]
    public int totalTokens = 0;
    public Vector3 lastCheckpointPosition;
    public string lastSavedSceneName = "Level_1";

    public void ResetToDefault()
    {
        totalTokens = 0;
        lastCheckpointPosition = Vector3.zero;
        lastSavedSceneName = "Level_1";
    }
}