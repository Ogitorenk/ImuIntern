using UnityEngine;

public class CastleGateController : MonoBehaviour
{
    [SerializeField] private GameProgressData progressionData; 
    
    [Header("--- 1. Kapı ve Sağ Giriş ---")]
    [SerializeField] private GameObject firstIronGate;
    [SerializeField] private GameObject rightSectionWoodenGate;

    [Header("--- 2. Kapı (Taht Odası Girişi) ---")]
    [SerializeField] private GameObject secondIronGate;

    void Start()
    {
        // Sahneye girildiğinde her ihtimale karşı güncel verileri diskten yükle
        progressionData.LoadFromDisk();

        // --- 1. Kapı Kontrolü ---
        if (progressionData.isFirstIronGateOpen)
        {
            firstIronGate.SetActive(false); // İlk kapıyı aç (gizle)
            
            if (rightSectionWoodenGate != null)
                rightSectionWoodenGate.SetActive(true); // Sağ bölüme giden tahta kapıyı aç
        }
        else
        {
            firstIronGate.SetActive(true);
            if (rightSectionWoodenGate != null)
                rightSectionWoodenGate.SetActive(false);
        }

        // --- 2. Kapı Kontrolü ---
        if (progressionData.isSecondIronGateOpen)
        {
            secondIronGate.SetActive(false); // İkinci kapıyı aç (gizle) -> Taht odası yolu artık açık!
        }
        else
        {
            secondIronGate.SetActive(true); // Kilitliyse kapalı tut
        }
    }
}