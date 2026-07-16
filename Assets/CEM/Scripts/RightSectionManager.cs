using UnityEngine;

public class RightSectionManager : MonoBehaviour
{
    [SerializeField] private GameProgressData progressionData;

    [Header("Sahne Nesneleri")]
    [SerializeField] private GameObject npcObject;         // NPC artık her zaman sahneye açık başlayacak
    [SerializeField] private GameObject portalObject;      // Castle Entrance'a götüren Portal objesi
    [SerializeField] private GameObject portalSpawnEffect; // Portal açılış efekti

    void Start()
    {
        // En güncel veriyi diskten yükle
        progressionData.LoadFromDisk();

        EvaluateRightSectionState();
    }

    public void EvaluateRightSectionState()
    {
        // NPC her zaman aktif kalıyor (oyuncuya ne yapacağını söylemesi için)
        if (npcObject != null) npcObject.SetActive(true);

        // Eğer oyuncu daha önce NPC ile konuşup portalı zaten açtıysa, portal aktif başlasın
        if (progressionData.isRightSectionNpcTalked)
        {
            if (portalObject != null) portalObject.SetActive(true);
        }
        else
        {
            // Konuşmadıysa veya şalterler bitmediyse portal gizli başlasın
            if (portalObject != null) portalObject.SetActive(false);
        }
    }

    // Portalın görsel ve işlevsel olarak yaratılma anı
    public void SpawnPortal()
    {
        if (portalObject != null && !portalObject.activeSelf)
        {
            portalObject.SetActive(true);

            // Görsel efekt fırlat
            if (portalSpawnEffect != null)
            {
                Instantiate(portalSpawnEffect, portalObject.transform.position, Quaternion.identity);
            }

            Debug.Log("<color=cyan>✨ [Portal Aktif] Oyuncu artık Castle Entrance'a dönebilir!</color>");
        }
    }
}