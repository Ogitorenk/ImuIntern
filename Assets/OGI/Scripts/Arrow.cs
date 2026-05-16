using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Ayarlar")]
    public float damage = 20f;
    public float lifeTime = 5f;

    private Rigidbody rb;
    private bool hasHit = false;

    [Header("Uçuş Fiziği")]
    public float customGravity = 2f; // Unity'nin normali 9.81'dir. Bunu 2-3 yaparak süzülmesini sağlıyoruz.

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false; // Unity'nin o ağır yerçekimini KAPATTIK!
        }

        // ... (Diğer çarpışma yoksayma kodların burada kalsın) ...
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (!hasHit)
        {
            // Oku kendi özel hafif yerçekimimizle aşağı çekiyoruz
            rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);

            // Mızrak gibi dönme efekti
            if (rb.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.velocity);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit || other.CompareTag("Player")) return;

        EnemyMelee enemy = other.GetComponent<EnemyMelee>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyMelee>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log("🎯 Ok düşmana saplandı! Hasar: " + damage);
            Destroy(gameObject);
            return;
        }

        // Zemine veya duvara çarptıysa
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.CompareTag("Ground"))
        {
            hasHit = true;
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
            Destroy(gameObject, 1f);
        }
    }
}