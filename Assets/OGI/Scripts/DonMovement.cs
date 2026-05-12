using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class DonMovement : MonoBehaviour
{
    // --- YENİ EKLENDİ: ÖZEL SAHNE KONTROL ŞALTERİ ---
    [Header("Özel Bölüm Kontrolü")]
    public bool isControlled = true;

    // --- YENİ EKLENEN SAĞLIK SİSTEMİ ---
    [Header("Sağlık Sistemi")]
    public float maxHealth = 100f;
    public float currentHealth;
    private float iFrames = 0f;

    [Header("Envanter (Can İksiri)")]
    public int healthPotionCount = 0;
    public float healthPotionHealAmount = 20f;
    public KeyCode healKey = KeyCode.Alpha1; // 1 Tuşu     

    // --- YENİ EKLENDİ: ZAMAN İKSİRİ ---
    [Header("Envanter (Zaman İksiri)")]
    public int slowPotionCount = 0;
    public float slowTimeAmount = 0.5f; // Zamanın hızı (%50 yavaşlar)
    public float slowTimeDuration = 5f; // Gerçek hayatta 5 saniye sürer
    public KeyCode slowTimeKey = KeyCode.Alpha2; // 2 Tuşu
    public static bool isTimePotionActive = false; // Çakışmaları önleyen global şalter

    // --- YENİ EKLENDİ: İKSİR İÇME KİLİDİ ---
    [HideInInspector] public bool isDrinking = false;

    // --- YENİ: PLATFORM FİZİĞİ DEĞİŞKENLERİ ---
    private Transform activePlatform;
    private Vector3 activeLocalPlatformPoint;
    private Vector3 activeGlobalPlatformPoint;
    private Quaternion activeLocalPlatformRotation;
    private Quaternion activeGlobalPlatformRotation;

    [Header("Hareket Ayarları")]
    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    private float referenceYaw;

    [Header("Ekstra Hareket (Koşma/Yürüme/Eğilme)")]
    public float sprintSpeed = 10f;
    public float walkSpeed = 2f;
    public float crouchSpeed = 3f;

    // ========================================================
    // --- GÜNCELLENDİ: SÜRÜNME, KİLİT VE YÜKSEKLİK AYARLARI ---
    // ========================================================
    public float crawlSpeed = 1.5f;
    public float normalHeight = 2f;
    public float crouchHeight = 1f;
    public float crawlHeight = 0.6f;
    public float crouchTransitionSpeed = 10f;

    [Tooltip("Ctrl'ye basıp eğilirken kaç saniye hareket kilitlensin?")]
    public float crouchDelayDuration = 1f; // Inspector'dan ayarlanabilir kilit süresi

    private float currentSpeed;
    private bool isCrouching = false;
    private bool isWalking = false;

    private bool isCrawling = false;
    private float crawlStartTimer = 0f;
    private bool isCrouchToggled = false;

    // Orijinal kapsül merkezini ve ayak tabanını tutacağımız değişkenler
    private Vector3 baseCenter;
    private float baseBottom;
    // ========================================================

    [Header("Zıplama & Fizik")]
    public float jumpHeight = 2f;
    [Range(0.1f, 0.9f)] public float jumpCutMultiplier = 0.5f;
    public float gravity = -19.62f;
    public int maxJumps = 2;
    private int jumpCount;
    private Vector3 velocity;

    [Header("Zemin Kontrolü")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;
    private bool wasGrounded;

    // --- YENİ EKLENDİ: İNİŞ SERSEMLEMESİ (LAND STUN) ---
    [Header("İniş Ayarları (Land)")]
    [Tooltip("Karakter yere indiğinde kaç saniye boyunca hareket edemeyip animasyonun bitmesini beklesin?")]
    public float landStunDuration = 0.15f;
    private float landStunTimer = 0f;

    // --- YENİ EKLENDİ: ERKEN İNİŞ ---
    [Tooltip("Yere ne kadar mesafe kala iniş animasyonu başlasın?")]
    public float nearGroundDistance = 1.2f;
    private bool isNearGround;

    [Header("Mızrak Ayarları")]
    public bool isLanceEquipped = true;
    public GameObject lancePrefab;
    public float throwForce = 100f;

    public GameObject eldekiGorselMizrak;

    [Tooltip("Tıkladıktan kaç saniye sonra mızrak elden çıksın?")]
    public float throwDelay = 0.2f;
    private bool isThrowing = false;

    public float lanceJumpMultiplier = 1f;
    public float latchRadius = 1.5f;

    [Tooltip("Karakter mızrağın neresinden tutunacak? (-1.5 mızrağın altı demektir)")]
    public float lanceHangOffset = -1.5f;

    [Tooltip("Yeni prefab ters duruyorsa bu değerlerle oyna. Eski mızrak için X=90'dı. Yenisinde hepsini 0 yapıp test edebilirsin.")]
    public Vector3 lanceRotationOffset = new Vector3(90f, 0f, 0f);

    [Tooltip("Karakterin duvara girmemesi için mızraktan dışarı doğru (geriye) mesafesi.")]
    public float lanceWallOffset = 0.8f;

    [Tooltip("Karakterin kendi Z ekseninde (ileri/geri) mızrağa göre konumu. Elleri hizalamak için kullan.")]
    public float lanceForwardOffset = 0f;

    [HideInInspector] public bool isLatched = false;
    private Transform latchedLance;
    [HideInInspector] public bool isZiplining = false;

    [Header("Nişan Alma (Tek Kamera Zoom)")]
    public GameObject crosshairUI;
    [Range(0.1f, 1f)] public float slowMotionAmount = 0.3f;
    public CinemachineFreeLook normalCamera;

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

    [Header("Duvar Kırma (Dash / Omuz Atma)")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 10f;
    public GameObject wallBreakEffect;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;

    // ========================================================
    // --- YENİ EKLENDİ: DODGE (KAÇINMA) AYARLARI ---
    // ========================================================
    [Header("Dodge (Kaçınma) Ayarları")]
    public float dodgeSpeed = 15f;
    public float dodgeDuration = 0.4f;
    [Tooltip("Shift'e ne kadar kısa basılırsa dodge sayılacak?")]
    public float shiftTapThreshold = 0.25f;

    private bool isDodging = false;
    private float dodgeTimer = 0f;
    private bool isShiftPressed = false;
    private float shiftPressTimer = 0f;
    // ========================================================

    private CharacterController controller;
    private Transform cam;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;

        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        currentSpeed = speed;

        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (eldekiGorselMizrak != null) eldekiGorselMizrak.SetActive(false);

        // ========================================================
        // --- YENİ EKLENDİ: ZEMİNE GİRME BUG'I İÇİN MATEMATİK ---
        // ========================================================
        // Oyun başında kapsülün merkezini ve ayak tabanının pozisyonunu hafızaya alıyoruz.
        baseCenter = controller.center;
        baseBottom = baseCenter.y - (controller.height / 2f);

        if (normalCamera != null)
        {
            normalCamera.Priority = 10;
            normalCamera.Follow = this.transform;
            normalCamera.LookAt = this.transform;
            normalCamera.PreviousStateIsValid = false;

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
        if (activePlatform != null)
        {
            Vector3 newGlobalPlatformPoint = activePlatform.TransformPoint(activeLocalPlatformPoint);
            Vector3 moveDiff = newGlobalPlatformPoint - activeGlobalPlatformPoint;

            if (moveDiff.magnitude > 0.0001f)
            {
                controller.Move(moveDiff);
            }

            Quaternion newGlobalPlatformRotation = activePlatform.rotation * activeLocalPlatformRotation;
            Quaternion rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(activeGlobalPlatformRotation);

            rotationDiff.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.001f)
            {
                transform.Rotate(axis, angle, Space.World);
            }

            activeGlobalPlatformPoint = transform.position;
            activeGlobalPlatformRotation = transform.rotation;
            activeLocalPlatformPoint = activePlatform.InverseTransformPoint(transform.position);
            activeLocalPlatformRotation = Quaternion.Inverse(activePlatform.rotation) * transform.rotation;
        }

        RaycastHit platformHit;
        if (Physics.Raycast(groundCheck.position, Vector3.down, out platformHit, 1.5f, groundMask))
        {
            Transform hitTransform = null;

            MovingColliders mc = platformHit.collider.GetComponent<MovingColliders>();
            if (mc == null) mc = platformHit.collider.GetComponentInParent<MovingColliders>();
            if (mc != null) hitTransform = platformHit.collider.transform;

            MovingIllusionPlatform mip = platformHit.collider.GetComponent<MovingIllusionPlatform>();
            if (mip == null) mip = platformHit.collider.GetComponentInParent<MovingIllusionPlatform>();
            if (mip != null) hitTransform = mip.movingBody;

            if (hitTransform != null)
            {
                if (activePlatform != hitTransform)
                {
                    activePlatform = hitTransform;
                    activeGlobalPlatformPoint = transform.position;
                    activeGlobalPlatformRotation = transform.rotation;
                    activeLocalPlatformPoint = activePlatform.InverseTransformPoint(transform.position);
                    activeLocalPlatformRotation = Quaternion.Inverse(activePlatform.rotation) * transform.rotation;
                }
            }
            else { activePlatform = null; }
        }
        else { activePlatform = null; }

        if (iFrames > 0)
        {
            iFrames -= Time.deltaTime;
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isControlled && Input.GetKeyDown(healKey))
        {
            UseHealthPotion();
        }

        if (isControlled && Input.GetKeyDown(slowTimeKey))
        {
            UseSlowPotion();
        }

        if (isLatched)
        {
            if (latchedLance != null)
            {
                Vector3 pushAwayDir = -latchedLance.forward;
                LanceObj lanceScript = latchedLance.GetComponent<LanceObj>();

                if (lanceScript != null)
                {
                    pushAwayDir = lanceScript.wallNormal;
                }

                transform.position = latchedLance.position + (Vector3.up * lanceHangOffset) + (pushAwayDir * lanceWallOffset) + (transform.forward * lanceForwardOffset);
            }
            else
            {
                DetachAndJump();
                return;
            }

            SetAimMode(false);
            if (isControlled && Input.GetButtonDown("Jump")) DetachAndJump();
            return;
        }

        if (isZiplining && isControlled && Input.GetButtonDown("Jump"))
        {
            isZiplining = false;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount = 1;
            if (animator != null) animator.SetTrigger("Jump");
        }

        if (isControlled && Input.GetKeyDown(KeyCode.C) && !isDrinking)
        {
            CheckForLanceLatch();
        }

        // ========================================================
        // --- GÜNCELLENDİ: DASH (OMUZ ATMA) ANİMASYON TETİKLEMESİ ---
        // ========================================================
        if (isControlled && Input.GetKeyDown(KeyCode.E) && !isDashing && !isDodging && isGrounded && dashCooldownTimer <= 0f && !isDrinking)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            // Animator'e "Dash" animasyonunu oynaması için sinyal yolluyoruz
            if (animator != null) animator.SetTrigger("Dash");
        }

        if (landStunTimer > 0)
        {
            landStunTimer -= Time.deltaTime;
        }

        if (crawlStartTimer > 0)
        {
            crawlStartTimer -= Time.deltaTime;
        }

        // ========================================================
        // --- YENİ EKLENDİ: DODGE VE SPRINT (BAS-ÇEK) KONTROLÜ ---
        // ========================================================
        if (isControlled && !isDrinking && !isZiplining && !isLatched && !isDashing && !isDodging)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                isShiftPressed = true;
                shiftPressTimer = 0f;
            }

            if (isShiftPressed)
            {
                shiftPressTimer += Time.deltaTime;

                if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
                {
                    isShiftPressed = false;

                    // Eğer kısa basıldıysa DODGE at!
                    if (shiftPressTimer <= shiftTapThreshold && isGrounded && landStunTimer <= 0f && !isCrouchToggled)
                    {
                        isDodging = true;
                        dodgeTimer = dodgeDuration;
                        if (animator != null) animator.SetTrigger("Dodge");
                    }
                }
            }
        }
        else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            isShiftPressed = false;
            shiftPressTimer = 0f;
        }
        // ========================================================

        // ========================================================
        // --- GÜNCELLENDİ: BAS/ÇEK KİLİDİ VE YUKARIDAN KESİLEN COLLIDER ---
        // ========================================================
        // GÜNCELLENDİ: Dodge atarken eğilme mantığı da iptal
        if (!isDashing && !isDodging && !isLatched && !isZiplining && !isDrinking)
        {
            if (isControlled && (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)))
            {
                isCrouchToggled = !isCrouchToggled;

                if (isCrouchToggled)
                {
                    // Delay ayarını değişkene bağladık
                    crawlStartTimer = crouchDelayDuration;
                    if (animator != null) animator.SetTrigger("CrawlStart");
                }
                else
                {
                    crawlStartTimer = 0f;
                }
            }

            if (isCrouchToggled)
            {
                if (crawlStartTimer > 0f)
                {
                    isCrouching = true;
                    isCrawling = false;
                }
                else
                {
                    isCrouching = false;
                    isCrawling = true;
                }
            }
            else
            {
                isCrouching = false;
                isCrawling = false;
            }

            if (isControlled && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                isWalking = true;
            }
            else
            {
                isWalking = false;
            }

            if (isCrawling) currentSpeed = crawlSpeed;
            else if (isCrouching) currentSpeed = crouchSpeed;
            else if (isWalking) currentSpeed = walkSpeed;
            else if (isControlled && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) currentSpeed = sprintSpeed;
            else currentSpeed = speed;

            if (isTimePotionActive)
            {
                currentSpeed = currentSpeed * 1.5f;
            }

            // Tost (Scale) olayını sildik, onun yerine direkt Kapsülün boyunu tıraşlıyoruz
            float targetHeight = isCrawling ? crawlHeight : (isCrouching ? crouchHeight : normalHeight);
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            // Kapsülün sadece "üstten" aşağı inmesi, tabanının zeminde sabit kalması için Orijinal Ayak Tabanı + (Boy / 2) formülü
            controller.center = new Vector3(baseCenter.x, baseBottom + (controller.height / 2f), baseCenter.z);
        }
        else
        {
            isCrouching = false;
            isCrawling = false;
            isWalking = false;
            isCrouchToggled = false; // Başka bir işleme geçerse toggle sıfırlansın
        }

        bool isAiming = isControlled && Input.GetMouseButton(1) && !isDrinking;
        float targetFOV = normalFOV;
        float targetOffsetX = 0f;
        float targetOffsetY = 0f;

        if (isLanceEquipped && !isDashing && !isDodging && !isZiplining && !isDrinking)
        {
            if (isAiming)
            {
                SetAimMode(true);

                if (isControlled && Input.GetMouseButtonDown(0) && !isThrowing)
                {
                    StartCoroutine(ThrowRoutine());
                }

                targetFOV = aimFOV;
                targetOffsetX = aimOffsetX;
                targetOffsetY = aimOffsetY;
            }
            else
            {
                SetAimMode(false);
            }
        }
        else if (isDashing || isDodging || isZiplining || isDrinking)
        {
            SetAimMode(false);
        }

        if (normalCamera != null)
        {
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

        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (!isGrounded && velocity.y < 0)
        {
            isNearGround = Physics.Raycast(groundCheck.position, Vector3.down, nearGroundDistance, groundMask);
        }
        else
        {
            isNearGround = isGrounded;
        }

        if (!wasGrounded && isGrounded && !isZiplining)
        {
            if (animator != null) animator.SetTrigger("Land");
            landStunTimer = landStunDuration;
        }

        if (isZiplining)
        {
            velocity.y = 0f;
            jumpCount = 0;
            isGrounded = false;
            isNearGround = false;
        }
        else
        {
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
                velocity.x = 0f;
                velocity.z = 0f;
                jumpCount = 0;
                if (animator != null) animator.ResetTrigger("Jump");
            }
            else if (!isGrounded && jumpCount == 0)
            {
                jumpCount = maxJumps;
            }
        }

        float horizontal = (isControlled && !isDrinking && crawlStartTimer <= 0f) ? Input.GetAxisRaw("Horizontal") : 0f;
        float vertical = (isControlled && !isDrinking && crawlStartTimer <= 0f) ? Input.GetAxisRaw("Vertical") : 0f;
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        float animSpeed = 0f;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
            else
            {
                if (controller.enabled) controller.Move(transform.forward * dashSpeed * Time.deltaTime);
            }
        }
        else if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0)
            {
                isDodging = false;
            }
            else
            {
                if (controller.enabled) controller.Move(transform.forward * dodgeSpeed * Time.deltaTime);
            }
        }
        else if (isZiplining || isDrinking)
        {
            animSpeed = 0f;
        }
        else
        {
            if (!isAiming)
            {
                if ((isControlled && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f) || inputDir.magnitude < 0.1f)
                {
                    referenceYaw = cam.eulerAngles.y;
                }

                if (inputDir.magnitude >= 0.1f)
                {
                    if (landStunTimer > 0)
                    {
                        animSpeed = 0f;
                    }
                    else
                    {
                        animSpeed = currentSpeed;
                        float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + referenceYaw;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);

                        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                        if (controller.enabled) controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
                    }
                }
            }
            else
            {
                float yawCamera = cam.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0, yawCamera, 0);

                if (inputDir.magnitude >= 0.1f && landStunTimer <= 0)
                {
                    animSpeed = currentSpeed * 0.6f;
                    Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
                    if (controller.enabled) controller.Move(moveDir * (currentSpeed * 0.6f) * Time.deltaTime);
                }
            }
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isNearGround", isNearGround);
            animator.SetFloat("VerticalVelocity", velocity.y);
            animator.SetBool("isZiplining", isZiplining);
            animator.SetBool("isAiming", isAiming);
            animator.SetBool("isLanceHanging", isLatched);
            animator.SetBool("isCrawling", isCrawling);

            animator.SetBool("isDodging", isDodging);

            // --- YENİ EKLENDİ: DASH SİNYALİ ---
            animator.SetBool("isDashing", isDashing); // Dodge gibi Dash'in bitişini de kontrol edebilmen için ekledim

            if (isAiming)
            {
                animator.SetFloat("AimSpeed", vertical, 0.1f, Time.deltaTime);
            }
        }

        if (isControlled && Input.GetButtonDown("Jump") && jumpCount < maxJumps && !isDashing && !isDodging && landStunTimer <= 0 && !isZiplining && !isDrinking && !isCrouchToggled)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;

            if (animator != null) animator.SetTrigger("Jump");
        }

        if (isControlled && Input.GetButtonUp("Jump") && velocity.y > 0f && !isZiplining)
        {
            velocity.y *= jumpCutMultiplier;
        }

        if (!isZiplining)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        if (controller.enabled) controller.Move(velocity * Time.deltaTime);
    }

    private System.Collections.IEnumerator ThrowRoutine()
    {
        isThrowing = true;

        if (animator != null) animator.SetTrigger("Throw");

        yield return new WaitForSeconds(throwDelay);

        if (eldekiGorselMizrak != null) eldekiGorselMizrak.SetActive(false);

        ThrowLance();

        yield return new WaitForSeconds(0.4f);

        isThrowing = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDashing && hit.gameObject.CompareTag("BreakableWall"))
        {
            if (wallBreakEffect != null) Instantiate(wallBreakEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(hit.gameObject);

            // Duvarı kırdıktan sonra dash mekaniği sıfırlanıyor (isteğe bağlı olarak devam da ettirilebilir)
            isDashing = false;
            dashTimer = 0f;
        }
    }

    void CheckForLanceLatch()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, latchRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Lance"))
            {
                LatchOntoLance(hitCollider.transform);
                break;
            }
        }
    }

    void SetAimMode(bool aiming)
    {
        if (aiming)
        {
            if (crosshairUI != null) crosshairUI.SetActive(true);
            Time.timeScale = slowMotionAmount;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            if (eldekiGorselMizrak != null && !isThrowing) eldekiGorselMizrak.SetActive(true);
        }
        else
        {
            if (crosshairUI != null) crosshairUI.SetActive(false);

            if (eldekiGorselMizrak != null) eldekiGorselMizrak.SetActive(false);

            if (!isTimePotionActive)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
            else
            {
                Time.timeScale = slowTimeAmount;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
        }
    }

    void ThrowLance()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetPoint = Physics.Raycast(ray, out hit, 300f, groundMask) ? hit.point : ray.GetPoint(300f);

        Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
        GameObject newLance = Instantiate(lancePrefab, spawnPos, Quaternion.identity);
        newLance.tag = "Lance";

        Vector3 flightDirection = (targetPoint - spawnPos).normalized;

        newLance.transform.rotation = Quaternion.LookRotation(flightDirection) * Quaternion.Euler(lanceRotationOffset);

        Rigidbody lanceRb = newLance.GetComponent<Rigidbody>();
        if (lanceRb != null) lanceRb.velocity = flightDirection * throwForce;
    }

    public void LatchOntoLance(Transform lance)
    {
        isLatched = true;
        latchedLance = lance;
        velocity = Vector3.zero;
        jumpCount = 0;
        controller.enabled = false;

        Vector3 pushAwayDir = -lance.forward;
        LanceObj lanceScript = lance.GetComponent<LanceObj>();

        if (lanceScript != null)
        {
            pushAwayDir = lanceScript.wallNormal;
        }

        Vector3 lookDirection = Vector3.ProjectOnPlane(transform.forward, pushAwayDir);
        lookDirection.y = 0f;

        if (lookDirection.magnitude < 0.05f)
        {
            lookDirection = Vector3.Cross(Vector3.up, pushAwayDir);
        }

        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
        }

        transform.position = lance.position + (Vector3.up * lanceHangOffset) + (pushAwayDir * lanceWallOffset) + (transform.forward * lanceForwardOffset);

        if (animator != null) animator.SetTrigger("LanceCatch");
    }

    void DetachAndJump()
    {
        isLatched = false;
        latchedLance = null;
        controller.enabled = true;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        Vector3 jumpDir = (inputDir.magnitude >= 0.1f) ?
            Quaternion.Euler(0f, Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y, 0f) * Vector3.forward :
            cam.forward;
        jumpDir.y = 0.5f;

        velocity = jumpDir.normalized * Mathf.Sqrt(jumpHeight * -2f * gravity) * lanceJumpMultiplier;
        jumpCount = 1;

        if (animator != null) animator.SetTrigger("Jump");
    }

    void OnEnable()
    {
        turnSmoothVelocity = 0f;
        if (Camera.main != null) referenceYaw = Camera.main.transform.eulerAngles.y;

        activePlatform = null;

        if (normalCamera != null)
        {
            normalCamera.Follow = this.transform;
            normalCamera.LookAt = this.transform;
            normalCamera.PreviousStateIsValid = false;
        }
    }

    void OnDisable()
    {

    }

    public void ExternalJump(float bounceHeight)
    {
        velocity.y = Mathf.Sqrt(bounceHeight * -2f * gravity);
        jumpCount = 1;
        if (animator != null) animator.SetTrigger("Jump");
    }

    public void TakeDamage(float damageAmount)
    {
        if (iFrames > 0) return;

        currentHealth -= damageAmount;
        iFrames = 1f;

        isDashing = false;
        isDodging = false;

        velocity.y = 5f;
        isGrounded = false;

        if (animator != null) animator.SetTrigger("Jump");

        Debug.Log("🩸 Don Quixote HASAR ALDI! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("💀 Don Öldü! Tüm fizik ve durumlar sıfırlanıyor...");

        isDrinking = false;
        isLatched = false;
        isThrowing = false;

        isDashing = false;
        isDodging = false;

        if (DualRealityManager.Instance != null)
        {
            DualRealityManager.Instance.ResetAllHealth();
        }

        PushableBox.ResetAllBoxes();

        velocity = Vector3.zero;

        Vector3 respawnPos = CheckpointManager.Instance.GetLastCheckpoint();

        controller.enabled = false;
        transform.position = respawnPos;
        controller.Move(Vector3.zero);
        controller.enabled = true;

        velocity = Vector3.zero;
    }

    public void UseHealthPotion()
    {
        if (!isGrounded || isDrinking || isZiplining || isDashing || isLatched) return;

        if (healthPotionCount > 0 && currentHealth < maxHealth)
        {
            StartCoroutine(DrinkPotionRoutine(true));
        }
        else if (currentHealth >= maxHealth)
        {
            Debug.Log("Canın zaten full kanka, israf etme!");
        }
        else
        {
            Debug.Log("Hiç can iksirin kalmamış!");
        }
    }

    public void UseSlowPotion()
    {
        if (!isGrounded || isDrinking || isZiplining || isDashing || isLatched) return;

        if (slowPotionCount > 0 && !isTimePotionActive)
        {
            StartCoroutine(DrinkPotionRoutine(false));
        }
        else if (isTimePotionActive)
        {
            Debug.Log("Zaman zaten yavaş kanka!");
        }
        else
        {
            Debug.Log("Hiç zaman iksirin kalmamış!");
        }
    }

    private System.Collections.IEnumerator DrinkPotionRoutine(bool isHealthPotion)
    {
        isDrinking = true;

        velocity.x = 0f;
        velocity.z = 0f;

        if (animator != null) animator.SetTrigger("DrinkPotion");

        yield return new WaitForSeconds(2f);

        if (isHealthPotion)
        {
            healthPotionCount--;
            currentHealth += healthPotionHealAmount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log("💚 İksir İçildi! Yeni Can: " + currentHealth + " | Kalan İksir: " + healthPotionCount);
        }
        else
        {
            slowPotionCount--;
            StartCoroutine(SlowTimeRoutine());
            Debug.Log("⏳ Zaman İksiri İçildi! Kalan İksir: " + slowPotionCount);
        }

        isDrinking = false;
    }

    private System.Collections.IEnumerator SlowTimeRoutine()
    {
        isTimePotionActive = true;
        Time.timeScale = slowTimeAmount;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(slowTimeDuration);

        isTimePotionActive = false;

        if (!Input.GetMouseButton(1))
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        Debug.Log("⏳ Zaman normale döndü!");
    }
}