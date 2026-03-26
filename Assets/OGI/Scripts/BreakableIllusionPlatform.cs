using UnityEngine;

public class BreakableIllusionPlatform : MonoBehaviour
{
    [Header("--- Level Designer Ayarlarý ---")]
    public float breakTime = 2f;
    public bool respawnAfterBreak = true;
    public float respawnTime = 3f;

    [Header("--- Görseller ---")]
    public GameObject solidModel;
    public GameObject ghostModel;
    public ParticleSystem breakEffect;

    private bool isBroken = false;
    private bool isBreaking = false; // YENÝ: Platform tetiklendi mi? (Geri dönüþü yok)
    private float currentBreakTimer = 0f;
    private float currentRespawnTimer = 0f;
    private bool currentlyDonActive = true;

    void Start()
    {
        if (DualRealityManager.Instance != null)
        {
            UpdatePerception(DualRealityManager.Instance.isDonActive);
        }
    }

    void Update()
    {
        if (isBroken)
        {
            if (respawnAfterBreak)
            {
                currentRespawnTimer += Time.deltaTime;
                if (currentRespawnTimer >= respawnTime)
                {
                    ResetPlatform();
                }
            }
            return;
        }

        // YENÝ: Kýrýlma süreci baþladýysa, karakter üstünde olmasa bile sayacý artýr
        if (isBreaking)
        {
            currentBreakTimer += Time.deltaTime;

            if (currentBreakTimer >= breakTime)
            {
                BreakPlatform();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don platforma deðdiðinde ve platform henüz kýrýlma sürecine girmemiþse
        if (other.CompareTag("Player") && currentlyDonActive && !isBroken && !isBreaking)
        {
            isBreaking = true; // Süreci baþlat, artýk durdurulamaz!
        }
    }

    // DÝKKAT: OnTriggerExit fonksiyonunu TAMAMEN SÝLDÝK. 
    // Artýk platformdan inmesi sayacý sýfýrlamayacak.

    public void UpdatePerception(bool isDonActive)
    {
        currentlyDonActive = isDonActive;

        if (isBroken) return;

        if (isDonActive)
        {
            if (solidModel != null) solidModel.SetActive(true);
            if (ghostModel != null) ghostModel.SetActive(false);

            gameObject.layer = LayerMask.NameToLayer("World_Don");
        }
        else
        {
            if (solidModel != null) solidModel.SetActive(false);
            if (ghostModel != null) ghostModel.SetActive(true);

            gameObject.layer = LayerMask.NameToLayer("World_Sancho");
        }
    }

    void BreakPlatform()
    {
        isBroken = true;
        isBreaking = false; // Kýrýlma süreci tamamlandý

        if (solidModel != null) solidModel.SetActive(false);
        if (ghostModel != null) ghostModel.SetActive(false);

        if (breakEffect != null) breakEffect.Play();

        currentRespawnTimer = 0f;
        currentBreakTimer = 0f;
    }

    void ResetPlatform()
    {
        isBroken = false;
        isBreaking = false; // Yeniden doðduðunda tehlike sýfýrlanýr
        currentBreakTimer = 0f;
        UpdatePerception(currentlyDonActive);
    }
}