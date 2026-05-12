using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class SanchoMovement : MonoBehaviour
{
    [Header("Özel Bölüm Kontrolü")]
    public bool isControlled = true;

    [Header("Sağlık Sistemi")]
    public float maxHealth = 100f;
    public float currentHealth;
    private float iFrames = 0f;

    [Header("Envanter (Can İksiri)")]
    public int healthPotionCount = 0;
    public float healthPotionHealAmount = 20f;
    public KeyCode healKey = KeyCode.Alpha1; // 1 Tuşu

    [Header("Envanter (Zaman İksiri)")]
    public int slowPotionCount = 0;
    public float slowTimeAmount = 0.5f;
    public float slowTimeDuration = 5f;
    public KeyCode slowTimeKey = KeyCode.Alpha2; // 2 Tuşu

    [Header("Etkileşim Durumu")]
    public bool isHoldingBox = false;

    // --- İKSİR VE TAMİR KİLİTLERİ ---
    [HideInInspector] public bool isDrinking = false;
    [HideInInspector] public bool isRepairing = false; // TAMİR KİLİDİ

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

    [Header("Ekstra Hareket (Koşma/Yürüme/Eğilme/Sürünme)")]
    public float sprintSpeed = 10f;
    public float walkSpeed = 2f;
    public float crouchSpeed = 3f;

    // ========================================================
    // --- SÜRÜNME, KİLİT VE YÜKSEKLİK AYARLARI ---
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

    [Header("Zıplama & Fizik")]
    public float jumpHeight = 2f;
    [Range(0.1f, 0.9f)] public float jumpCutMultiplier = 0.5f;
    public float gravity = -19.62f;
    public int maxJumps = 2;
    private int jumpCount;
    private Vector3 velocity;

    public Vector3 CurrentVelocity { get { return velocity; } set { velocity = value; } }

    [Header("Zemin Kontrolü")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Tooltip("Yere ne kadar mesafe kala iniş animasyonu başlasın?")]
    public float nearGroundDistance = 1.2f;
    private bool isNearGround;
    private bool wasGrounded;

    [Header("İniş Ayarları (Land)")]
    public float landStunDuration = 0.15f;
    private float landStunTimer = 0f;

    [HideInInspector] public bool isGrounded;

    [Header("Kamera Sistemi")]
    public CinemachineFreeLook normalCamera;

    private CharacterController controller;
    private Transform cam;
    private Animator animator;
    [HideInInspector] public bool isZiplining = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;
        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        currentSpeed = speed;

        // ZEMİNE GİRME BUG'I İÇİN MATEMATİK
        controller.height = normalHeight;
        baseCenter = controller.center;
        baseBottom = baseCenter.y - (controller.height / 2f);

        if (normalCamera != null)
        {
            normalCamera.Priority = 10;
            normalCamera.Follow = this.transform;
            normalCamera.LookAt = this.transform;
            normalCamera.PreviousStateIsValid = false;
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
        if (Physics.Raycast(groundCheck.position, Vector3.down, out platformHit, 1.5f, groundMask, QueryTriggerInteraction.Ignore))
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

        if (isControlled && Input.GetKeyDown(healKey))
        {
            UseHealthPotion();
        }

        if (isControlled && Input.GetKeyDown(slowTimeKey))
        {
            UseSlowPotion();
        }

        if (isZiplining && isControlled && Input.GetButtonDown("Jump"))
        {
            isZiplining = false;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount = 1;
            if (animator != null) animator.SetTrigger("Jump");
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
        // --- YENİ EKLENDİ: SPRINT VE DODGE (BAS-ÇEK) KONTROLÜ ---
        // ========================================================
        // Tamir ederken veya elinde kutu varken Dodge atamasın!
        if (isControlled && !isDrinking && !isRepairing && !isZiplining && !isHoldingBox && !isDodging)
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
        // --- GÜNCELLENDİ: EĞİLME, SÜRÜNME, KİLİT VE COLLIDER ---
        // ========================================================
        // Dodge atarken sürünme tetiklenmesin
        if (!isZiplining && !isHoldingBox && !isDrinking && !isRepairing && !isDodging)
        {
            if (isControlled && (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)))
            {
                isCrouchToggled = !isCrouchToggled;

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

            // Tost (Scale) olayını sildik, onun yerine direkt Kapsülün boyunu tıraşlıyoruz
            float targetHeight = isCrawling ? crawlHeight : (isCrouching ? crouchHeight : normalHeight);
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            // Kapsülün sadece "üstten" aşağı inmesi, tabanının zeminde sabit kalması için formül
            controller.center = new Vector3(baseCenter.x, baseBottom + (controller.height / 2f), baseCenter.z);
        }
        else
        {
            isCrouching = false;
            isCrawling = false;
            isWalking = false;
            isCrouchToggled = false;
        }

        if (isCrawling) currentSpeed = crawlSpeed;
        else if (isCrouching) currentSpeed = crouchSpeed;
        else if (isWalking) currentSpeed = walkSpeed;
        // Shift'e basılı tutuluyorsa (veya basıldıysa) koştur. Kısa çekersen yukarıdaki Dodge sistemi çalışacak.
        else if (isControlled && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) currentSpeed = sprintSpeed;
        else currentSpeed = speed;

        if (DonMovement.isTimePotionActive)
        {
            currentSpeed = currentSpeed * 1.5f;
        }

        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (!isGrounded && velocity.y < 0)
        {
            isNearGround = Physics.Raycast(groundCheck.position, Vector3.down, nearGroundDistance, groundMask, QueryTriggerInteraction.Ignore);
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

        // İksir, Tamir VEYA Sürünme kilidi varken WASD iptal
        float horizontal = (isControlled && !isDrinking && !isRepairing && crawlStartTimer <= 0f) ? Input.GetAxisRaw("Horizontal") : 0f;
        float vertical = (isControlled && !isDrinking && !isRepairing && crawlStartTimer <= 0f) ? Input.GetAxisRaw("Vertical") : 0f;
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if ((isControlled && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f) || inputDir.magnitude < 0.1f)
        {
            referenceYaw = cam.eulerAngles.y;
        }

        float animSpeed = 0f;

        if (isZiplining || isDrinking || isRepairing)
        {
            animSpeed = 0f;
        }
        // ========================================================
        // --- YENİ EKLENDİ: DODGE (İLERİ ATILMA) FİZİĞİ ---
        // ========================================================
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
        else if (inputDir.magnitude >= 0.1f)
        {
            if (landStunTimer > 0)
            {
                animSpeed = 0f;
            }
            else
            {
                animSpeed = currentSpeed;

                float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + referenceYaw;

                if (!isHoldingBox)
                {
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                if (controller.enabled) controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
            }
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isNearGround", isNearGround);
            animator.SetFloat("VerticalVelocity", velocity.y);
            animator.SetBool("isZiplining", isZiplining);

            animator.SetBool("isCrawling", isCrawling);
            // Ekstra güvenlik için Animatör'e de gönderelim
            animator.SetBool("isDodging", isDodging);

            animator.SetBool("isHoldingBox", isHoldingBox);
            if (isHoldingBox)
            {
                animator.SetFloat("PushPull", vertical, 0.1f, Time.deltaTime);
            }

            animator.SetBool("isRepairing", isRepairing);
        }

        // GÜNCELLENDİ: Dodge atarken zıplayamayız
        if (isControlled && Input.GetButtonDown("Jump") && jumpCount < maxJumps && landStunTimer <= 0 && !isZiplining && !isHoldingBox && !isDrinking && !isRepairing && !isCrouchToggled && !isDodging)
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

        // Hasar yediğinde Dodge yarıda kesilsin ki saçma yerlere kaymasın
        isDodging = false;

        // Kütük veya tuzak vurduğunda karakteri havaya sıçratıp yerden kesiyoruz
        velocity.y = 5f;
        isGrounded = false;

        if (animator != null) animator.SetTrigger("Jump");

        Debug.Log("🩸 Sancho HASAR ALDI! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("💀 Sancho Öldü! Tüm fizik ve durumlar sıfırlanıyor...");

        isDrinking = false;
        isRepairing = false;
        isHoldingBox = false;
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
        // GÜNCELLENDİ: Dodge atarken iksir içemesin
        if (!isGrounded || isDrinking || isZiplining || isHoldingBox || isRepairing || isDodging) return;

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
        // GÜNCELLENDİ: Dodge atarken iksir içemesin
        if (!isGrounded || isDrinking || isZiplining || isHoldingBox || isRepairing || isDodging) return;

        if (slowPotionCount > 0 && !DonMovement.isTimePotionActive)
        {
            StartCoroutine(DrinkPotionRoutine(false));
        }
        else if (DonMovement.isTimePotionActive)
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
            Debug.Log("⏳ Sancho Zaman İksiri İçti! Kalan İksir: " + slowPotionCount);
        }

        isDrinking = false;
    }

    private System.Collections.IEnumerator SlowTimeRoutine()
    {
        DonMovement.isTimePotionActive = true;
        Time.timeScale = slowTimeAmount;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(slowTimeDuration);

        DonMovement.isTimePotionActive = false;

        if (!Input.GetMouseButton(1))
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        Debug.Log("⏳ Zaman normale döndü!");
    }

    public void StartRepairing()
    {
        // GÜNCELLENDİ: Dodge atarken tamir edemesin
        if (!isGrounded || isRepairing || isDrinking || isZiplining || isHoldingBox || isDodging) return;

        isRepairing = true;

        velocity.x = 0f;
        velocity.z = 0f;

        if (animator != null)
        {
            animator.SetTrigger("RepairStart");
            animator.SetBool("isRepairing", true);
        }

        Debug.Log("🔧 Sancho tamir etmeye başladı!");
    }

    public void StopRepairing()
    {
        if (!isRepairing) return;

        isRepairing = false;

        if (animator != null)
        {
            animator.SetBool("isRepairing", false);
        }

        Debug.Log("✅ Sancho tamiri bitirdi (veya bıraktı)!");
    }
}