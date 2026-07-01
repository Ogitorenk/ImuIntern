using UnityEngine;

public class CastleGateController : MonoBehaviour
{
    [SerializeField] private GameProgressData progressionData; 
    
    [Header("Kapı Modelleri")]
    [SerializeField] private GameObject closedGatePrefab; // Kapalı kapı modeli/objesi
    [SerializeField] private GameObject openGatePrefab;   // Açık kapı modeli/objesi

    [Header("Sağ Bölüm Kapısı")]
    [SerializeField] private GameObject rightSectionWoodenGate;

    void Start()
    {

        
    {
    // ÖNCE DİSKTEKİ VERİYİ ÇEK (Eğer PlayerPrefs temizlendiyse default ayarlara dönecek)
    progressionData.LoadFromDisk(); 

    // SONRA KAPININ DURUMUNU AYARLA
    if (progressionData.isFirstIronGateOpen)
    {
        closedGatePrefab.SetActive(false);
        openGatePrefab.SetActive(true);

        if (rightSectionWoodenGate != null)
            rightSectionWoodenGate.SetActive(true);
    }
    else
    {
        closedGatePrefab.SetActive(true);
        openGatePrefab.SetActive(false);

        if (rightSectionWoodenGate != null)
            rightSectionWoodenGate.SetActive(false);
    }
}
        if (progressionData.isFirstIronGateOpen)
        {
            // Kapı AÇIKSA: Kapalıyı gizle, açığı göster
            closedGatePrefab.SetActive(false);
            openGatePrefab.SetActive(true);

            if (rightSectionWoodenGate != null)
                rightSectionWoodenGate.SetActive(true);
        }
        else
        {
            // Kapı KAPALIYSA: Kapalıyı göster, açığı gizle
            closedGatePrefab.SetActive(true);
            openGatePrefab.SetActive(false);

            if (rightSectionWoodenGate != null)
                rightSectionWoodenGate.SetActive(false);
        }
    }
}