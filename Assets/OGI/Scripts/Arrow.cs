using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float damage = 20f; // Okun vereceği hasar
    public float lifeTime = 5f; // Iska geçerse haritada sonsuza kadar kalmasın diye silinme süresi

    void Start()
    {
        // Ok fırlatıldıktan 5 saniye sonra sahneden otomatik silinsin
        Destroy(gameObject, lifeTime);
    }

    // Okun collider'ı bir şeye çarptığında bu metot tetiklenir
    void OnTriggerEnter(Collider other)
    {
        // Çarptığımız objede veya onun Parent'ında EnemyMelee scripti var mı diye bakıyoruz
        EnemyMelee enemy = other.GetComponent<EnemyMelee>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyMelee>();

        // Eğer düşmana çarptıysak hasar ver!
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log("🎯 Ok düşmana saplandı! Hasar: " + damage);

            // Ok düşmanın içinde kalsın istersek veya direkt yok edebiliriz:
            Destroy(gameObject);
            return;
        }

        // Düşman harici bir yere (Zemine/Duvara) çarptıysa oku yere çak veya yok et
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.CompareTag("Ground"))
        {
            // Oku yere saplanmış gibi sabitlemek için fiziğini kapatabilirsin:
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Ya da direkt yok et:
            Destroy(gameObject, 1f); // 1 saniye sonra silinsin
        }
    }
}