using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class DonMovement : MonoBehaviour, IDamageable
{
    [Header("--- SCRIPTABLE OBJECT DATA ---")]
    [SerializeField] private CharacterData donData; // Don'un verilerini kalıcı tutan ScriptableObject kanka

    [Header("Özel Bölüm Kontrolü")]
    public bool isControlled = true;

    [Header("Sağlık Sistemi")]
    public float maxHealth = 100f;
    public float currentHealth;
    private float iFrames = 0f;
    [Tooltip("Ölürken karakterin ne kadar yukarı ışınlanacağını ayarlar.")]
    public float deathYOffset = 100f; // Sen test et diye direkt 100f verdim kanka!;

    // ========================================================
    // --- GÜNCELLENDİ: %50 YAVAŞLATMALI STAMINA SİSTEMİ ---
    // ========================================================
    [Header("Kondisyon (Stamina) Sistemi")]
    public float maxStamina = 100f;
    public float currentStamina;
    [Tooltip("Saniyede kaç stamina geri dolsun?")]
    public float staminaRegenRate = 25f; // Tam istediğin gibi 25 kanka
    [Tooltip("Saldırı yaptıktan sonra stamina yenilenmeye başlamadan önce kaç saniye beklensin?")]
    public float staminaRegenDelay = 0.5f; // Tam istediğin gibi 0.5s kanka
    private float staminaRegenTimer = 0f;

    [Header("Envanter (Can İksiri)")]
    public int healthPotionCount = 0;
    public float healthPotionHealAmount = 20f;
    public KeyCode healKey = KeyCode.Alpha1;

    [Header("Envanter (Zaman İksiri)")]
    public int slowPotionCount = 0;
    public float slowTimeAmount = 0.5f;
    public float slowTimeDuration = 5f;
    public KeyCode slowTimeKey = KeyCode.Alpha2;
    public static bool isTimePotionActive = false;

    [HideInInspector] public bool isDrinking = false;

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

    public float crawlSpeed = 1.5f;
    public float normalHeight = 2f;
    public float crouchHeight = 1f;
    public float crawlHeight = 0.6f;
    public float crouchTransitionSpeed = 10f;

    [Tooltip("Ctrl'ye basıp eğilirken kaç saniye hareket kilitlensin?")]
    public float crouchDelayDuration = 1f;

    [Tooltip("Eğildikten sonra kalkmak veya kalktıktan sonra eğilmek için Ctrl'nin bekleme süresi (Saniye)")]
    public float crouchCooldown = 2f;
    private float crouchCooldownTimer = 0f;

    private float currentSpeed;
    private bool isCrouching = false;
    private bool isWalking = false;

    [HideInInspector] public bool isCrawling = false;
    [HideInInspector] public bool isCrouchToggled = false;
    private float crawlStartTimer = 0f;

    private Vector3 baseCenter;
    private float baseBottom;

    [Header("Zıplama & Fizik")]
    public float jumpHeight = 2f;
    [Range(0.1f, 0.9f)] public float jumpCutMultiplier = 0.5f;
    public float gravity = -19.62f;
    public int maxJumps = 2;
    private int jumpCount;
    private Vector3 velocity;

    // --- YENİ EKLENDİ: MANAGERIN HIZI OKUYUP AKTARABİLMESİ İÇİN KAPSÜL ---
    public Vector3 CurrentVelocity { get { return velocity; } set { velocity = value; } }

    // --- YENİ EKLENDİ: PLATFORM MOMENTUM DEĞİŞKENLERİ ---
    [Header("Düşüş Momentum Ayarları")]
    [Tooltip("Düşerken yerçekimi normalin kaç katı etki etsin? (Daha sert ve gerçekçi düşüş)")]
    public float fallMultiplier = 2.5f;
    [Tooltip("Karakterin düşebileceği maksimum dikey hız sınırı")]
    public float terminalVelocity = -30f;

    [Header("Zemin Kontrolü")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    [HideInInspector] public bool isGrounded;
    private bool wasGrounded;

    [Header("İniş Ayarları (Land)")]
    [Tooltip("Karakter yere indiğinde kaç saniye boyunca hareket edemeyip animasyonun bitmesini beklesin?")]
    public float landStunDuration = 0.15f;
    private float landStunTimer = 0f;

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

    [Tooltip("Karakterin kendi Z Keyboard (ileri/geri) mızrağa göre konumu. Elleri hizalamak için kullan.")]
    public float lanceForwardOffset = 0f;

    [Tooltip("Zıplamadan hemen önce çarpışmayı yoksayarak mızrak fırlatılınca karakterin ne kadar ilerisine ışınlanacak?")]
    public float lanceGhostForwardOffset = 1.0f;

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

    [Header("Dodge (Kaçınma) Ayarları")]
    public float dodgeSpeed = 15f;
    public float dodgeDuration = 0.4f;
    [Tooltip("Shift'e ne kadar kısa basılırsa dodge sayılacak?")]
    public float shiftTapThreshold = 0.25f;

    [HideInInspector] public bool isDodging = false;
    private float dodgeTimer = 0f;
    private bool isShiftPressed = false;
    private float shiftPressTimer = 0f;

    private CharacterController controller;
    private Transform cam;
    private Transform camTransform; // Güvenlik amacıyla önlem
    private Animator animator;

    // --- YENİ: COMBAT BAĞLANTISI ---
    private DonCombat donCombat;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;

        animator = GetComponentInChildren<Animator>();
        donCombat = GetComponent<DonCombat>();

        currentHealth = maxHealth;
        currentStamina = maxStamina; // Stamina full başlasın kanka
        currentSpeed = speed;

        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (eldekiGorselMizrak != null) eldekiGorselMizrak.SetActive(false);

        baseCenter = controller.center;
        baseBottom = baseCenter.y - (controller.height / 2f);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateDonQuixoteHealth(currentHealth, maxHealth);
            HUDManager.Instance.UpdateDonQuixotePotions(healthPotionCount, slowPotionCount);
        }

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
        if (Time.timeScale == 0f) return;

        // === BUGFIX 1: KİLİT ŞALTERİ ===
        if (currentHealth <= 0) return;

        bool isAttacking = false;
        bool isBlocking = false;
        if (donCombat != null)
        {
            isAttacking = donCombat.isAttacking;
            isBlocking = donCombat.isBlocking;
        }

        // === STAMINA DOLMA MEKANİZMASI ===
        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }

        if (activePlatform != null)
        {
            Vector3 newGlobalPlatformPoint = activePlatform.TransformPoint(activeLocalPlatformPoint);
            Vector3 moveDiff = newGlobalPlatformPoint - activeGlobalPlatformPoint;

            if (moveDiff.magnitude > 0.0001f && currentHealth > 0)
            {
                controller.Move(moveDiff);
            }

            Quaternion newGlobalPlatformRotation = activePlatform.rotation * activeLocalPlatformRotation;
            Quaternion rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(activeGlobalPlatformRotation);

            rotationDiff.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.001f && currentHealth > 0)
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
            iFrames -= Time.unscaledDeltaTime;
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isControlled && Input.GetKeyDown(healKey) && currentHealth > 0)
        {
            UseHealthPotion();
        }

        if (isControlled && Input.GetKeyDown(slowTimeKey) && currentHealth > 0)
        {
            UseSlowPotion();
        }

        if (isLatched && currentHealth > 0)
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

        if (isZiplining && isControlled && Input.GetButtonDown("Jump") && currentHealth > 0)
        {
            isZiplining = false;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount = 1;

            // --- SES ENTEGRASYONU ---
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.jumpSound, transform.position);

            if (animator != null) animator.SetTrigger("Jump");
        }

        if (isControlled && Input.GetKeyDown(KeyCode.F) && !isDrinking && currentHealth > 0)
        {
            CheckForLanceLatch();
        }

        if (isControlled && Input.GetKeyDown(KeyCode.E) && !isDashing && !isDodging && isGrounded && dashCooldownTimer <= 0f && !isDrinking && !isAttacking && !isBlocking && currentHealth > 0)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            // --- SES ENTEGRASYONU ---
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.dodgeSound, transform.position);

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

        if (crouchCooldownTimer > 0)
        {
            crouchCooldownTimer -= Time.deltaTime;
        }

        if (isControlled && !isDrinking && !isZiplining && !isLatched && !isDashing && !isDodging && !isCrouchToggled && currentHealth > 0)
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

                    if (shiftPressTimer <= shiftTapThreshold && isGrounded && landStunTimer <= 0f)
                    {
                        isDodging = true;
                        dodgeTimer = dodgeDuration;

                        // --- SES ENTEGRASYONU ---
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.dodgeSound, transform.position);

                        if (animator != null) animator.SetTrigger("Dodge");

                        if (donCombat != null && donCombat.isAttacking)
                        {
                            donCombat.isAttacking = false;
                            if (animator != null) animator.SetBool("isAttacking", false);
                        }
                    }
                }
            }
        }
        else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            isShiftPressed = false;
            shiftPressTimer = 0f;
        }

        if (!isDashing && !isDodging && !isLatched && !isZiplining && !isDrinking && currentHealth > 0)
        {
            if (isControlled && (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)))
            {
                if (isGrounded && crouchCooldownTimer <= 0f && !isAttacking && !isBlocking)
                {
                    isCrouchToggled = !isCrouchToggled;
                    crouchCooldownTimer = crouchCooldown;

                    if (isCrouchToggled)
                    {
                        crawlStartTimer = crouchDelayDuration;
                        if (animator != null) animator.SetTrigger("CrawlStart");
                    }
                    else
                    {
                        crawlStartTimer = 0f;
                    }
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
            else if (isControlled && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !isCrouchToggled && isGrounded) currentSpeed = sprintSpeed;
            else currentSpeed = speed;

            if (isTimePotionActive)
            {
                currentSpeed = currentSpeed * 1.5f;
            }

            // === CRITICAL COMBAT HIZ AYARI (%50 YAVAŞLATMA SİHRİ) ===
            if (isAttacking)
            {
                currentSpeed = speed * 0.5f;
            }

            float targetHeight = isCrawling ? crawlHeight : (isCrouching ? crouchHeight : normalHeight);
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            controller.center = new Vector3(baseCenter.x, baseBottom + (controller.height / 2f), baseCenter.z);
        }
        else
        {
            isCrouching = false;
            isCrawling = false;
            isWalking = false;
        }

        bool isAiming = isControlled && Input.GetMouseButton(1) && !isDrinking && currentHealth > 0;
        float targetFOV = normalFOV;
        float targetOffsetX = 0f;
        float targetOffsetY = 0f;

        float horizontal = 0f;
        if (isControlled && !isDrinking && crawlStartTimer <= 0f && !isAttacking && !isBlocking && currentHealth > 0)
        {
            if (!isAiming)
            {
                horizontal = Input.GetAxisRaw("Horizontal");
            }
        }
        float vertical = (isControlled && !isDrinking && crawlStartTimer <= 0f && !isAttacking && !isBlocking && currentHealth > 0) ? Input.GetAxisRaw("Vertical") : 0f;
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (isLanceEquipped && !isDashing && !isDodging && !isZiplining && !isDrinking && currentHealth > 0)
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

        if (!wasGrounded && isGrounded && !isZiplining && iFrames <= 0 && currentHealth > 0)
        {
            // --- SES ENTEGRASYONU ---
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.landSound, transform.position);

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
            // ==============================================================================
            // === GÜNCELLENDİ: RAMPA VE MERDİVENLERDEN AŞAĞI İNERKEN YAPIŞMA SİHRİ ===
            // ==============================================================================
            if (isGrounded && velocity.y < 0)
            {
                // Karakter merdivenlerden aşağı inerken kaymasın/uçmasın diye dikey yapışma kuvvetini -8f yaptık kanka!
                velocity.y = -8f;
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

        float animSpeed = 0f;

        if (currentHealth > 0)
        {
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
                    if (controller.enabled)
                    {
                        float hInput = Input.GetAxisRaw("Horizontal");
                        float vInput = Input.GetAxisRaw("Vertical");
                        Vector3 dodgeDir = new Vector3(hInput, 0f, vInput).normalized;

                        if (dodgeDir.magnitude < 0.1f)
                        {
                            controller.Move(transform.forward * dodgeSpeed * Time.deltaTime);
                        }
                        else
                        {
                            Vector3 camForward = Camera.main.transform.forward;
                            Vector3 camRight = Camera.main.transform.right;
                            camForward.y = 0f;
                            camRight.y = 0f;
                            camForward.Normalize();
                            camRight.Normalize();

                            Vector3 finalDodgeDir = (camForward * vInput + camRight * hInput).normalized;

                            transform.rotation = Quaternion.LookRotation(finalDodgeDir);

                            controller.Move(finalDodgeDir * dodgeSpeed * Time.deltaTime);
                        }
                    }
                }
            }
            else if (isZiplining || isDrinking)
            {
                animSpeed = 0f;
            }
            else
            {
                if (!isAiming && !isBlocking)
                {
                    if (isAttacking)
                    {
                        float yawCamera = cam.eulerAngles.y;
                        transform.rotation = Quaternion.Euler(0, yawCamera, 0);
                        referenceYaw = yawCamera;
                    }
                    else if ((isControlled && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f) || inputDir.magnitude < 0.1f)
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
                    Vector3 camToPlayer = transform.position - cam.position;
                    camToPlayer.y = 0f;
                    camToPlayer.Normalize();

                    float trueYaw = Mathf.Atan2(camToPlayer.x, camToPlayer.z) * Mathf.Rad2Deg;

                    transform.rotation = Quaternion.Euler(0, trueYaw, 0);
                    referenceYaw = trueYaw;

                    if (inputDir.magnitude >= 0.1f && landStunTimer <= 0 && !isBlocking)
                    {
                        animSpeed = currentSpeed * 0.6f;

                        float targetAngle = Mathf.Atan2(horizontal, vertical) * Mathf.Rad2Deg + trueYaw;
                        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                        if (controller.enabled) controller.Move(moveDir.normalized * (currentSpeed * 0.6f) * Time.deltaTime);
                    }
                }
            }
        }

        if (animator != null)
        {
            float finalAnimSpeed = (currentHealth <= 0) ? 0f : animSpeed;

            animator.SetFloat("Speed", finalAnimSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isNearGround", isNearGround);
            animator.SetFloat("VerticalVelocity", velocity.y);
            animator.SetBool("isZiplining", isZiplining);
            animator.SetBool("isAiming", isAiming);
            animator.SetBool("isLanceHanging", isLatched);
            animator.SetBool("isCrawling", isCrawling);
            animator.SetBool("isDodging", isDodging);
            animator.SetBool("isDashing", isDashing);

            if (isAiming)
            {
                animator.SetFloat("AimSpeed", vertical, 0.1f, Time.deltaTime);
            }
        }

        if (isControlled && Input.GetButtonDown("Jump") && jumpCount < maxJumps && !isDashing && !isDodging && landStunTimer <= 0 && !isZiplining && !isDrinking && !isCrouchToggled && !isAttacking && !isBlocking && currentHealth > 0)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;

            // --- SES ENTEGRASYONU ---
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.jumpSound, transform.position);

            if (animator != null) animator.SetTrigger("Jump");
        }

        if (isControlled && Input.GetButtonUp("Jump") && velocity.y > 0f && !isZiplining && currentHealth > 0)
        {
            velocity.y *= jumpCutMultiplier;
        }

        if (!isZiplining && currentHealth > 0)
        {
            if (velocity.y < 0)
            {
                velocity.y += gravity * fallMultiplier * Time.deltaTime;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }

            if (velocity.y < terminalVelocity)
            {
                velocity.y = terminalVelocity;
            }

            if (controller.enabled) controller.Move(velocity * Time.deltaTime);
        }
    }

    // ==============================================================================
    // === ANIMASYONDAN GELECEK ADIM SESİ RADARI (3 ZEMİN AYIRTMALI) ===
    // ==============================================================================
    public void PlayFootstepSound()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        bool isMovingInput = (Mathf.Abs(inputX) > 0.05f || Mathf.Abs(inputZ) > 0.05f);

        if (!isGrounded || !isMovingInput) return;

        RaycastHit hit;
        if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, 1f, groundMask, QueryTriggerInteraction.Ignore))
        {
            string hitTag = hit.collider.gameObject.tag;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFootstep(hitTag, transform.position);
            }
        }
    }

    public void UseStamina(float amount)
    {
        currentStamina -= amount;
        if (currentStamina < 0f) currentStamina = 0f;
        staminaRegenTimer = staminaRegenDelay;
    }

    private System.Collections.IEnumerator ThrowRoutine()
    {
        isThrowing = true;

        if (animator != null) animator.SetTrigger("Throw");

        yield return new WaitForSeconds(throwDelay);

        if (eldekiGorselMizrak != null) eldekiGorselMizrak.SetActive(false);

        ThrowLance();

        yield return new WaitForSeconds(0.4f);

        if (!Input.GetMouseButton(1) && eldekiGorselMizrak != null) eldekiGorselMizrak.SetActive(false);

        isThrowing = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDashing && hit.gameObject.CompareTag("BreakableWall"))
        {
            if (wallBreakEffect != null) Instantiate(wallBreakEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(hit.gameObject);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.leverSound, hit.point);

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
        bool isMelee = false;
        if (donCombat != null)
        {
            isMelee = donCombat.isAttacking;
        }

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

            if (eldekiGorselMizrak != null && !isMelee) eldekiGorselMizrak.SetActive(false);

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
        Vector3 targetPoint;

        int layerMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        if (Physics.Raycast(ray, out hit, 300f, layerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(300f);
        }

        Vector3 spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
        GameObject newLance = Instantiate(lancePrefab, spawnPos, Quaternion.identity);
        newLance.tag = "Lance";

        Vector3 flightDirection = (targetPoint - spawnPos).normalized;

        newLance.transform.rotation = Quaternion.LookRotation(flightDirection) * Quaternion.Euler(lanceRotationOffset);

        Rigidbody lanceRb = newLance.GetComponent<Rigidbody>();
        if (lanceRb != null)
        {
            lanceRb.velocity = flightDirection * throwForce;
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.lanceThrowSound, spawnPos);
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

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.landSound, transform.position);

        if (animator != null) animator.SetTrigger("LanceCatch");
    }

    void DetachAndJump()
    {
        Vector3 forwardDir = transform.forward;

        isLatched = false;
        latchedLance = null;
        transform.position += forwardDir * lanceGhostForwardOffset;
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

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.jumpSound, transform.position);

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

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0 || iFrames > 0 || isDodging) return;

        if (donCombat != null && donCombat.shieldScript != null)
        {
            damageAmount = donCombat.shieldScript.BlockDamage(damageAmount);
        }

        if (damageAmount <= 0)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.shieldBlockSound, transform.position);
            Debug.Log("🛡️ Don kalkanıyla hasarı tamamen süzdü, canı gitmedi!");
            return;
        }

        currentHealth -= damageAmount;
        iFrames = 1f;

        isDashing = false;
        isDodging = false;

        velocity.y = 5f;
        isGrounded = false;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.donDamageSound, transform.position);

        if (animator != null && currentHealth > 0) animator.SetTrigger("Damage");

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateDonQuixoteHealth(currentHealth, maxHealth);
        }

        Debug.Log("🩸 Don Quixote HASAR ALDI! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"💀 {gameObject.name} ÖLDÜ! Tüm hareket donduruluyor ve ölüm zorla oynatılıyor...");

        isControlled = false;
        velocity = Vector3.zero;

        if (animator != null)
        {
            animator.ResetTrigger("Attack1");
            animator.ResetTrigger("Attack2");
            animator.ResetTrigger("Damage");
            animator.ResetTrigger("Throw");
            animator.ResetTrigger("Jump");
            animator.ResetTrigger("Land");

            animator.SetTrigger("Death");
        }

        StartCoroutine(DonRespawnRoutine());
    }

    private IEnumerator DonRespawnRoutine()
    {
        CombatAreaTrigger.ResetAllCombatArenas();

        Debug.Log("💀 Don Öldü! Ölüm animasyonu için 2 saniyelik sinematik bekleme başladı...");

        yield return new WaitForSeconds(2f);

        Debug.Log("🔄 Zaman doldu, Don için checkpoint sıfırlamaları ve hileli Y ışınlaması yapılıyor...");

        controller.enabled = false;
        transform.position = new Vector3(transform.position.x, transform.position.y + deathYOffset, transform.position.z);
        controller.enabled = true;

        isDrinking = false;
        isLatched = false;
        isThrowing = false;
        isDashing = false;
        isDodging = false;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnResetStats();
            currentHealth = donData != null ? donData.currentHealth : maxHealth;
        }
        else
        {
            currentHealth = maxHealth;
        }

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

        if (animator != null)
        {
            animator.Play("Locomotion", 0, 0f);
            animator.SetBool("isWalking", false);
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateDonQuixoteHealth(currentHealth, maxHealth);
            HUDManager.Instance.UpdateDonQuixotePotions(healthPotionCount, slowPotionCount);
        }

        isControlled = true;
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

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.drinkPotionSound, transform.position);

        yield return new WaitForSeconds(2f);

        if (isHealthPotion)
        {
            healthPotionCount--;
            currentHealth += healthPotionHealAmount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateDonQuixoteHealth(currentHealth, maxHealth);
                HUDManager.Instance.UpdateDonQuixotePotions(healthPotionCount, slowPotionCount);
            }

            Debug.Log("💚 İksir İçildi! Yeni Can: " + currentHealth + " | Kalan İksir: " + healthPotionCount);
        }
        else
        {
            slowPotionCount--;

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateDonQuixotePotions(healthPotionCount, slowPotionCount);
            }

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

    public void ExternalJump(float bounceHeight)
    {
        velocity.y = Mathf.Sqrt(bounceHeight * -2f * gravity);
        jumpCount = 1;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.jumpSound, transform.position);

        if (animator != null) animator.SetTrigger("Jump");
    }

    public void ResetCharacterStates()
    {
        if (isGrounded)
        {
            velocity = Vector3.zero;
        }
        currentSpeed = speed;

        isDrinking = false;
        isLatched = false;
        isThrowing = false;
        isDashing = false;
        isDodging = false;
        isCrawling = false;
        isCrouchToggled = false;
        isZiplining = false;

        if (donCombat != null)
        {
            donCombat.isAttacking = false;
            donCombat.isBlocking = false;
        }

        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
            animator.SetBool("isBlocking", false);
            animator.SetFloat("Speed", 0f);

            animator.ResetTrigger("Attack1");
            animator.ResetTrigger("Attack2");
            animator.ResetTrigger("Damage");
            animator.ResetTrigger("Throw");
        }
    }
}