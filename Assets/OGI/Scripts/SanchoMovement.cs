using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class SanchoMovement : MonoBehaviour
{
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
    private bool isGrounded;

    [Header("Kamera Sistemi")]
    public CinemachineFreeLook normalCamera;

    private CharacterController controller;
    private Transform cam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;

        // --- YENİ: OYUN BAŞINDA CANI FULLE ---
        currentHealth = maxHealth;

        // --- İLK AÇILIŞ: KAMERAYI ENSENE YAPIŞTIR ---
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
            // Platformun hareketini hesapla ve karaktere direkt yürüme olarak (Move) uygula
            Vector3 newGlobalPlatformPoint = activePlatform.TransformPoint(activeLocalPlatformPoint);
            Vector3 moveDiff = newGlobalPlatformPoint - activeGlobalPlatformPoint;

            if (moveDiff.magnitude > 0.0001f)
            {
                controller.Move(moveDiff);
            }

            // Platformun dönüşünü hesapla ve sadece karakterin kendi ekseninde çevir
            Quaternion newGlobalPlatformRotation = activePlatform.rotation * activeLocalPlatformRotation;
            Quaternion rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(activeGlobalPlatformRotation);

            rotationDiff.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.001f)
            {
                transform.Rotate(axis, angle, Space.World);
            }

            // Değerleri bir sonraki kare için hafızaya al
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

            // Önce Pervane kontrolü (MovingColliders)
            MovingColliders mc = platformHit.collider.GetComponent<MovingColliders>();
            if (mc == null) mc = platformHit.collider.GetComponentInParent<MovingColliders>();
            if (mc != null) hitTransform = platformHit.collider.transform;

            // Sonra Asansör kontrolü (MovingIllusionPlatform)
            MovingIllusionPlatform mip = platformHit.collider.GetComponent<MovingIllusionPlatform>();
            if (mip == null) mip = platformHit.collider.GetComponentInParent<MovingIllusionPlatform>();
            if (mip != null) hitTransform = mip.movingBody; // Asansörün movingBody'sine kilitlen!

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

        // --- 3. DİĞER TÜM MEKANİKLER (Zerre dokunulmadı) ---
        if (iFrames > 0)
        {
            iFrames -= Time.deltaTime;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            velocity.x = 0f;
            velocity.z = 0f;
            jumpCount = 0;
        }
        else if (!isGrounded && jumpCount == 0)
        {
            jumpCount = maxJumps;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f || inputDir.magnitude < 0.1f)
        {
            referenceYaw = cam.eulerAngles.y;
        }

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + referenceYaw;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            if (controller.enabled) controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
        }

        if (Input.GetButtonUp("Jump") && velocity.y > 0f)
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

        // --- BUG FİX: KARAKTER UYANDIĞINDA ESKİ PLATFORM HAFIZASINI SİL ---
        activePlatform = null;

        // --- SWİTCH İLE SANCHO'YA GEÇİNCE KAMERAYI ÇAL ---
        if (normalCamera != null)
        {
            normalCamera.Follow = this.transform;
            normalCamera.LookAt = this.transform;
            normalCamera.PreviousStateIsValid = false;
        }
    }

    void OnDisable()
    {
        // ORTAK KAMERAYI ASLA KAPATMIYORUZ!
    }

    public void ExternalJump(float bounceHeight)
    {
        velocity.y = Mathf.Sqrt(bounceHeight * -2f * gravity);
        jumpCount = 1;
    }

    // --- YENİ EKLENEN HASAR VE ÖLÜM FONKSİYONLARI ---
    public void TakeDamage(float damageAmount)
    {
        if (iFrames > 0) return; // 1 saniyelik ölümsüzlük devredeyse hasar alma!

        currentHealth -= damageAmount;
        iFrames = 1f; // Hasar yedi, 1 saniye dokunulmaz yap

        Debug.Log("🩸 Sancho HASAR ALDI! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Hem Don hem de Sancho Movement scriptlerinde Die() fonksiyonunu buna çevir:
    void Die()
    {
        Debug.Log("💀 Sancho Öldü! Canlar sıfırlanıyor...");

        // --- KRİTİK SATIR BURASI ---
        if (DualRealityManager.Instance != null)
        {
            DualRealityManager.Instance.ResetAllHealth();
        }

        // Checkpoint'e ışınlanma
        Vector3 respawnPos = CheckpointManager.Instance.GetLastCheckpoint();
        controller.enabled = false;
        transform.position = respawnPos;
        controller.enabled = true;
        velocity = Vector3.zero;
    }
}