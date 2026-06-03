using UnityEngine;
using System.Collections;

public class SharedBreakableObject : MonoBehaviour
{
    [Header("--- Görsel ve Fizik ---")]
    [Tooltip("Kýrýlacak olan 3D model (Küp, Vazo vb.)")]
    public GameObject visualModel;
    [Tooltip("Objenin çarpýþma kutusu (BoxCollider vb.)")]
    public Collider objectCollider;

    [Header("--- Parçalanma Efekti ---")]
    [Tooltip("Kýrýlma anýnda patlayacak Particle System")]
    public ParticleSystem breakParticles;

    [Header("--- Düþecek Eþyalar (Loot) ---")]
    [Tooltip("Ýçinden çýkabilecek eþyalarýn listesi (Arrow_Loot_Prefab, Can, Zaman Potu)")]
    public GameObject[] itemsToDrop;
    [Range(0f, 100f)]
    [Tooltip("Eþya düþme þansý % kaç?")]
    public float dropChance = 80f;

    [Header("--- Yeniden Doðma Ayarlarý ---")]
    [Tooltip("Kýrýldýktan bir süre sonra geri gelsin mi?")]
    public bool respawnable = false;
    public float respawnTime = 3f;

    private bool isBroken = false;

    // Kýlýç, Mýzrak veya Ok bu fonksiyonu tetikleyecek
    public void BreakIt()
    {
        if (isBroken) return;
        isBroken = true;

        // 1. Modeli ve çarpýþmayý kapat
        if (visualModel != null) visualModel.SetActive(false);
        if (objectCollider != null) objectCollider.enabled = false;

        // 2. Partikül efektini patlat!
        if (breakParticles != null)
        {
            breakParticles.Play();
        }

        // 3. Eþya Düþürme (Loot) Mantýðý
        HandleLootDrop();

        // 4. Yeniden doðma kontrolü
        if (respawnable)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            float destroyDelay = breakParticles != null ? breakParticles.main.duration : 0.1f;
            Destroy(gameObject, destroyDelay);
        }
    }

    private void HandleLootDrop()
    {
        if (itemsToDrop == null || itemsToDrop.Length == 0) return;

        // Þans kontrolü
        if (Random.Range(0f, 100f) <= dropChance)
        {
            // Listeden rastgele bir item seç (Can, Zaman Potu veya Ok)
            int randomIndex = Random.Range(0, itemsToDrop.Length);
            GameObject selectedItem = itemsToDrop[randomIndex];

            if (selectedItem != null)
            {
                // --- GÜNCELLENDÝ: BUG FIX ---
                // Eþyayý 0.5f yerine 1.2f yüksekliðinde doðuruyoruz ki zeminin/kutunun dibine batmasýn kanka
                Vector3 dropPos = transform.position + Vector3.up * 1.2f;
                GameObject droppedObj = Instantiate(selectedItem, dropPos, Quaternion.identity);

                // Eðer doðan eþyada Rigidbody varsa, ilk doðma anýnda fýrlayýp gitmesin diye hýzýný sýfýrlýyoruz
                Rigidbody rb = droppedObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        isBroken = false;
        if (visualModel != null) visualModel.SetActive(true);
        if (objectCollider != null) objectCollider.enabled = true;
    }

    // Tetikleyicilerle kýrýlma kontrolü (Kýlýç/Mýzrak Collider'ý veya Ok gelirse)
    private void OnTriggerEnter(Collider other)
    {
        // Vuran þey Kýlýç, Mýzrak veya Sancho'nun fýrlattýðý Ok ise kýrýlacak
        if (other.CompareTag("Sword") || other.CompareTag("Spear") || other.CompareTag("Arrow"))
        {
            BreakIt();

            // Eðer vuran þey ok ise, ok saplanýp kalmasýn diye oku yok edebiliriz
            if (other.CompareTag("Arrow"))
            {
                Destroy(other.gameObject);
            }
        }
    }
}