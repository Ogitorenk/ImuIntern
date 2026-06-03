using UnityEngine;
using System.Collections;

public class DonCombat : MonoBehaviour
{
    private DonMovement donMovement;
    private Animator animator;

    // ========================================================
    // --- GÜNCELLENDİ: GÖRSEL SİLAH PİVOT SİSTEMİ ---
    // ========================================================
    [Header("Görsel Silahlar (Kemik İçindeki Modeller)")]
    [Tooltip("Saldırırken elde belirecek mızrağın PİVOT (Yalancı Parent) objesi")]
    public GameObject meleeLancePivot;

    [Tooltip("Kalkan açarken belirecek kalkan (Model gelince buraya atarsın)")]
    public GameObject shieldModel;

    // ========================================================
    // --- YENİ EKLENDİ: KALKAN SCRIPT BAĞLANTISI ---
    // ========================================================
    [Header("Kalkan Hesaplama Ayarları")]
    [Tooltip("Kalkanın üzerine attığımız o Shield.cs scriptini buraya sürükle kanka")]
    public Shield shieldScript;
    // ========================================================

    [Header("Yakın Dövüş Kombo Ayarları")]
    public float comboResetTime = 1.0f;
    public float attack1Duration = 0.5f;
    public float attack2Duration = 0.7f;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    [HideInInspector] public bool isAttacking = false;

    [Header("Kalkan (Blok) Ayarları")]
    public KeyCode blockKey = KeyCode.Mouse2;
    [HideInInspector] public bool isBlocking = false;

    // ========================================================
    // --- İLK KEZ EKLEYECEĞİN YAKIN DÖVÜŞ HASAR AYARLARI ---
    // ========================================================
    [Header("--- Yakın Dövüş Hasar Ayarları ---")]
    [Tooltip("Don'un önünde duracak ve vuruşun merkez noktasını belirleyecek boş obje")]
    public Transform attackPoint;
    [Tooltip("Vuruşun menzili (Menzil küresinin yarıçapı)")]
    public float attackRange = 1.5f;
    [Tooltip("Kılıç/Mızrak savurunca verilecek yakın dövüş hasarı")]
    public float meleeDamage = 25f;
    [Tooltip("Sol tık bastıktan kaç saniye sonra hasar düşmana işlesin? (Vuruş gecikmesi)")]
    public float hitDelay = 0.2f;

    private Coroutine attackResetRoutine;

    void Start()
    {
        donMovement = GetComponent<DonMovement>();
        animator = GetComponentInChildren<Animator>();

        // Başlangıçta dövüş halinde olmadığımız için silahları gizle
        if (meleeLancePivot != null) meleeLancePivot.SetActive(false);
        if (shieldModel != null) shieldModel.SetActive(false);
    }

    void Update()
    {
        if (!donMovement.isControlled || donMovement.isDrinking || donMovement.isZiplining ||
            donMovement.isDodging || donMovement.isCrawling || donMovement.isCrouchToggled || donMovement.isLatched)
        {
            isBlocking = false;
            if (animator != null) animator.SetBool("isBlocking", false);

            // Kontrol bizde değilse kalkanı zorla kapat ve durumunu sıfırla
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
            if (Time.time - lastAttackTime > comboResetTime)
            {
                comboStep = 0;
            }

            comboStep++;
            lastAttackTime = Time.time;

            if (attackResetRoutine != null) StopCoroutine(attackResetRoutine);

            // --- ATAK BAŞLADI: MIZRAK PİVOTUNU GÖSTER ---
            if (meleeLancePivot != null) meleeLancePivot.SetActive(true);

            if (comboStep == 1)
            {
                animator.ResetTrigger("Attack2");
                animator.SetTrigger("Attack1");

                isAttacking = true;
                if (animator != null) animator.SetBool("isAttacking", true);

                StartCoroutine(DealDamageWithDelay(hitDelay));

                attackResetRoutine = StartCoroutine(ResetAttackState(attack1Duration));
            }
            else if (comboStep >= 2)
            {
                animator.ResetTrigger("Attack1");
                animator.SetTrigger("Attack2");

                isAttacking = true;
                if (animator != null) animator.SetBool("isAttacking", true);

                comboStep = 0;

                StartCoroutine(DealDamageWithDelay(hitDelay));

                attackResetRoutine = StartCoroutine(ResetAttackState(attack2Duration));
            }
        }
    }

    // ==============================================================================================
    // --- GÜNCELLENDİ: KUTULARI VE VAZOLARI PARÇALAYAN HASAR SİSTEMİ ---
    // ==============================================================================================
    private IEnumerator DealDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (attackPoint == null)
        {
            Debug.LogError("🚨 KANKA! DonCombat içindeki 'Attack Point' kutusu boş! Don'un önüne boş bir obje açıp bağla!");
            yield break;
        }

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);

        foreach (Collider enemyCollider in hitEnemies)
        {
            if (enemyCollider.gameObject.CompareTag("Player")) continue;

            // --- YENİ KONTROL: KIRILABİLİR KUTU KONTROLÜ ---
            // Don mızrağı savurduğunda alan içinde kırılabilir kutu varsa direkt patlatsın kanka!
            SharedBreakableObject breakable = enemyCollider.GetComponent<SharedBreakableObject>();
            if (breakable != null)
            {
                breakable.BreakIt();
                continue; // Kutuyu kırdık, düşman hasar kontrolüne girmeden sıradaki collider'a geç kanka
            }

            IDamageable enemy = enemyCollider.GetComponent<IDamageable>();
            if (enemy == null) enemy = enemyCollider.GetComponentInParent<IDamageable>();
            if (enemy == null) enemy = enemyCollider.GetComponentInChildren<IDamageable>();

            if (enemy != null)
            {
                enemy.TakeDamage(meleeDamage);
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
            if (shieldScript != null) shieldScript.SetShieldStatus(true); // Kalkanı aktif et
        }
        else
        {
            isBlocking = false;
            if (animator != null) animator.SetBool("isBlocking", false);

            if (shieldModel != null) shieldModel.SetActive(false);
            if (shieldScript != null) shieldScript.SetShieldStatus(false); // Kalkanı kapat
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