using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    // En son kaydedilen checkpoint pozisyonu
    [SerializeField] private Vector3 lastCheckpointPosition;

    // Oyun başında karakterlerin başladığı yer ilk checkpoint olsun mu?
    public bool useInitialPositionAsCheckpoint = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Oyun başında karakterin durduğu yeri ilk güvenli nokta olarak kaydet
        if (useInitialPositionAsCheckpoint && DualRealityManager.Instance != null)
        {
            lastCheckpointPosition = DualRealityManager.Instance.donQuixote.transform.position;
        }
    }

    // Yeni bir checkpoint alındığında çağrılır
    public void UpdateCheckpoint(Vector3 newPos)
    {
        lastCheckpointPosition = newPos;
        Debug.Log("<color=cyan>🚩 Checkpoint Kaydedildi: </color>" + newPos);
    }

    // Karakter öldüğünde bu pozisyonu isteyeceğiz
    public Vector3 GetLastCheckpoint()
    {
        return lastCheckpointPosition;
    }
}