using UnityEngine;

public class BreakablePlatform : MonoBehaviour
{
    // Hangi karakterlerin durabileceğini seçmek için enum tanımı
    public enum PlatformAccessibility { OnlyDon, Both }

    [Header("--- Level Designer Ayarları ---")]
    public PlatformAccessibility accessibility = PlatformAccessibility.OnlyDon; // Varsayılan olarak Sadece Don
    public float breakTime = 2f;
    public bool respawnAfterBreak = true;
    public float respawnTime = 3f;

    [Header("--- Görseller ---")]
    public GameObject solidModel;
    public GameObject ghostModel;
    public ParticleSystem breakEffect;

    private bool isBroken = false;
    private bool isBreaking = false; 
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
        // Karakterin tag kontrolü (Sancho'nun tag'inin de "Player" olduğunu veya doğru tag'e sahip olduğunu varsayıyorum)
        if (other.CompareTag("Player") && !isBroken && !isBreaking)
        {
            // Eğer "Both" seçiliyse her iki karakter de kırılmayı tetikler
            // Eğer "OnlyDon" seçiliyse sadece Don aktifken tetiklenir
            if (accessibility == PlatformAccessibility.Both || (accessibility == PlatformAccessibility.OnlyDon && currentlyDonActive))
            {
                isBreaking = true; 
            }
        }
    }

    public void UpdatePerception(bool isDonActive)
    {
        currentlyDonActive = isDonActive;

        if (isBroken) return;

        // --- GÖRSEL GÜNCELLEME ---
        // Eğer platform her ikisine de açıksa ("Both"), her iki gerçeklikte de katı (solid) görünmeli.
        if (accessibility == PlatformAccessibility.Both)
        {
            if (solidModel != null) solidModel.SetActive(true);
            if (ghostModel != null) ghostModel.SetActive(false);

            // "Both" durumunda hangi katmanda kalacağı projenizin fizik matrisine bağlıdır. 
            // Eğer "Default" katmanı her iki karakterle de çarpışıyorsa Default yapabilirsin.
            // Veya her iki karakterin de basabilmesi için "World_Don" katmanında bırakabilirsin (Eğer Sancho da bu katmana basabiliyorsa)
            gameObject.layer = LayerMask.NameToLayer("Default"); 
        }
        else // Eğer sadece Don basabiliyorsa ("OnlyDon") eski illüzyon mantığı aynen çalışır
        {
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
    }

    void BreakPlatform()
    {
        isBroken = true;
        isBreaking = false; 

        if (solidModel != null) solidModel.SetActive(false);
        if (ghostModel != null) ghostModel.SetActive(false);

        if (breakEffect != null) breakEffect.Play();

        currentRespawnTimer = 0f;
        currentBreakTimer = 0f;
    }

    void ResetPlatform()
    {
        isBroken = false;
        isBreaking = false; 
        currentBreakTimer = 0f;
        if (breakEffect != null)
        {
            breakEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        UpdatePerception(currentlyDonActive);
    }
}