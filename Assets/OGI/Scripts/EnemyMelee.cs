using UnityEngine;
using UnityEngine.AI; // Yürüme yapay zekası için şart
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMelee : MonoBehaviour, IDamageable
{
    [Header("--- MOD SEÇİMİ (HİBRİT) ---")]
    [Tooltip("Eğer bunu tiklersen kanka, bu haydut dibe koşmaz, uzaktan mermi/ok fırlatır!")]
    public bool isRanged = false;

    [Header("Sağlık Ayarları")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;
    private bool isTakingDamage = false; // Hasar yerken movement kesilsin diye

    [Header("Hareket ve Hedef")]
    public Transform player;
    [Tooltip("Düşman seni kaç metreden fark edip koşmaya başlasın?")]
    public float chaseRange = 15f;
    private NavMeshAgent agent;

    [Header("Saldırı Ayarları")]
    [Tooltip("Düşmanın vurma/sıkma menzili. (HİLE: Eğer Uzakçıysa Start'ta otomatik 10f olur kanka, burayı yakıncıya göre ayarlayabilirsin)")]
    public float attackRange = 2f;
    public float attackDamage = 15f;
    [Tooltip("İki saldırı arası kaç saniye beklesin?")]
    public float attackCooldown = 2f;
    [Tooltip("Animasyon başladıktan kaç saniye sonra hasar işlesin veya ok elden çıksın?")]
    public float attackHitDelay = 0.5f;

    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    [HideInInspector] public Animator animator;

    [Header("--- MELEE (Yakın Dövüş) Silah Ayarları ---")]
    [Tooltip("Düşmanın elinde duran fiziksel Balta GameObject'i. Başlangıçta kapalı olacak kanka.")]
    public GameObject visualAxe;

    [Header("--- RANGED (Uzakçı) Silah Ayarları ---")]
    [Tooltip("Uzakçı modu açıkken düşmanın elinde belirecek görsel Yay (Bow) objesi kanka.")]
    public GameObject visualBow;
    [Tooltip("Uzakçının elinden fırlatacağı ok, mızrak veya mermi prefab'ı.")]
    public GameObject projectilePrefab;
    [Tooltip("Okun elinden çıkması için yayın önüne veya el kemiğine koyduğun boş obje (Spawn Point).")]
    public Transform shootSpawnPoint;
    [Tooltip("Fırlatılan okun uçuş hızı kanka.")]
    public float projectileSpeed = 15f;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        FindActivePlayer();

        // Oyun başlarken iki silah da elinde gizlensin, saçmalamasınlar
        if (visualAxe != null) visualAxe.SetActive(false);
        if (visualBow != null) visualBow.SetActive(false);

        agent.speed = 3.5f;

        // ==============================================================================================
        // --- BUG FIX 2: MENZİL KARIŞIKLIĞINI KÖKTEN ÇÖZME HİLESİ ---
        // ==============================================================================================
        if (isRanged)
        {
            // Eğer uzakçıysa ve yanlışlıkla Inspector'da menzili küçük bıraktıysan emniyet olarak 10 metreye çekiyoruz kanka!
            if (attackRange <= 3f) attackRange = 10f;

            agent.stoppingDistance = attackRange; // Tam atış sınırında zınk diye dursun
        }
        else
        {
            agent.stoppingDistance = attackRange - 0.2f; // Yakıncıysa dibine girsin
        }
    }

    void Update()
    {
        if (isDead) return;

        FindActivePlayer();

        // PLAYER YOKSA VEYA PLAYER ÖLDÜYSE HAYDUTU DURDURAN KONTROL
        if (player == null || !isPlayerAlive())
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);

            if (visualAxe != null && visualAxe.activeInHierarchy) visualAxe.SetActive(false);
            if (visualBow != null && visualBow.activeInHierarchy) visualBow.SetActive(false);

            isAttacking = false;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isTakingDamage || isAttacking)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
            return;
        }

        // SALDIRI MENZİLİNDEYSE
        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);

            // === DİKEY EKSEN BUGFIX DOKUNUŞU ===
            // Uzakçıysa hedefin Y eksenini (yukarı/aşağı) sıfırlamıyoruz ki tam çapraz nişan alabilsin kanka!
            Vector3 direction = (player.position - transform.position).normalized;
            if (!isRanged)
            {
                direction.y = 0; // Sadece yakıncılar düz baksın sahnede bükülmesin
            }

            if (direction != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        // TAKİP MENZİLİNDEYSE
        else if (distanceToPlayer <= chaseRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (animator != null) animator.SetBool("isWalking", true);
        }
        // MENZİL DIŞINDAYSA (BEKLE)
        else
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
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

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (isRanged)
        {
            if (visualBow != null) visualBow.SetActive(true);
            if (animator != null) animator.SetTrigger("RangedAttack");
        }
        else
        {
            if (visualAxe != null) visualAxe.SetActive(true);
            if (animator != null) animator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && !isTakingDamage && player != null && isPlayerAlive())
        {
            if (isRanged)
            {
                // ==================================================================
                // RANGED (UZAKÇI - YAY) MODU - TAM 3B DİKEY NİŞAN ALMA SİSTEMİ
                // ==================================================================
                if (projectilePrefab != null && shootSpawnPoint != null)
                {
                    // === KRİTİK GÜNCELLEME: Y ekseni dahil tam yön vektörü hesaplanıyor kanka ===
                    Vector3 targetDir = ((player.position + Vector3.up * 1f) - shootSpawnPoint.position).normalized;

                    // Oku tam hedefe doğru döndürerek yaratıyoruz
                    Quaternion projectileRotation = Quaternion.LookRotation(targetDir);
                    GameObject bullet = Instantiate(projectilePrefab, shootSpawnPoint.position, projectileRotation);

                    Rigidbody rb = bullet.GetComponentInChildren<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.velocity = Vector3.zero; // Kalıntı fizik verilerini sıfırla

                        // Ok artık yukarı/aşağı çapraz eksende kusursuz uçacak kanka!
                        rb.velocity = targetDir * projectileSpeed;
                    }

                    var bulletScript = bullet.GetComponent<SpitBullet>();
                    if (bulletScript != null)
                    {
                        bulletScript.SetupBullet(attackDamage);
                    }
                    Debug.Log($"🎯 Uzakçı Haydut oku tam 3B eksende hedefe doğrultup fırlattı!");
                }
            }
            else
            {
                // ==================================================================
                // MELEE (YAKIN DÖVÜŞ - BALTA) MODU
                // ==================================================================
                float distance = Vector3.Distance(transform.position, player.position);
                if (distance <= attackRange + 0.5f)
                {
                    IDamageable damageableTarget = player.GetComponent<IDamageable>();
                    if (damageableTarget != null)
                    {
                        damageableTarget.TakeDamage(attackDamage);
                        Debug.Log($"🪓 Haydut anlık beliren baltasıyla oyuncuya {attackDamage} hasar vurdu!");
                    }
                }
            }
        }

        yield return new WaitForSeconds(1f - attackHitDelay);

        if (visualAxe != null) visualAxe.SetActive(false);
        if (visualBow != null) visualBow.SetActive(false);

        isAttacking = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} Hasar Yedi! Kalan Can: {currentHealth}");

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
        agent.isStopped = true;

        if (animator != null) animator.SetTrigger("Damage");

        if (visualAxe != null) visualAxe.SetActive(false);
        if (visualBow != null) visualBow.SetActive(false);

        isAttacking = false;

        yield return new WaitForSeconds(0.6f);

        if (!isDead)
        {
            isTakingDamage = false;
        }
    }

    private void Die()
    {
        isDead = true;
        agent.isStopped = true;
        agent.enabled = false;

        if (visualAxe != null) visualAxe.SetActive(false);
        if (visualBow != null) visualBow.SetActive(false);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (animator != null) animator.SetTrigger("Death");

        Debug.Log(gameObject.name + " GEBERDİ!");
        Destroy(gameObject, 3f);
    }
}