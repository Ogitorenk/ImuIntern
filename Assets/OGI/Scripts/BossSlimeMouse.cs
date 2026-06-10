using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class BossSlimeMouse : MonoBehaviour, IDamageable
{
    [Header("Boss Evre Ayarları (Bölünme)")]
    [Tooltip("Bu objenin bir klon (yavru) olup olmadığını belirler.")]
    public bool isClone = false;
    [Tooltip("Boss bölündüğünde fırlayacak yavruların ne kadar uzağa saçılacağını ayarlar.")]
    public float splitSpreadRadius = 3f;

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
    public float maxHealth = 500f;
    private float currentHealth;
    private bool isDead = false;
    private bool isTakingDamage = false;

    [Header("Hedef ve Takip")]
    public Transform targetPlayer;
    public float chaseRange = 25f;
    private NavMeshAgent agent;

    [Header("Saldırı Ayarları (Normal)")]
    public float attackRange = 5.0f;
    public float attackDamage = 15f;
    public float attackCooldown = 2f;
    public float attackHitDelay = 0.3f;

    [Header("Boss Özel Saldırı Ayarları (Heavy Attack)")]
    public float heavyAttackRange = 12.0f;
    public string heavyAttackTrigger = "HeavyAttack";
    public float heavyAttackDamage = 35f;
    [Tooltip("Boss havaya yükserken çıkacağı maksimum yükseklik")]
    public float heavyAttackHeight = 6f;
    [Tooltip("Yere çakıldığında hasar vereceği alanın yarıçapı")]
    public float heavyAttackRadius = 8f;
    [Tooltip("Boss havada oyuncunun üzerine doğru ne kadar hızlı kaysın?")]
    public float heavyAttackLeapSpeed = 15f;

    [Tooltip("Tam kafaya atlamasın diye hedeften ne kadar sapacağını ayarlar (Metre).")]
    public float targetLeapOffset = 2.0f;

    [Tooltip("Yere çakılınca doğacak o yeşil particle prefabını buraya at kanka")]
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

        // === BOSS SCALE AYARI ===
        if (!isClone)
        {
            transform.localScale = new Vector3(4f, 4f, 4f);
        }

        FindActivePlayer();
        CheckRealityVisibility();

        agent.speed = isClone ? 4.5f : 3.5f;
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

        // Oyuncu boss'un kafasındaysa ezme mantığı
        if (distanceToPlayer <= (attackRange * 0.6f) && heightDifference > (transform.localScale.y * 0.5f))
        {
            if (Time.time >= lastAttackTime + attackCooldown && !isTakingDamage && !isAttacking)
            {
                StartCoroutine(TriggerSpecificAttack(true));
                return;
            }
        }

        if (isTakingDamage || isAttacking)
        {
            agent.isStopped = true;
            SetMovingAnimation(false);
            return;
        }

        // --- DINAMIK IHTIMAL MATRISI ---
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            float randomRoll = Random.Range(0f, 100f);

            if (distanceToPlayer >= heavyAttackRange && distanceToPlayer <= chaseRange)
            {
                if (randomRoll <= 50f)
                {
                    agent.isStopped = true;
                    SetMovingAnimation(false);
                    LookAtPlayer();
                    StartCoroutine(TriggerSpecificAttack(true));
                    return;
                }
            }
            else if (distanceToPlayer > attackRange && distanceToPlayer < heavyAttackRange)
            {
                if (randomRoll <= 30f)
                {
                    agent.isStopped = true;
                    SetMovingAnimation(false);
                    LookAtPlayer();
                    StartCoroutine(TriggerSpecificAttack(true));
                    return;
                }
            }
            else if (distanceToPlayer <= attackRange)
            {
                agent.isStopped = true;
                SetMovingAnimation(false);
                LookAtPlayer();

                if (randomRoll <= 10f) StartCoroutine(TriggerSpecificAttack(true));
                else StartCoroutine(TriggerSpecificAttack(false));
                return;
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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 0.25f);
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

    private IEnumerator NormalAttackRoutine()
    {
        LookAtPlayer();

        if (isDonActive && slimeAnimator != null) slimeAnimator.SetTrigger(slimeAttackTrigger);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetTrigger(mouseAttackTrigger);

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && !isTakingDamage && targetPlayer != null)
        {
            float heightDifference = targetPlayer.position.y - transform.position.y;
            Vector3 enemyPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 playerPos = new Vector3(targetPlayer.position.x, 0f, targetPlayer.position.z);
            float horizontalDistance = Vector3.Distance(enemyPos, playerPos);

            if (horizontalDistance <= attackRange + 1.0f || (horizontalDistance <= (attackRange * 0.6f) && heightDifference > 2.0f))
            {
                IDamageable damageableTarget = targetPlayer.GetComponent<IDamageable>();
                if (damageableTarget != null) damageableTarget.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(0.5f);
        ForceResetModelPosition();
    }

    private IEnumerator HeavyAttackRoutine()
    {
        LookAtPlayer();

        Vector2 randomOffsetCircle = Random.insideUnitCircle.normalized * targetLeapOffset;
        Vector3 targetLandingPoint = new Vector3(
            targetPlayer.position.x + randomOffsetCircle.x,
            transform.position.y,
            targetPlayer.position.z + randomOffsetCircle.y
        );

        if (isDonActive && slimeAnimator != null) slimeAnimator.SetTrigger(heavyAttackTrigger);
        else if (!isDonActive && mouseAnimator != null) mouseAnimator.SetTrigger(heavyAttackTrigger);

        Transform activeModelTransform = isDonActive ? slimeModel.transform : mouseModel.transform;
        Vector3 baseOffset = isDonActive ? slimePositionOffset : mousePositionOffset;

        float duration = 0.5f;
        float elapsed = 0f;

        // --- HAVAYA YÜKSELİŞ VE KAYMA ---
        while (elapsed < duration)
        {
            if (isDead || isTakingDamage) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float currentY = Mathf.Lerp(baseOffset.y, baseOffset.y + heavyAttackHeight, t);
            activeModelTransform.localPosition = new Vector3(baseOffset.x, currentY, baseOffset.z);

            transform.position = Vector3.MoveTowards(transform.position, targetLandingPoint, heavyAttackLeapSpeed * Time.deltaTime);
            yield return null;
        }

        float hangTime = 0.15f;
        float hangElapsed = 0f;
        while (hangElapsed < hangTime)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetLandingPoint, heavyAttackLeapSpeed * Time.deltaTime);
            hangElapsed += Time.deltaTime;
            yield return null;
        }

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

            transform.position = Vector3.MoveTowards(transform.position, targetLandingPoint, heavyAttackLeapSpeed * 2f * Time.deltaTime);
            yield return null;
        }

        transform.position = targetLandingPoint;
        activeModelTransform.localPosition = baseOffset;

        // --- SARSINTI DALGASI VE HASAR ANI ---
        if (!isDead && !isTakingDamage)
        {
            if (heavyAttackEffect != null)
            {
                Vector3 spawnEffectPos = new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z);
                GameObject goEffect = Instantiate(heavyAttackEffect, spawnEffectPos, Quaternion.identity);

                float finalScale = heavyAttackRadius * 2f;
                goEffect.transform.localScale = new Vector3(finalScale, 1f, finalScale);

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
                    // === GÜNCELLENDİ: DON VE SANCHO ZEMİN DURUMU KONTROLLERİ ===
                    bool playerIsGrounded = true;

                    DonMovement donMove = hitCollider.GetComponent<DonMovement>();
                    SanchoMovement sanchoMove = hitCollider.GetComponent<SanchoMovement>();

                    if (donMove != null)
                    {
                        playerIsGrounded = donMove.isGrounded;
                    }
                    else if (sanchoMove != null)
                    {
                        playerIsGrounded = sanchoMove.isGrounded; // Sancho'nun zemin kontrolü buraya kilitlendi kanka!
                    }
                    else
                    {
                        CharacterController cc = hitCollider.GetComponent<CharacterController>();
                        if (cc != null) playerIsGrounded = cc.isGrounded;
                    }

                    if (playerIsGrounded)
                    {
                        IDamageable playerDamage = hitCollider.GetComponent<IDamageable>();
                        if (playerDamage != null) playerDamage.TakeDamage(heavyAttackDamage);
                    }
                    else
                    {
                        Debug.Log("🛡️ Sarsıntı anında havadasın! Hasar savuşturuldu kanka!");
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.6f);
        ForceResetModelPosition();
        if (agent.enabled) agent.Warp(transform.position);
    }

    void ForceResetModelPosition()
    {
        if (slimeModel != null && isDonActive)
        {
            slimeModel.transform.localPosition = slimePositionOffset;
            slimeModel.transform.localRotation = Quaternion.Euler(slimeRotationOffset);
        }
        if (mouseModel != null && !isDonActive)
        {
            mouseModel.transform.localPosition = mousePositionOffset;
            mouseModel.transform.localRotation = Quaternion.Euler(mouseRotationOffset);
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
            agent.speed = isDonActive ? (isClone ? 4.5f : 3.5f) : (isClone ? 5.8f : 4.8f);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + " Canı: " + currentHealth + " / " + maxHealth);

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
        ForceResetModelPosition();
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

        Debug.Log("💀 " + gameObject.name + " öldü!");

        if (!isClone)
        {
            SplitBossIntoClones();
        }

        Destroy(gameObject, 2.5f);
    }

    void SplitBossIntoClones()
    {
        Debug.Log("🔄 👑 ANA BOSS BÖLÜNÜYOR! Yavrular fırlatılıyor kanka!");

        for (int i = 0; i < 2; i++)
        {
            Vector2 randomSpread = Random.insideUnitCircle.normalized * splitSpreadRadius;
            Vector3 spawnPos = new Vector3(transform.position.x + randomSpread.x, transform.position.y, transform.position.z + randomSpread.y);

            GameObject cloneGo = Instantiate(gameObject, spawnPos, transform.rotation);

            BossSlimeMouse cloneScript = cloneGo.GetComponent<BossSlimeMouse>();
            if (cloneScript != null)
            {
                cloneScript.isClone = true;

                cloneScript.maxHealth = maxHealth / 2f;
                cloneScript.attackDamage = attackDamage / 2f;
                cloneScript.heavyAttackDamage = heavyAttackDamage / 2f;
                cloneScript.attackRange = attackRange / 2f;
                cloneScript.heavyAttackRange = heavyAttackRange / 1.5f;
                cloneScript.heavyAttackRadius = heavyAttackRadius / 2f;

                cloneGo.transform.localScale = new Vector3(2f, 2f, 2f);
                cloneScript.isDead = false;

                Collider cloneCol = cloneGo.GetComponent<Collider>();
                if (cloneCol != null) cloneCol.enabled = true;

                NavMeshAgent cloneAgent = cloneGo.GetComponent<NavMeshAgent>();
                if (cloneAgent != null)
                {
                    cloneAgent.enabled = true;
                    cloneAgent.isStopped = false;
                }
            }
        }
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