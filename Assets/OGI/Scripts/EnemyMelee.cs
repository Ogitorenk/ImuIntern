using UnityEngine;
using UnityEngine.AI; // Yürüme yapay zekasý için þart
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
// === GÜNCELLENDÝ: MIZRAÐIN HASAR VEREBÝLMESÝ ÝÇÝN IDAMAGEABLE KÝMLÝÐÝ EKLENDÝ ===
public class EnemyMelee : MonoBehaviour, IDamageable
{
    [Header("Saðlýk Ayarlarý")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;
    private bool isTakingDamage = false; // Hasar yerken hareket etmemesi için

    [Header("Hareket ve Hedef")]
    public Transform player;
    [Tooltip("Düþman seni kaç metreden fark edip koþmaya baþlasýn?")]
    public float chaseRange = 15f;
    private NavMeshAgent agent;

    [Header("Saldýrý Ayarlarý")]
    [Tooltip("Vurmak için ne kadar yaklaþmalý?")]
    public float attackRange = 2f;
    public float attackDamage = 15f;
    [Tooltip("Ýki saldýrý arasý kaç saniye beklesin?")]
    public float attackCooldown = 2f;
    [Tooltip("Animasyon baþladýktan kaç saniye sonra hasar oyuncuya iþlesin? (Kýlýcýn inme aný)")]
    public float attackHitDelay = 0.5f;

    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    [HideInInspector] public Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Her ihtiyaca karþý ilk baþta da aktif olaný bulalým
        FindActivePlayer();

        // Düþman hýzýný buradan da ayarlayabilirsin
        agent.speed = 3.5f;
        agent.stoppingDistance = attackRange - 0.2f; // Oyuncunun içine girmemesi için
    }

    void Update()
    {
        if (isDead) return;

        // ========================================================
        // SAHNEDEKÝ AKTÝF PLAYER'I HER KAREDE DÝNAMÝK OLARAK BUL
        // ========================================================
        FindActivePlayer();

        // Eðer o an sahnede hiç aktif player yoksa hata vermemesi için bekle
        if (player == null)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
            return;
        }
        // ========================================================

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Hasar alýrken veya saldýrýrken ayaklarý yere çivilensin
        if (isTakingDamage || isAttacking)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
            return;
        }

        // SALDIRI MENZÝLÝNDEYSE
        if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);

            // Oyuncuya doðru dön
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.1f);

            // Bekleme süresi bittiyse VUR!
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        // TAKÝP MENZÝLÝNDEYSE
        else if (distanceToPlayer <= chaseRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (animator != null) animator.SetBool("isWalking", true);
        }
        // MENZÝL DIÞINDAYSA (BEKLE)
        else
        {
            agent.isStopped = true;
            if (animator != null) animator.SetBool("isWalking", false);
        }
    }

    // ========================================================
    // SAHNEDE O AN AKTÝF OLAN "PLAYER" ETÝKETLÝ OBJEYÝ BULUR
    // ========================================================
    void FindActivePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject p in players)
        {
            if (p.activeInHierarchy)
            {
                player = p.transform;
                return; // Aktif olaný bulduðumuz an fonksiyondan çýk
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (animator != null) animator.SetTrigger("Attack");

        // Kýlýcýn oyuncuya deðme anýna kadar bekle (Animasyonun ortasý)
        yield return new WaitForSeconds(attackHitDelay);

        // Vuruþ anýnda oyuncu hala menzilde mi ve ölmediysek hasar ver!
        if (!isDead && !isTakingDamage && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= attackRange + 0.5f) // Ufak bir kaçma payý toleransý
            {
                // ========================================================
                // --- GÜNCELLENDÝ: ARTIK IDAMAGEABLE KÝMLÝÐÝNE VURUYORUZ ---
                // ========================================================
                IDamageable damageableTarget = player.GetComponent<IDamageable>();
                if (damageableTarget != null)
                {
                    damageableTarget.TakeDamage(attackDamage); // Don mu Sancho mu bakmýyor, hasarý basýyor!
                }
                // ========================================================
            }
        }

        // Animasyonun geri kalanýnýn bitmesini bekle
        yield return new WaitForSeconds(1f - attackHitDelay);
        isAttacking = false;
    }

    // Bizim Don veya Sancho bu metoda hasar gönderecek
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        // Konsola kimin ne kadar hasar yediðini yazalým kanka takip etmesi kolay olsun
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

        // Eðer o sýrada bize vurmaya çalýþýyorsa ataðý iptal et (Stun yedi)
        isAttacking = false;

        // Sersemleme süresi (Hasar yeme animasyonu uzunluðu)
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
        agent.enabled = false; // Ölü adam yürümez

        // Don Kiþot cesede takýlýp bug'a girmesin diye collider'ý kapat
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (animator != null) animator.SetTrigger("Death");

        Debug.Log(gameObject.name + " GEBERDÝ!");

        // === GÜNCELLENDÝ: ÖLDÜKTEN 3 SANÝYE SONRA CESET SAHNEDEN SÝLÝNSÝN ===
        Destroy(gameObject, 3f);
    }
}