using UnityEngine;
using System.Collections;

public class EnemyVenusPlant : MonoBehaviour, IDamageable
{
    [Header("Sağlık Ayarları")]
    public float maxHealth = 60f;
    private float currentHealth;
    private bool isDead = false;
    private bool isTakingDamage = false;

    [Header("Menzil ve Hedef")]
    public Transform player;
    [Tooltip("Bitki seni kaç metreden fark edip tükürmeye başlasın kanka?")]
    public float attackRange = 10f;

    [Header("Saldırı Ayarları")]
    [Tooltip("İki tükürük arası kaç saniye beklesin?")]
    public float attackCooldown = 2.5f;
    [Tooltip("Animasyon başladıktan kaç saniye sonra tükürük ağzından çıksın? (Görsel uyum için)")]
    public float attackSpitDelay = 0.4f;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    [Header("--- Müstakbel Tükürük Asset Ayarları ---")]
    [Tooltip("Tükürük prefab'ını buraya sürükleyeceksin kanka.")]
    public GameObject spitPrefab;
    [Tooltip("Tükürüğün çıkacağı boş obje (Ağız kısmı).")]
    public Transform spitSpawnPoint;
    [Tooltip("Tükürüğün oyuncuya doğru uçma hızı.")]
    public float spitSpeed = 12f; // Çıtırından hızlandırdım jilet gibi gitsin diye

    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();

        FindActivePlayer();
    }

    void Update()
    {
        if (isDead || isTakingDamage) return;

        FindActivePlayer();

        if (player == null || !isPlayerAlive())
        {
            isAttacking = false;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.05f);

            if (!isAttacking && Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(SpitRoutine());
            }
        }
    }

    void FindActivePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.activeInHierarchy)
            {
                player = p.transform;
                return;
            }
        }
    }

    private bool isPlayerAlive()
    {
        if (player == null) return false;
        var targetHealth = player.GetComponent<IDamageable>();
        return targetHealth != null;
    }

    // ==============================================================================================
    // --- GÜNCELLENEN TÜKÜRME RUTİNİ (MERMİYİ KESİN UÇURAN SİSTEM) ---
    // ==============================================================================================
    private IEnumerator SpitRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (animator != null) animator.SetTrigger("Attack");

        yield return new WaitForSeconds(attackSpitDelay);

        if (!isDead && !isTakingDamage && player != null && isPlayerAlive())
        {
            if (spitPrefab != null && spitSpawnPoint != null)
            {
                // 1. Tükürüğü ağız noktasında yarat kanka
                GameObject temporarySpit = Instantiate(spitPrefab, spitSpawnPoint.position, spitSpawnPoint.rotation);

                // 2. Oyuncunun tam göğsüne doğru nişan al (Yere çakılmasın diye Vector3.up ekledik)
                Vector3 targetTargetDir = ((player.position + Vector3.up * 1f) - spitSpawnPoint.position).normalized;

                // 3. EMNİYET KİLİDİ: Rigidbody'yi objenin içinde veya alt çocuklarında (children) derinlemesine arasın
                Rigidbody mermiRb = temporarySpit.GetComponentInChildren<Rigidbody>();

                if (mermiRb != null)
                {
                    // Fizik engellerini kaldırıyoruz kanka
                    mermiRb.isKinematic = false;

                    // YÖNTEM A: Anlık hız ataması
                    mermiRb.velocity = targetTargetDir * spitSpeed;

                    // YÖNTEM B: Çift dikiş emniyet! Hız yemezse arkadan fiziki kuvvetle fırlatıyoruz
                    mermiRb.AddForce(targetTargetDir * spitSpeed, ForceMode.VelocityChange);

                    Debug.Log($"💦 Mermi jilet gibi fırlatıldı! Hedef Yönü: {targetTargetDir}");
                }
                else
                {
                    // Eğer hâlâ bu hata geliyorsa o mermi prefab'ında kesinlikle Rigidbody yoktur kanka!
                    Debug.LogError("⚠️ [KRİTİK HATA] Sürüklediğin mermi prefab'ında veya alt objelerinde Rigidbody BULUNAMADI kanka!");
                }
            }
            else
            {
                Debug.LogWarning("📌 Inspector'da slotlar boş kanka!");
            }
        }

        yield return new WaitForSeconds(1.0f - attackSpitDelay);
        isAttacking = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"🌱 Venus Plant hasar yedi! Kalan Can: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(DamageRoutine());
        }
    }

    private IEnumerator DamageRoutine()
    {
        isTakingDamage = true;
        isAttacking = false;

        if (animator != null) animator.SetTrigger("Damage");

        yield return new WaitForSeconds(0.5f);
        if (!isDead) isTakingDamage = false;
    }

    private void Die()
    {
        isDead = true;
        if (animator != null) animator.SetTrigger("Death");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log("💀 Venus Plant çürüdü ve öldü!");
        Destroy(gameObject, 3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}