using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [Header("Hasar Ayarý")]
    public float damageAmount = 20f; // Inspector'dan mýzrak için 40, ok için 20 yaparsýn kanka

    [Header("Efektler")]
    public GameObject hitEffect; // Çarpma anýnda çýkacak toz/kan efekti (isteðe baðlý)

    private bool hasHit = false; // Ok/Mýzrak saplandýktan sonra birden fazla kez hasar vermesin diye

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Vurduðumuz obje bir düþman mý ve hasar alabiliyor mu?
        IDamageable enemy = other.GetComponent<IDamageable>();

        // Eðer child objesinde varsa onu da kontrol et
        if (enemy == null) enemy = other.GetComponentInParent<IDamageable>();

        if (enemy != null)
        {
            hasHit = true;

            // DÜÞMANI SÝKE SÝKE VURUYORUZ!
            enemy.TakeDamage(damageAmount);

            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, transform.rotation);
            }

            // Saplanma hissi için rigidbody'yi durdurup düþmanýn child'ý yapabilirsin 
            // ya da direkt yok edebilirsin. Þimdilik temizce yok edelim:
            Destroy(gameObject);
        }
        else if (other.gameObject.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Eðer yere veya duvara çarptýysa hasar vermeden 2 saniye sonra silinsin
            hasHit = true;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // Hareketi durdur
            Destroy(gameObject, 2f);
        }
    }
}