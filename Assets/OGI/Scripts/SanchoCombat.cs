using UnityEngine;
using System.Collections;
using Cinemachine; // KAMERA İÇİN GEREKLİ KÜTÜPHANE

public class SanchoCombat : MonoBehaviour
{
    private SanchoMovement sanchoMovement;
    private Animator animator;

    [Header("Görsel Silahlar")]
    public GameObject meleeWeaponPivot;
    [Tooltip("Elde/Sırtta belirecek olan Quiver (Yay/Sadak) objesi")]
    public GameObject bowPivot;

    // ==========================================
    // DON STİLİ KAMERA AYARLARI (YENİ EKLENDİ)
    // ==========================================
    [Header("Nişan Alma (Kamera Zoom & Kaydırma)")]
    public CinemachineFreeLook normalCamera; // Sancho'nun kullandığı FreeLook Kamera

    public float normalFOV = 40f;
    public float aimFOV = 20f;

    [Tooltip("Nişan alırken karakteri sağa almak için negatif (-1), sola almak için pozitif (1)")]
    public float aimOffsetX = -1f;

    [Tooltip("Nişan alırken kamerayı ne kadar yukarı kaldıracağını belirler (Örn: 0.5 veya 1.2)")]
    public float aimOffsetY = 0.8f;

    public float zoomSpeed = 10f;
    private float currentOffsetX = 0f;
    private float currentOffsetY = 0f;

    private float[] baseOffsetX = new float[3];
    private float[] baseOffsetY = new float[3];

    [Header("Yakın Dövüş Kombo Ayarları")]
    public float comboResetTime = 1.0f;
    public float attack1Duration = 1.0f;
    public float attack2Duration = 1.0f;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    [HideInInspector] public bool isAttacking = false;
    private Coroutine attackResetRoutine;

    // ========================================================
    // --- YENİ EKLENDİ: SANCHO YAKIN DÖVÜŞ HASAR AYARLARI ---
    // ========================================================
    [Header("--- Sancho Yakın Dövüş Hasar Ayarları ---")]
    [Tooltip("Sancho'nun önünde duracak ve vuruşun merkez noktasını belirleyecek boş obje")]
    public Transform attackPoint;
    [Tooltip("Vuruşun menzili (Menzil küresinin yarıçapı)")]
    public float attackRange = 1.3f; // Sancho biraz daha kısa boylu olduğu için menzili çıtırık küçük tuttuk kanka
    [Tooltip("Kılıç/Topuz savurunca verilecek yakın dövüş hasarı")]
    public float meleeDamage = 20f; // Sadık yaverimiz 20 vursun şimdilik
    [Tooltip("Sol tık bastıktan kaç saniye sonra hasar düşmana işlesin? (Vuruş gecikmesi)")]
    public float hitDelay = 0.2f;

    [Header("Okçuluk Ayarları")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float arrowForce = 40f;
    public float fireRate = 1.5f;

    [HideInInspector] public bool isAiming = false;
    private float lastFireTime = 0f;

    void Start()
    {
        sanchoMovement = GetComponent<SanchoMovement>();
        animator = GetComponentInChildren<Animator>();

        if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(false);
        if (bowPivot != null) bowPivot.SetActive(false);

        // --- KAMERANIN ORİJİNAL RİG AYARLARINI KAYDET ---
        if (normalCamera != null)
        {
            normalCamera.m_Lens.FieldOfView = normalFOV;
            currentOffsetX = 0f;
            currentOffsetY = 0f;

            for (int i = 0; i < 3; i++)
            {
                var composer = normalCamera.GetRig(i).GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    baseOffsetX[i] = composer.m_TrackedObjectOffset.x;
                    baseOffsetY[i] = composer.m_TrackedObjectOffset.y;
                }
            }
        }
    }

    void Update()
    {
        if (!sanchoMovement.isControlled || sanchoMovement.isDrinking || sanchoMovement.isRepairing ||
            sanchoMovement.isZiplining || sanchoMovement.isDodging || sanchoMovement.isCrawling ||
            sanchoMovement.isCrouchToggled || sanchoMovement.isHoldingBox)
        {
            isAiming = false;
            if (animator != null) animator.SetBool("isAiming", false);
            if (bowPivot != null) bowPivot.SetActive(false);
            if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(false);

            HandleCameraZoomAndOffset(); // Güvenlik: Kamera merkeze dönsün
            return;
        }

        HandleAiming();
        HandleMeleeAttack();
        HandleCameraZoomAndOffset(); // Her karede kameranın zoom'unu/kaymasını denetle
    }

    void HandleAiming()
    {
        if (Input.GetMouseButton(1) && !isAttacking && sanchoMovement.isGrounded)
        {
            isAiming = true;
            if (bowPivot != null) bowPivot.SetActive(true);
            if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(true);

            if (Input.GetMouseButtonDown(0) && Time.time >= lastFireTime + fireRate)
            {
                FireArrow();
            }
        }
        else
        {
            isAiming = false;
            if (bowPivot != null) bowPivot.SetActive(false);
            if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(false);
        }
    }

    void HandleCameraZoomAndOffset()
    {
        if (normalCamera == null) return;

        float targetFOV = isAiming ? aimFOV : normalFOV;
        float targetOffsetX = isAiming ? aimOffsetX : 0f;
        float targetOffsetY = isAiming ? aimOffsetY : 0f;

        normalCamera.m_Lens.FieldOfView = Mathf.Lerp(normalCamera.m_Lens.FieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        currentOffsetX = Mathf.Lerp(currentOffsetX, targetOffsetX, Time.deltaTime * zoomSpeed);
        currentOffsetY = Mathf.Lerp(currentOffsetY, targetOffsetY, Time.deltaTime * zoomSpeed);

        for (int i = 0; i < 3; i++)
        {
            var composer = normalCamera.GetRig(i).GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                Vector3 offset = composer.m_TrackedObjectOffset;
                offset.x = baseOffsetX[i] + currentOffsetX;
                offset.y = baseOffsetY[i] + currentOffsetY;
                composer.m_TrackedObjectOffset = offset;
            }
        }
    }

    // ==============================================================================================
    // --- GÜNCELLENDİ: KAMERA OFFSET HATASINI SIFIRLAYAN KESKİN NİŞANCI ATIŞ SİSTEMİ ---
    // ==============================================================================================
    void FireArrow()
    {
        lastFireTime = Time.time;
        if (animator != null) animator.SetTrigger("FireArrow");

        if (arrowPrefab != null && firePoint != null)
        {
            // 1. Ekranın tam ortasından (Crosshair'dan) sonsuza giren bir ışın atıyoruz
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 targetPoint;

            int layerMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

            // Işın bir yere çarparsa hedef noktamız orası, çarpmazsa kameranın 200 metre ilerisindeki hayali nokta
            if (Physics.Raycast(ray, out hit, 200f, layerMask))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(200f);
            }

            // 2. KAMERA OFFSET ÇÖZÜMÜ: Yönü kamera açısına göre değil, eldeki yayın ucundan (firePoint) hedef noktaya doğru hesapla!
            Vector3 direction = (targetPoint - firePoint.position).normalized;

            // BUG FIX: Oku sapıtan o yapay "direction.y += 0.04f;" bükme satırını sildik! Tam crosshair'ın ortasına gitsin.

            // 3. Oku fırlat ve yönünü tam bu düz çizgiye kilitle kanka
            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.LookRotation(direction));
            Rigidbody rb = arrow.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.velocity = direction * arrowForce;
            }
        }
    }

    void HandleMeleeAttack()
    {
        if (Input.GetMouseButtonDown(0) && !isAiming && sanchoMovement.isGrounded)
        {
            if (Time.time - lastAttackTime > comboResetTime) comboStep = 0;

            comboStep++;
            lastAttackTime = Time.time;

            if (attackResetRoutine != null) StopCoroutine(attackResetRoutine);

            if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(true);

            if (comboStep == 1)
            {
                animator.ResetTrigger("Attack2");
                animator.SetTrigger("Attack1");
                isAttacking = true;
                if (animator != null) animator.SetBool("isAttacking", true);

                StartCoroutine(DealMeleeDamageWithDelay(hitDelay));

                attackResetRoutine = StartCoroutine(ResetAttackState(attack1Duration));
            }
            else if (comboStep >= 2)
            {
                animator.ResetTrigger("Attack1");
                animator.SetTrigger("Attack2");
                isAttacking = true;
                if (animator != null) animator.SetBool("isAttacking", true);
                comboStep = 0;

                StartCoroutine(DealMeleeDamageWithDelay(hitDelay));

                attackResetRoutine = StartCoroutine(ResetAttackState(attack2Duration));
            }
        }
    }

    private IEnumerator DealMeleeDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (attackPoint == null)
        {
            Debug.LogError("🚨 KANKA! SanchoCombat içindeki 'Attack Point' kutusu boş! Sancho'nun önüne boş bir obje açıp bağla!");
            yield break;
        }

        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange);

        foreach (Collider enemyCollider in hitEnemies)
        {
            if (enemyCollider.gameObject.CompareTag("Player")) continue;

            IDamageable enemy = enemyCollider.GetComponent<IDamageable>();
            if (enemy == null) enemy = enemyCollider.GetComponentInParent<IDamageable>();
            if (enemy == null) enemy = enemyCollider.GetComponentInChildren<IDamageable>();

            if (enemy != null)
            {
                enemy.TakeDamage(meleeDamage);
                Debug.Log($"⚔️ Sancho yakın dövüşle {enemyCollider.name} objesine {meleeDamage} hasar verdi!");
            }
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

        if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(false);
        comboStep = 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}