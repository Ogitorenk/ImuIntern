using UnityEngine;
using System.Collections;

public class SanchoCombat : MonoBehaviour
{
    private SanchoMovement sanchoMovement;
    private Animator animator;

    // ========================================================
    // --- GÖRSEL SÝLAHLAR ---
    // ========================================================
    [Header("Görsel Silahlar")]
    [Tooltip("Saldýrýrken elde belirecek hançer/sopa PÝVOT objesi")]
    public GameObject meleeWeaponPivot;

    [Tooltip("Niþan alýrken belirecek Yay (Bow) Pivotu")]
    public GameObject bowPivot;
    [Tooltip("Niþan alýrken elde duracak olan Ok modeli")]
    public GameObject arrowInHand;

    // ========================================================
    // --- YAKIN DÖVÜÞ (MELEE) AYARLARI ---
    // ========================================================
    [Header("Yakýn Dövüþ Kombo Ayarlarý")]
    public float comboResetTime = 1.0f;
    public float attack1Duration = 0.5f;
    public float attack2Duration = 0.7f;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    [HideInInspector] public bool isAttacking = false;
    private Coroutine attackResetRoutine;

    // ========================================================
    // --- OKÇULUK (RANGED) AYARLARI ---
    // ========================================================
    [Header("Okçuluk Ayarlarý")]
    public GameObject arrowPrefab;      // Fýrlatýlacak ok prefabý
    public Transform firePoint;         // Okun çýkacaðý nokta
    public float arrowForce = 40f;
    public float fireRate = 1.5f;

    [HideInInspector] public bool isAiming = false;
    private float lastFireTime = 0f;

    void Start()
    {
        sanchoMovement = GetComponent<SanchoMovement>();
        animator = GetComponentInChildren<Animator>();

        // Baþlangýçta silahlarý gizle
        if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(false);
        if (bowPivot != null) bowPivot.SetActive(false);
        if (arrowInHand != null) arrowInHand.SetActive(false);
    }

    void Update()
    {
        // Kontrol bizde deðilse veya Sancho kilitli bir eylem yapýyorsa (Sürünme, Ýksir, Dodging, Kutu taþýma vs.)
        if (!sanchoMovement.isControlled || sanchoMovement.isDrinking || sanchoMovement.isRepairing ||
            sanchoMovement.isZiplining || sanchoMovement.isDodging || sanchoMovement.isCrawling ||
            sanchoMovement.isCrouchToggled || sanchoMovement.isHoldingBox)
        {
            // Niþan alma durumunu zorla kapat
            isAiming = false;
            if (animator != null) animator.SetBool("isAiming", false);

            if (bowPivot != null) bowPivot.SetActive(false);
            if (arrowInHand != null) arrowInHand.SetActive(false);
            if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(false);

            return; // Savaþ kodlarýný okuma
        }

        HandleAiming();
        HandleMeleeAttack();
    }

    // ========================================================
    // --- NÝÞAN ALMA VE OK ATMA MANTIÐI ---
    // ========================================================
    void HandleAiming()
    {
        // Sað týk: Niþan Al (Yakýn dövüþ yapmýyorken)
        if (Input.GetMouseButton(1) && !isAttacking && sanchoMovement.isGrounded)
        {
            isAiming = true;
            if (bowPivot != null) bowPivot.SetActive(true);
            if (arrowInHand != null) arrowInHand.SetActive(true);
            if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(true);

            // Sol týk: Ateþ Et
            if (Input.GetMouseButtonDown(0) && Time.time >= lastFireTime + fireRate)
            {
                FireArrow();
            }
        }
        else
        {
            isAiming = false;
            if (bowPivot != null) bowPivot.SetActive(false);
            if (arrowInHand != null) arrowInHand.SetActive(false);
            if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(false);
        }
    }

    void FireArrow()
    {
        lastFireTime = Time.time;
        if (animator != null) animator.SetTrigger("FireArrow");

        // Kameranýn merkezinden hedef noktasý belirle
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.GetPoint(100f);

        Vector3 direction = (targetPoint - firePoint.position).normalized;

        if (arrowPrefab != null && firePoint != null)
        {
            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.LookRotation(direction));
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = direction * arrowForce;
        }

        // Animasyon bitene kadar eldeki oku geçici gizle
        StartCoroutine(ArrowVisibilityRoutine());
    }

    IEnumerator ArrowVisibilityRoutine()
    {
        if (arrowInHand != null) arrowInHand.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        if (isAiming && arrowInHand != null) arrowInHand.SetActive(true);
    }

    // ========================================================
    // --- YAKIN DÖVÜÞ (DON KÝÞOT MANTIÐI BÝREBÝR) ---
    // ========================================================
    void HandleMeleeAttack()
    {
        // Sol Týklandý + Niþan alýnmýyor + Yerde
        if (Input.GetMouseButtonDown(0) && !isAiming && sanchoMovement.isGrounded)
        {
            if (Time.time - lastAttackTime > comboResetTime)
            {
                comboStep = 0;
            }

            comboStep++;
            lastAttackTime = Time.time;

            if (attackResetRoutine != null) StopCoroutine(attackResetRoutine);

            // --- ATAK BAÞLADI: MELEE PÝVOTUNU GÖSTER ---
            if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(true);

            if (comboStep == 1)
            {
                animator.ResetTrigger("Attack2");
                animator.SetTrigger("Attack1");

                isAttacking = true;
                if (animator != null) animator.SetBool("isAttacking", true);

                attackResetRoutine = StartCoroutine(ResetAttackState(attack1Duration));
            }
            else if (comboStep >= 2)
            {
                animator.ResetTrigger("Attack1");
                animator.SetTrigger("Attack2");

                isAttacking = true;
                if (animator != null) animator.SetBool("isAttacking", true);

                comboStep = 0;
                attackResetRoutine = StartCoroutine(ResetAttackState(attack2Duration));
            }
        }
    }

    private IEnumerator ResetAttackState(float delay)
    {
        // Unity'nin geçiþ (blend) yapabilmesi için kilidi animasyon bitmeden çok ufak bir süre önce açýyoruz
        float safeDelay = Mathf.Max(0f, delay - 0.15f);

        yield return new WaitForSeconds(safeDelay);

        isAttacking = false;

        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
            animator.ResetTrigger("Attack1");
            animator.ResetTrigger("Attack2");
        }

        // --- ATAK BÝTTÝ: MELEE PÝVOTUNU GÝZLE ---
        if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(false);

        comboStep = 0;
    }
}