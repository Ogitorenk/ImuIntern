using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemySlimeMouse : MonoBehaviour, IDamageable
{
    [Header("Görsel Modeller (Dual Reality)")]
    [Tooltip("Don Kişot aktifken görünecek Slime modeli")]
    public GameObject slimeModel;
    [Tooltip("Sancho aktifken görünecek Fare modeli")]
    public GameObject mouseModel;

    [Header("Model Hizalama Hileleri (Inspector Offset)")]
    [Tooltip("Slime modelinin merkez kaçıklığını düzeltmek için lokal pozisyon offseti")]
    public Vector3 slimePositionOffset = Vector3.zero;
    [Tooltip("Slime modelinin yönünü düzeltmek için rotasyon offseti (Euler)")]
    public Vector3 slimeRotationOffset = Vector3.zero;
    [Space(10)]
    [Tooltip("Farenin sola/sağa kayma bug'ını çözmek için lokal pozisyon offseti (Örn: X = -2f)")]
    public Vector3 mousePositionOffset = Vector3.zero;
    [Tooltip("Fare modelinin yönünü düzeltmek için rotasyon offseti (Euler)")]
    public Vector3 mouseRotationOffset = Vector3.zero;

    [Header("Can Ayarları")]
    [Tooltip("Inspector'dan canı değiştirebilirsin kanka")]
    public float maxHealth = 80f; // 40f olan canı sike sike 80 yaptık!
    private float currentHealth;
    private bool isDead = false;
    private bool isTakingDamage = false;

    [Header("Hedef ve Takip")]
    public Transform targetPlayer;
    public float chaseRange = 10f;
    private NavMeshAgent agent;

    [Header("Saldırı Ayarları")]
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;
    public float attackHitDelay = 0.3f;

    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    [Header("Slime Animator Ayarları")]
    public Animator slimeAnimator;
    public string slimeWalkingBool = "isMoving";
    public string slimeAttackTrigger = "Attack";
    public string slimeDamageTrigger = "Damage";
    public string slimeDeathTrigger = "Death";

    [Header("Fare (Mouse) Animator Ayarları")]
    public Animator mouseAnimator;
    public string mouseWalkingBool = "isMoving";
    public string mouseAttackTrigger = "Attack";
    public string mouseDamageTrigger = "Damage";
    public string mouseDeathTrigger = "Death";

    private bool isDonActive = true;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        FindActivePlayer();
        CheckRealityVisibility();

        agent.speed = 4f;
        agent.stoppingDistance = attackRange - 0.1f;
    }

    void Update()
    {
        if (isDead) return;

        // Karakter değişti mi, kim aktif kontrol et ve görselliği güncelle
        FindActivePlayer();
        CheckRealityVisibility();

        if (targetPlayer == null)
        {
            agent.isStopped = true;
            SetMovingAnimation(false);
            return;
        }

        // Mesafe kontrolünü yaparken Y eksenindeki (hava fırlama) sapmayı sıfırlıyoruz kanka!
        Vector3 enemyPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 playerPos = new Vector3(targetPlayer.position.x, 0f, targetPlayer.position.z);
        float distanceToPlayer = Vector3.Distance(enemyPos, playerPos);

        // === BUG ÇÖZÜMÜ: OYUNCU TEPEMİZE ÇIKTIYSA HASAR VER VE ÜSTÜMÜZDEN DÜŞÜR ===
        // Yatayda çok yakınız ama oyuncu Y ekseninde bizden yukarıda (kafamıza basıyor)
        float heightDifference = targetPlayer.position.y - transform.position.y;

        if (distanceToPlayer <= 1.2f && heightDifference > 0.8f && heightDifference < 2.5f)
        {
            if (Time.time >= lastAttackTime + attackCooldown && !isTakingDamage && !isAttacking)
            {
                Debug.Log("🧠 " + gameObject.name + ": Kafama basma serseri! Üstümdeki oyuncuya hasar veriliyor.");
                StartCoroutine(AttackRoutine());
                return; // O karede takip kodlarını çalıştırma direkt atağa odaklansın
            }
        }

        if (isTakingDamage || isAttacking)
        {
            agent.isStopped = true;
            SetMovingAnimation(false);
            return;
        }

        // SALDIRI MENZİLİ (Karakter havaya fırlasa bile yatayda yakınsa vurmaya devam etsin)
        if (distanceToPlayer <= attackRange + 0.3f)
        {
            agent.isStopped = true;
            SetMovingAnimation(false);

            Vector3 direction = (targetPlayer.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.15f);
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        // TAKİP MENZİLİ
        else if (distanceToPlayer <= chaseRange)
        {
            agent.isStopped = false;
            agent.SetDestination(targetPlayer.position);
            SetMovingAnimation(true);
        }
        else
        {
            agent.isStopped = true;
            SetMovingAnimation(false);
        }
    }

    void FindActivePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.activeInHierarchy)
            {
                targetPlayer = p.transform;

                // Karakterin adına göre gerçekliği belirliyoruz (Don mu Sancho mu?)
                isDonActive = p.name.Contains("Don") || p.GetComponent<DonMovement>() != null;
                return;
            }
        }
    }

    // Gerçekliğe göre hangi modelin görüneceğini ayarlar
    void CheckRealityVisibility()
    {
        if (slimeModel != null)
        {
            slimeModel.SetActive(isDonActive);

            // === SİKE SİKE HİZALAMA: SLIME OFFSET AYARI ===
            if (isDonActive)
            {
                slimeModel.transform.localPosition = slimePositionOffset;
                slimeModel.transform.localRotation = Quaternion.Euler(slimeRotationOffset);
            }
        }

        if (mouseModel != null)
        {
            mouseModel.SetActive(!isDonActive);

            // === SİKE SİKE HİZALAMA: FARE OFFSET AYARI ===
            if (!isDonActive)
            {
                mouseModel.transform.localPosition = mousePositionOffset;
                mouseModel.transform.localRotation = Quaternion.Euler(mouseRotationOffset);
            }
        }

        if (agent != null)
        {
            agent.speed = isDonActive ? 4f : 5.5f;
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Aktif olan modele göre ilgili animatörü tetikle
        if (isDonActive && slimeAnimator != null) slimeAnimator.SetTrigger(slimeAttackTrigger);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetTrigger(mouseAttackTrigger);

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && !isTakingDamage && targetPlayer != null)
        {
            // Kafaya basma durumu veya normal yatay mesafe durumunda hasar geçerli olsun
            float heightDifference = targetPlayer.position.y - transform.position.y;
            Vector3 enemyPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 playerPos = new Vector3(targetPlayer.position.x, 0f, targetPlayer.position.z);
            float horizontalDistance = Vector3.Distance(enemyPos, playerPos);

            if (horizontalDistance <= attackRange + 0.4f || (horizontalDistance <= 1.2f && heightDifference > 0.8f))
            {
                IDamageable damageableTarget = targetPlayer.GetComponent<IDamageable>();
                if (damageableTarget != null)
                {
                    damageableTarget.TakeDamage(attackDamage);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + " Hasar Yedi! Kalan Can: " + currentHealth);

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

        if (isDonActive && slimeAnimator != null) slimeAnimator.SetTrigger(slimeDamageTrigger);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetTrigger(mouseDamageTrigger);

        isAttacking = false;

        yield return new WaitForSeconds(0.4f);

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

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (isDonActive && slimeAnimator != null) slimeAnimator.SetTrigger(slimeDeathTrigger);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetTrigger(mouseDeathTrigger);

        Debug.Log(gameObject.name + " öldü!");
        Destroy(gameObject, 2.5f);
    }

    void SetMovingAnimation(bool isMoving)
    {
        if (isDonActive && slimeAnimator != null) slimeAnimator.SetBool(slimeWalkingBool, isMoving);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetBool(mouseWalkingBool, isMoving);
    }
}