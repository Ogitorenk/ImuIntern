using UnityEngine;
using System.Collections;
using Cinemachine; // KAMERA ÝÇÝN GEREKLÝ KÜTÜPHANE

public class SanchoCombat : MonoBehaviour
{
    private SanchoMovement sanchoMovement;
    private Animator animator;

    [Header("Görsel Silahlar")]
    public GameObject meleeWeaponPivot;
    [Tooltip("Elde/Sýrtta belirecek olan Quiver (Yay/Sadak) objesi")]
    public GameObject bowPivot;

    // ==========================================
    // DON STÝLÝ KAMERA AYARLARI (YENÝ EKLENDÝ)
    // ==========================================
    [Header("Niþan Alma (Kamera Zoom & Kaydýrma)")]
    public CinemachineFreeLook normalCamera; // Sancho'nun kullandýðý FreeLook Kamera

    public float normalFOV = 40f;
    public float aimFOV = 20f;

    [Tooltip("Niþan alýrken karakteri saða almak için negatif (-1), sola almak için pozitif (1)")]
    public float aimOffsetX = -1f;

    [Tooltip("Niþan alýrken kamerayý ne kadar yukarý kaldýracaðýný belirler (Örn: 0.5 veya 1.2)")]
    public float aimOffsetY = 0.8f;

    public float zoomSpeed = 10f;
    private float currentOffsetX = 0f;
    private float currentOffsetY = 0f;

    private float[] baseOffsetX = new float[3];
    private float[] baseOffsetY = new float[3];

    [Header("Yakýn Dövüþ Kombo Ayarlarý")]
    public float comboResetTime = 1.0f;
    public float attack1Duration = 1.0f;
    public float attack2Duration = 1.0f;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    [HideInInspector] public bool isAttacking = false;
    private Coroutine attackResetRoutine;

    [Header("Okçuluk Ayarlarý")]
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

        // --- KAMERANIN ORÝJÝNAL RÝG AYARLARINI KAYDET ---
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
        HandleCameraZoomAndOffset(); // Her karede kameranýn zoom'unu/kaymasýný denetle
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

    // ==========================================
    // DON STÝLÝ KAMERA KAYDIRMA FONKSÝYONU
    // ==========================================
    void HandleCameraZoomAndOffset()
    {
        if (normalCamera == null) return;

        // Hedef deðerleri belirle (Niþan alýyorsa zoomla ve saða kaydýr)
        float targetFOV = isAiming ? aimFOV : normalFOV;
        float targetOffsetX = isAiming ? aimOffsetX : 0f;
        float targetOffsetY = isAiming ? aimOffsetY : 0f;

        // Yumuþak geçiþ (Lerp)
        normalCamera.m_Lens.FieldOfView = Mathf.Lerp(normalCamera.m_Lens.FieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        currentOffsetX = Mathf.Lerp(currentOffsetX, targetOffsetX, Time.deltaTime * zoomSpeed);
        currentOffsetY = Mathf.Lerp(currentOffsetY, targetOffsetY, Time.deltaTime * zoomSpeed);

        // Deðerleri kameranýn 3 rig'ine (Top, Middle, Bottom) uygula
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

    void FireArrow()
    {
        lastFireTime = Time.time;
        if (animator != null) animator.SetTrigger("FireArrow");

        if (arrowPrefab != null && firePoint != null)
        {
            // Kameranýn ortasýndan ýþýn at
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 targetPoint;

            // Karakterin kendisini, müttefikini ve okun doðan gövdesini (Default ve Player layer'larýný) raycast'ten muaf tutmak en temizi.
            // Sadece Ground (Zemin), Enemy (Düþman) gibi katmanlarý vursun istiyorsan bitwise maske kullanabiliriz.
            // Þimdilik ok kendi collider'ýna çarpmasýn diye ýþýný Sancho'nun biraz ilerisinden baþlatýyoruz ya da layer mask koyuyoruz:
            int layerMask = ~LayerMask.GetMask("Player", "Ignore Raycast"); // Player ve Ignore Raycast layer'larýný görmezden gel

            if (Physics.Raycast(ray, out hit, 200f, layerMask))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(200f);
            }

            // Atýþ yönünü belirle
            Vector3 direction = (targetPoint - firePoint.position).normalized;
            // Oku yapay olarak birazcýk yukarý doðru büker (0.05f deðerini test ederek büyütebilir veya küçültebilirsin)
            direction.y += 0.04f;
            direction = direction.normalized; // Yönü tekrar eþitle

            // Oku fýrlat
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
}