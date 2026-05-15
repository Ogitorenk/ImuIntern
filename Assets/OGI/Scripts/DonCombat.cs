using UnityEngine;
using System.Collections;

public class DonCombat : MonoBehaviour
{
    private DonMovement donMovement;
    private Animator animator;

    // ========================================================
    // --- GÜNCELLENDÝ: GÖRSEL SÝLAH PÝVOT SÝSTEMÝ ---
    // ========================================================
    [Header("Görsel Silahlar (Kemik Ýçindeki Modeller)")]
    [Tooltip("Saldýrýrken elde belirecek mýzraðýn PÝVOT (Yalancý Parent) objesi")]
    public GameObject meleeLancePivot;

    [Tooltip("Kalkan açarken belirecek kalkan (Model gelince buraya atarsýn)")]
    public GameObject shieldModel;
    // ========================================================

    [Header("Yakýn Dövüþ Kombo Ayarlarý")]
    public float comboResetTime = 1.0f;
    public float attack1Duration = 0.5f;
    public float attack2Duration = 0.7f;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    [HideInInspector] public bool isAttacking = false;

    [Header("Kalkan (Blok) Ayarlarý")]
    public KeyCode blockKey = KeyCode.Mouse2;
    [HideInInspector] public bool isBlocking = false;

    private Coroutine attackResetRoutine;

    void Start()
    {
        donMovement = GetComponent<DonMovement>();
        animator = GetComponentInChildren<Animator>();

        // Baþlangýçta dövüþ halinde olmadýðýmýz için silahlarý gizle
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

            // Kontrol bizde deðilse kalkaný zorla kapat
            if (shieldModel != null) shieldModel.SetActive(false);
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

            // --- ATAK BAÞLADI: MIZRAK PÝVOTUNU GÖSTER ---
            if (meleeLancePivot != null) meleeLancePivot.SetActive(true);

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

    void HandleBlocking()
    {
        bool isAiming = Input.GetMouseButton(1);

        if (Input.GetKey(blockKey) && !isAttacking && !isAiming && donMovement.isGrounded)
        {
            isBlocking = true;
            if (animator != null) animator.SetBool("isBlocking", true);

            // --- BLOK BAÞLADI: KALKANI GÖSTER ---
            if (shieldModel != null) shieldModel.SetActive(true);
        }
        else
        {
            isBlocking = false;
            if (animator != null) animator.SetBool("isBlocking", false);

            // --- BLOK BÝTTÝ: KALKANI GÝZLE ---
            if (shieldModel != null) shieldModel.SetActive(false);
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

        // --- ATAK BÝTTÝ: MIZRAK PÝVOTUNU GÝZLE ---
        if (meleeLancePivot != null) meleeLancePivot.SetActive(false);

        comboStep = 0;
    }
}