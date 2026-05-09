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
    public float normalScaleY = 1f;
    public float crouchScaleY = 0.5f;
    public float crouchTransitionSpeed = 10f;

    private float currentSpeed;
    private bool isCrouching = false;
    private bool isWalking = false;

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

        if (!isZiplining && !isHoldingBox) // Kutu tutarken eğilme iptal
        {
            if (isControlled && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                isCrouching = true;
            }
            else
            {
                isCrouching = false;
            }

            if (isControlled && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                isWalking = true;
            }
            else
            {
                isWalking = false;
            }
        }
        else
        {
            isCrouching = false;
            isWalking = false;
        }

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isWalking)
        {
            currentSpeed = walkSpeed;
        }
        else if (isControlled && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = speed;
        }

        if (DonMovement.isTimePotionActive)
        {
            currentSpeed = currentSpeed * 1.5f;
        }

        float targetScaleY = isCrouching ? crouchScaleY : normalScaleY;
        Vector3 targetScale = new Vector3(transform.localScale.x, targetScaleY, transform.localScale.z);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * crouchTransitionSpeed);

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

        float horizontal = isControlled ? Input.GetAxisRaw("Horizontal") : 0f;
        float vertical = isControlled ? Input.GetAxisRaw("Vertical") : 0f;
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if ((isControlled && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f) || inputDir.magnitude < 0.1f)
        {
            referenceYaw = cam.eulerAngles.y;
        }

        float animSpeed = 0f;

        if (isZiplining)
        {
            animSpeed = 0f;
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

                // GÜNCELLENDİ: Kutu tutarken sağa sola dönmesin, hep kutuya baksın diye rotasyonu kilitledik
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

            // --- YENİ EKLENDİ: KUTU İTME / ÇEKME SİNYALLERİ ---
            animator.SetBool("isHoldingBox", isHoldingBox);
            if (isHoldingBox)
            {
                // W'ye basarsa +1 (İtme), S'ye basarsa -1 (Çekme), basmazsa 0 (Idle) gider.
                animator.SetFloat("PushPull", vertical, 0.1f, Time.deltaTime);
            }
        }

        if (isControlled && Input.GetButtonDown("Jump") && jumpCount < maxJumps && landStunTimer <= 0 && !isZiplining && !isHoldingBox)
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

        Debug.Log("🩸 Sancho HASAR ALDI! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("💀 Sancho Öldü! Canlar sıfırlanıyor...");

        if (DualRealityManager.Instance != null)
        {
            DualRealityManager.Instance.ResetAllHealth();
        }

        Vector3 respawnPos = CheckpointManager.Instance.GetLastCheckpoint();
        controller.enabled = false;
        transform.position = respawnPos;
        controller.enabled = true;
        velocity = Vector3.zero;
    }

    public void UseHealthPotion()
    {
        if (healthPotionCount > 0 && currentHealth < maxHealth)
        {
            healthPotionCount--;
            currentHealth += healthPotionHealAmount;

            if (currentHealth > maxHealth) currentHealth = maxHealth;

            Debug.Log("💚 İksir İçildi! Yeni Can: " + currentHealth + " | Kalan İksir: " + healthPotionCount);
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
        if (slowPotionCount > 0 && !DonMovement.isTimePotionActive)
        {
            slowPotionCount--;
            StartCoroutine(SlowTimeRoutine());
            Debug.Log("⏳ Sancho Zaman İksiri İçti! Kalan İksir: " + slowPotionCount);
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
}