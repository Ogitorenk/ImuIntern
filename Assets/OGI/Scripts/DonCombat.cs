using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DonCombat : MonoBehaviour
{
    private DonMovement donMovement;
    private Animator animator;

    [Header("Görsel Silahlar (Kemik İçindeki Modeller)")]
    [Tooltip("Saldırırken elde belirecek mızrağın PİVOT (Yalancı Parent) objesi")]
    public GameObject meleeLancePivot;

    [Tooltip("Kalkan açarken belirecek kalkan (Model gelince buraya atarsın)")]
    public GameObject shieldModel;

    [Header("Kalkan Hesaplama Ayarları")]
    [Tooltip("Kalkanın üzerine attığımız o Shield.cs scriptini buraya sürükle kanka")]
    public Shield shieldScript;

    [Header("Yakın Dövüş Kombo Ayarları")]
    public float comboResetTime = 1.0f;
    public float attack1Duration = 0.5f;
    public float attack2Duration = 0.7f;

    // === GÜNCELLENDİ: SABİTLENEN YENİ STAMINA TÜKETİM MALİYETLERİ ===
    [Header("Kombo Stamina Maliyetleri")]
    public float attack1StaminaCost = 10f; // Tam istediğin gibi 10 kanka
    public float attack2StaminaCost = 15f; // Tam istediğin gibi 15 kanka
    private float minimumAttackThreshold = 10f; // 10'un altındaysa vuruş engellenecek kanka

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    [HideInInspector] public bool isAttacking = false;

    [Header("Kalkan (Blok) Ayarları")]
    public KeyCode blockKey = KeyCode.Mouse2;
    [HideInInspector] public bool isBlocking = false;

    [Header("--- Yakın Dövüş Hasar Ayarları ---")]
    [Tooltip("Don'un önünde duracak ve vuruşun merkez noktasını belirleyecek boş obje")]
    public Transform attackPoint;
    [Tooltip("Vuruşun menzili (Menzil küresinin yarıçapı)")]
    public float attackRange = 1.5f;
    [Tooltip("Kılıç/Mızrak savurunca verilecek yakın dövüş hasarı")]
    public float meleeDamage = 25f;
    [Tooltip("Sol tık bastıktan kaç saniye sonra hasar düşmana işlesin? (Vuruş gecikmesi)")]
    public float hitDelay = 0.2f;

    private Dictionary<IDamageable, float> enemyHitCooldowns = new Dictionary<IDamageable, float>();
    private float globalHitCooldown = 0.25f;

    private Coroutine attackResetRoutine;

    void Start()
    {
        donMovement = GetComponent<DonMovement>();
        animator = GetComponentInChildren<Animator>();

        if (meleeLancePivot != null) meleeLancePivot.SetActive(false);
        if (shieldModel != null) shieldModel.SetActive(false);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (!donMovement.isControlled || donMovement.currentHealth <= 0 || donMovement.isDrinking ||
            donMovement.isZiplining || donMovement.isDodging || donMovement.isCrawling ||
            donMovement.isCrouchToggled || donMovement.isLatched)
        {
            isBlocking = false;
            if (animator != null) animator.SetBool("isBlocking", false);

            if (shieldModel != null) shieldModel.SetActive(false);
            if (shieldScript != null) shieldScript.SetShieldStatus(false);
            return;
        }

        HandleBlocking();
        HandleMeleeAttack();
    }

    void HandleMeleeAttack()
    {
        bool isAiming = Input.GetMouseButton(1);

        if (Input.GetMouseButtonDown(0) && !isAiming && !isBlocking && donMovement.isGrounded)
        {
            // === CRITICAL BUGFIX: 10'UN ALTINDAYSA HİÇBİR ŞEY YAPMA VE STAMINA HARCAMA ===
            if (donMovement.currentStamina < minimumAttackThreshold)
            {
                Debug.LogWarning($"<color=red>🛑 ATAK ENGELLENDİ! </color> Stamina 10'un altında! | <color=yellow>Mevcut Stamina: {Mathf.RoundToInt(donMovement.currentStamina)}</color>");
                return; // Stamina harcamadan ve kodu tetiklemeden direkt çıkıyoruz kanka!
            }

            int nextStep = comboStep;
            if (Time.time - lastAttackTime > comboResetTime)
            {
                nextStep = 0;
            }
            nextStep++;

            float requiredStamina = (nextStep == 1) ? attack1StaminaCost : attack2StaminaCost;

            // Üstteki 10 kontrolünden geçse bile, eğer tam gereken stamina yetmiyorsa (Örn: Stamina 12 ama Atak2 için 15 lazım) koruma kalkanı kanka
            if (donMovement.currentStamina < requiredStamina)
            {
                Debug.LogWarning($"<color=orange>⚠️ STAMINA YETERSİZ! </color> Kombo {nextStep} için gereken: {requiredStamina} | <color=yellow>Mevcut Stamina: {Mathf.RoundToInt(donMovement.currentStamina)}</color>");
                return;
            }

            comboStep = nextStep;
            lastAttackTime = Time.time;

            if (attackResetRoutine != null) StopCoroutine(attackResetRoutine);

            if (meleeLancePivot != null) meleeLancePivot.SetActive(true);

            isAttacking = true;
            if (animator != null) animator.SetBool("isAttacking", true);

            // Staminayı pürüzsüzce harca kanka
            donMovement.UseStamina(requiredStamina);

            Debug.Log($"<color=cyan>⚔️ Atak Başarılı (Kombo {comboStep}) -> </color> Harcanan Stamina: {requiredStamina} | <color=green>Kalan Stamina: {Mathf.RoundToInt(donMovement.currentStamina)}</color>");

            if (comboStep == 1)
            {
                animator.ResetTrigger("Attack2");
                animator.SetTrigger("Attack1");

                StartCoroutine(DealDamageWithDelay(hitDelay));
                attackResetRoutine = StartCoroutine(ResetAttackState(attack1Duration));
            }
            else if (comboStep >= 2)
            {
                animator.ResetTrigger("Attack1");
                animator.SetTrigger("Attack2");

                comboStep = 0;

                StartCoroutine(DealDamageWithDelay(hitDelay));
                attackResetRoutine = StartCoroutine(ResetAttackState(attack2Duration));
            }
        }
    }

    private IEnumerator DealDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (attackPoint == null)
        {
            Debug.LogError("🚨 KANKA! DonCombat içindeki 'Attack Point' boş!");
            yield break;
        }

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider enemyCollider in hitEnemies)
        {
            if (enemyCollider.gameObject.CompareTag("Player")) continue;

            SharedBreakableObject breakable = enemyCollider.GetComponent<SharedBreakableObject>();
            if (breakable == null) breakable = enemyCollider.GetComponentInParent<SharedBreakableObject>();

            if (breakable != null)
            {
                breakable.BreakIt();
                continue;
            }

            IDamageable enemy = enemyCollider.GetComponent<IDamageable>();
            if (enemy == null) enemy = enemyCollider.GetComponentInParent<IDamageable>();
            if (enemy == null) enemy = enemyCollider.GetComponentInChildren<IDamageable>();

            if (enemy != null)
            {
                if (enemyHitCooldowns.ContainsKey(enemy) && Time.time < enemyHitCooldowns[enemy])
                {
                    continue;
                }

                enemy.TakeDamage(meleeDamage);
                enemyHitCooldowns[enemy] = Time.time + globalHitCooldown;

                Debug.Log($"⚔️ Don yakın dövüşle {enemyCollider.name} objesine {meleeDamage} hasar verdi!");
            }
        }
    }

    void HandleBlocking()
    {
        bool isAiming = Input.GetMouseButton(1);

        if (Input.GetKey(blockKey) && !isAttacking && !isAiming && donMovement.isGrounded)
        {
            isBlocking = true;
            if (animator != null) animator.SetBool("isBlocking", true);

            if (shieldModel != null) shieldModel.SetActive(true);
            if (shieldScript != null) shieldScript.SetShieldStatus(true);
        }
        else
        {
            isBlocking = false;
            if (animator != null) animator.SetBool("isBlocking", false);

            if (shieldModel != null) shieldModel.SetActive(false);
            if (shieldScript != null) shieldScript.SetShieldStatus(false);
        }
    }

    private IEnumerator ResetAttackState(float delay)
    {
        float safeDelay = Mathf.Max(0f, delay - 0.15f);

        yield return new WaitForSeconds(safeDelay);

        isAttacking = false;

        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
            animator.ResetTrigger("Attack1");
            animator.ResetTrigger("Attack2");
        }

        if (meleeLancePivot != null) meleeLancePivot.SetActive(false);

        comboStep = 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}