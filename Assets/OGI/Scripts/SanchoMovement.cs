using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class SanchoMovement : MonoBehaviour
{
    // --- YENİ EKLENDİ: ÖZEL SAHNE KONTROL ŞALTERİ ---
    [Header("Özel Bölüm Kontrolü")]
    public bool isControlled = true; // Hep true kalacak, sadece özel sahnede SwitchManager bunu false yapacak.

    // --- YENİ EKLENEN SAĞLIK SİSTEMİ ---
    [Header("Sağlık Sistemi")]
    public float maxHealth = 100f;
    public float currentHealth;
    private float iFrames = 0f; // Hasar alınca 1 saniye ölümsüzlük

    // --- YENİ: ETKİLEŞİM DURUMU ---
    [Header("Etkileşim Durumu")]
    public bool isHoldingBox = false; // Kutu tutarken Switch atılmasını engellemek için eklendi

    // --- YENİ: PLATFORM FİZİĞİ DEĞİŞKENLERİ (GERÇEK TREN MANTIĞI) ---
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

    // --- YENİ EKLENDİ: KOŞMA, YÜRÜME VE EĞİLME ---
    [Header("Ekstra Hareket (Koşma/Yürüme/Eğilme)")]
    public float sprintSpeed = 10f; // Shift'e basınca çıkılacak hız
    public float walkSpeed = 2f;    // Alt tuşuna basınca düşülecek yürüme hızı
    public float crouchSpeed = 3f;  // Eğilirkenki hız
    public float normalScaleY = 1f; // Kapsülün normal Y boyutu (1-1-1 olduğu için 1)
    public float crouchScaleY = 0.5f; // Eğilirkenki Y boyutu (Yarıya inmesi için)
    public float crouchTransitionSpeed = 10f; // Eğilme-Kalkma hızı (Animasyonumsu geçiş)

    private float currentSpeed; // Anlık hızımız
    private bool isCrouching = false; // Eğiliyor muyuz?
    private bool isWalking = false;   // Yürüyor muyuz?

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

    // --- YENİ: ERKEN İNİŞ SİSTEMİ ---
    [Tooltip("Yere ne kadar mesafe kala iniş animasyonu başlasın?")]
    public float nearGroundDistance = 1.2f;
    private bool isNearGround;

    [HideInInspector] public bool isGrounded;

    [Header("Kamera Sistemi")]
    public CinemachineFreeLook normalCamera;

    private CharacterController controller;
    private Transform cam;

    // --- YENİ: ANİMASYON SİSTEMİ BAĞLANTISI ---
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;

        // Kapsülün içindeki 3D modelin Animator'ünü bul
        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        currentSpeed = speed;

        if (normalCamera != null)
        {
            normalCamera.Priority = 10;
            normalCamera.Follow = this.transform;
            normalCamera.LookAt = this.transform;
            normalCamera.PreviousStateIsValid = false; // Anında ışınlan
        }
    }

    void Update()
    {
        // --- 1. PLATFORM FİZİĞİ HESAPLAMA (TREN MANTIĞI) ---
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

        // --- 2. ZEMİN VE PLATFORM TESPİTİ (RAYCAST SİSTEMİ) ---
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

        // --- 3. DİĞER TÜM MEKANİKLER ---
        if (iFrames > 0)
        {
            iFrames -= Time.deltaTime;
        }

        // Eğilme (Sol Ctrl veya Sağ Ctrl) - GÜNCELLENDİ
        if (isControlled && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }

        // Yürüme (Sol Alt veya Sağ Alt) - GÜNCELLENDİ
        if (isControlled && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }

        // Hız Belirleme
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isWalking)
        {
            currentSpeed = walkSpeed;
        }
        else if (isControlled && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) // GÜNCELLENDİ
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = speed;
        }

        // PROTOTİP KAPSÜL İÇİN BOYUT DEĞİŞTİRME:
        float targetScaleY = isCrouching ? crouchScaleY : normalScaleY;
        Vector3 targetScale = new Vector3(transform.localScale.x, targetScaleY, transform.localScale.z);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * crouchTransitionSpeed);

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // --- YENİ: ERKEN İNİŞ İÇİN LAZER KONTROLÜ ---
        if (!isGrounded && velocity.y < 0)
        {
            isNearGround = Physics.Raycast(transform.position, Vector3.down, nearGroundDistance, groundMask);
        }
        else
        {
            isNearGround = isGrounded;
        }

        // --- YERE DEĞDİĞİNDE TETİĞİ SIFIRLA ---
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

        // GÜNCELLENDİ: Kontrol bizdeyse tuşları oku, değilse 0 yolla
        float horizontal = isControlled ? Input.GetAxisRaw("Horizontal") : 0f;
        float vertical = isControlled ? Input.GetAxisRaw("Vertical") : 0f;
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if ((isControlled && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f) || inputDir.magnitude < 0.1f) // GÜNCELLENDİ
        {
            referenceYaw = cam.eulerAngles.y;
        }

        // --- ANİMASYON HIZINI HESAPLAMA ---
        float animSpeed = 0f;

        if (inputDir.magnitude >= 0.1f)
        {
            animSpeed = currentSpeed;

            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + referenceYaw;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            if (controller.enabled) controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        // --- ANİMATOR'E SİNYALLERİ GÖNDERME ---
        if (animator != null)
        {
            animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isNearGround", isNearGround);
            animator.SetFloat("VerticalVelocity", velocity.y);
        }

        // Zıplama - GÜNCELLENDİ
        if (isControlled && Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;

            if (animator != null) animator.SetTrigger("Jump");
        }

        // GÜNCELLENDİ
        if (isControlled && Input.GetButtonUp("Jump") && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
        }

        velocity.y += gravity * Time.deltaTime;
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
}