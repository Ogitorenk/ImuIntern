using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class BossSlimeMouse : MonoBehaviour, IDamageable
{
    [Header("Görsel Modeller (Dual Reality)")]
    [Tooltip("Don Kişot aktifken görünecek Slime modeli")]
    public GameObject slimeModel;
    [Tooltip("Sancho aktifken görünecek Fare modeli")]
    public GameObject mouseModel;

    [Header("Model Hizalama Hileleri (Inspector Offset)")]
    public Vector3 slimePositionOffset = Vector3.zero;
    public Vector3 slimeRotationOffset = Vector3.zero;
    [Space(5)]
    public Vector3 mousePositionOffset = Vector3.zero;
    public Vector3 mouseRotationOffset = Vector3.zero;

    [Header("Boss Can Ayarları")]
    [Tooltip("Boss'un canı babalar gibi yüksek olur kanka")]
    public float maxHealth = 500f;
    private float currentHealth;
    private bool isDead = false;
    private bool isTakingDamage = false;

    [Header("Hedef ve Takip")]
    public Transform targetPlayer;
    public float chaseRange = 25f;
    private NavMeshAgent agent;

    [Header("Saldırı Ayarları (Normal)")]
    [Tooltip("Boss devasa (Scale 4) olduğu için normal yakın dövüş menzili")]
    public float attackRange = 5.0f;
    public float attackDamage = 15f;
    public float attackCooldown = 2f;
    public float attackHitDelay = 0.3f;

    [Header("Boss Özel Saldırı Ayarları (Heavy Attack)")]
    [Range(0f, 100f)]
    [Tooltip("Boss'un her cooldown bittiğinde ağır saldırı yapma yüzdesi (Örn: 35 kanka)")]
    public float heavyAttackChance = 35f;

    [Tooltip("YENİ: Heavy Attack tetiklemek için gereken maksimum uzaklık. İlla dibine girmesine gerek yok kanka!")]
    public float heavyAttackRange = 12.0f;

    [Tooltip("Heavy Attack tetiklendiğinde Duplicate ettiğin yeni Animator'ı tetikleyecek trigger adı")]
    public string heavyAttackTrigger = "HeavyAttack";

    public float heavyAttackDamage = 35f;
    [Tooltip("Boss havaya yükselirken çıkacağı maksimum yükseklik")]
    public float heavyAttackHeight = 6f;
    [Tooltip("Yere çakıldığında hasar vereceği alanın yarıçapı")]
    public float heavyAttackRadius = 8f;

    [Tooltip("YENİ: Yere çakılınca doğacak o yeşil particle prefabını buraya at kanka")]
    public GameObject heavyAttackEffect;

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

        // === BOSS SCALE 4 AYARI ===
        transform.localScale = new Vector3(4f, 4f, 4f);

        FindActivePlayer();
        CheckRealityVisibility();

        agent.speed = 3.5f;
        agent.stoppingDistance = attackRange - 0.5f;
    }

    void Update()
    {
        if (isDead) return;

        FindActivePlayer();
        CheckRealityVisibility();

        if (targetPlayer == null)
        {
            agent.isStopped = true;
            SetMovingAnimation(false);
            return;
        }

        Vector3 enemyPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 playerPos = new Vector3(targetPlayer.position.x, 0f, targetPlayer.position.z);
        float distanceToPlayer = Vector3.Distance(enemyPos, playerPos);

        float heightDifference = targetPlayer.position.y - transform.position.y;

        if (distanceToPlayer <= 3.0f && heightDifference > 2.0f && heightDifference < 6f)
        {
            if (Time.time >= lastAttackTime + attackCooldown && !isTakingDamage && !isAttacking)
            {
                StartCoroutine(SelectAttackPatternRoutine(distanceToPlayer));
                return;
            }
        }

        if (isTakingDamage || isAttacking)
        {
            agent.isStopped = true;
            SetMovingAnimation(false);
            return;
        }

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (distanceToPlayer <= heavyAttackRange)
            {
                float randomRoll = Random.Range(0f, 100f);
                if (randomRoll <= heavyAttackChance)
                {
                    agent.isStopped = true;
                    SetMovingAnimation(false);
                    LookAtPlayer();
                    StartCoroutine(TriggerSpecificAttack(true));
                    return;
                }
                else if (distanceToPlayer <= attackRange)
                {
                    agent.isStopped = true;
                    SetMovingAnimation(false);
                    LookAtPlayer();
                    StartCoroutine(TriggerSpecificAttack(false));
                    return;
                }
            }
        }

        if (distanceToPlayer <= chaseRange && distanceToPlayer > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(targetPlayer.position);
            SetMovingAnimation(true);
        }
        else if (distanceToPlayer <= attackRange)
        {
            agent.isStopped = true;
            SetMovingAnimation(false);
            LookAtPlayer();
        }
        else
        {
            agent.isStopped = true;
            SetMovingAnimation(false);
        }
    }

    void LookAtPlayer()
    {
        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.15f);
        }
    }

    private IEnumerator TriggerSpecificAttack(bool doHeavy)
    {
        isAttacking = true;
        agent.isStopped = true;

        if (doHeavy) yield return StartCoroutine(HeavyAttackRoutine());
        else yield return StartCoroutine(NormalAttackRoutine());

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    private IEnumerator SelectAttackPatternRoutine(float distance)
    {
        isAttacking = true;
        agent.isStopped = true;

        float randomRoll = Random.Range(0f, 100f);

        if (randomRoll <= heavyAttackChance)
        {
            yield return StartCoroutine(HeavyAttackRoutine());
        }
        else if (distance <= attackRange)
        {
            yield return StartCoroutine(NormalAttackRoutine());
        }

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    private IEnumerator NormalAttackRoutine()
    {
        if (isDonActive && slimeAnimator != null) slimeAnimator.SetTrigger(slimeAttackTrigger);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetTrigger(mouseAttackTrigger);

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && !isTakingDamage && targetPlayer != null)
        {
            float heightDifference = targetPlayer.position.y - transform.position.y;
            Vector3 enemyPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 playerPos = new Vector3(targetPlayer.position.x, 0f, targetPlayer.position.z);
            float horizontalDistance = Vector3.Distance(enemyPos, playerPos);

            if (horizontalDistance <= attackRange + 1.0f || (horizontalDistance <= 3.0f && heightDifference > 2.0f))
            {
                IDamageable damageableTarget = targetPlayer.GetComponent<IDamageable>();
                if (damageableTarget != null) damageableTarget.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator HeavyAttackRoutine()
    {
        if (isDonActive && slimeAnimator != null) slimeAnimator.SetTrigger(heavyAttackTrigger);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetTrigger(heavyAttackTrigger);

        Transform activeModelTransform = isDonActive ? slimeModel.transform : mouseModel.transform;
        Vector3 baseOffset = isDonActive ? slimePositionOffset : mousePositionOffset;

        float duration = 0.5f;
        float elapsed = 0f;

        // --- HAVAYA YÜKSELİŞ ---
        while (elapsed < duration)
        {
            if (isDead || isTakingDamage) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float currentY = Mathf.Lerp(baseOffset.y, baseOffset.y + heavyAttackHeight, t);
            activeModelTransform.localPosition = new Vector3(baseOffset.x, currentY, baseOffset.z);
            yield return null;
        }

        yield return new WaitForSeconds(0.15f);

        // --- ZEMİNE ÇAKILMA ---
        elapsed = 0f;
        float dropDuration = 0.15f;

        while (elapsed < dropDuration)
        {
            if (isDead || isTakingDamage) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / dropDuration;

            float currentY = Mathf.Lerp(baseOffset.y + heavyAttackHeight, baseOffset.y, t);
            activeModelTransform.localPosition = new Vector3(baseOffset.x, currentY, baseOffset.z);
            yield return null;
        }

        activeModelTransform.localPosition = baseOffset;

        // --- ALAN HASARI VE YEŞİL PARTICLE TETİKLENME ANI ---
        if (!isDead && !isTakingDamage)
        {
            Debug.Log("💥 BOOOM! Devasa Boss yere çakıldı, şok dalgası yayılıyor!");

            if (heavyAttackEffect != null)
            {
                Vector3 spawnEffectPos = new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z);
                GameObject goEffect = Instantiate(heavyAttackEffect, spawnEffectPos, Quaternion.identity);

                // === DÜZELTİLDİ: SAHNEDEKİ BAĞIMSIZ EFECT SCALE ORANINI RADIUS İLE BİREBİR SENKRONİZE EDİYORUZ ===
                // Prefab dışarıda 1 ölçekte doğduğu için, onu tam hasar yarıçapının kaplayacağı matematiksel boyuta sike sike çekiyoruz kanka!
                float finalScale = heavyAttackRadius * 2f; // Çap hesabı (Yarıçapın 2 katı kaplasın diye)
                goEffect.transform.localScale = new Vector3(finalScale, 1f, finalScale);

                // Shuriken Shaper koruması: Eğer sistem particle componentine sahipse onun da iç yarıçapını zorla genişletelim
                ParticleSystem ps = goEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var shape = ps.shape;
                    shape.radius = heavyAttackRadius;
                }

                Destroy(goEffect, 3.5f);
            }

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, heavyAttackRadius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    IDamageable playerDamage = hitCollider.GetComponent<IDamageable>();
                    if (playerDamage != null)
                    {
                        playerDamage.TakeDamage(heavyAttackDamage);
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.6f);
    }

    void FindActivePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.activeInHierarchy)
            {
                targetPlayer = p.transform;
                isDonActive = p.name.Contains("Don") || p.GetComponent<DonMovement>() != null;
                return;
            }
        }
    }

    void CheckRealityVisibility()
    {
        if (slimeModel != null)
        {
            slimeModel.SetActive(isDonActive);
            if (isDonActive && !isAttacking)
            {
                slimeModel.transform.localPosition = slimePositionOffset;
                slimeModel.transform.localRotation = Quaternion.Euler(slimeRotationOffset);
            }
        }

        if (mouseModel != null)
        {
            mouseModel.SetActive(!isDonActive);
            if (!isDonActive && !isAttacking)
            {
                mouseModel.transform.localPosition = mousePositionOffset;
                mouseModel.transform.localRotation = Quaternion.Euler(mouseRotationOffset);
            }
        }

        if (agent != null)
        {
            agent.speed = isDonActive ? 3.5f : 4.8f;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log("👑 BOSS CANI: " + currentHealth + " / " + maxHealth);

        if (currentHealth <= 0) Die();
        else StartCoroutine(DamageRoutine());
    }

    private IEnumerator DamageRoutine()
    {
        isTakingDamage = true;
        agent.isStopped = true;

        if (isDonActive && slimeAnimator != null) slimeAnimator.SetTrigger(slimeDamageTrigger);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetTrigger(mouseDamageTrigger);

        yield return new WaitForSeconds(0.4f);

        if (!isDead) isTakingDamage = false;
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

        Debug.Log("🏆 BOSS İNDİRİLDİ! Bölüm tamamlanıyor kanka!");
        Destroy(gameObject, 4f);
    }

    void SetMovingAnimation(bool isMoving)
    {
        if (isDonActive && slimeAnimator != null) slimeAnimator.SetBool(slimeWalkingBool, isMoving);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetBool(mouseWalkingBool, isMoving);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, heavyAttackRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, heavyAttackRange);
    }
}