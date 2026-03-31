using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class DonMovement : MonoBehaviour
{
    // --- YENİ EKLENEN SAĞLIK SİSTEMİ ---
    [Header("Sağlık Sistemi")]
    public float maxHealth = 100f;
    public float currentHealth;
    private float iFrames = 0f; // Hasar alınca 1 saniye ölümsüzlük

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

    [Header("Zemin Kontrolü")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;

    [Header("Mızrak Ayarları")]
    public bool isLanceEquipped = true;
    public GameObject lancePrefab;
    public float throwForce = 100f;
    public float lanceJumpMultiplier = 1f;
    public float latchRadius = 1.5f;
    public float lanceStickOffset = 1.1f;

    [HideInInspector] public bool isLatched = false;
    private Transform latchedLance;

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

    // Senin orijinal kamera ayarlarını ezmemek için hafıza
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

    private CharacterController controller;
    private Transform cam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;

        // --- YENİ: OYUN BAŞINDA CANI FULLE ---
        currentHealth = maxHealth;

        if (crosshairUI != null) crosshairUI.SetActive(false);

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
        // --- PLATFORM FİZİĞİ (GERÇEK TREN MANTIĞI KESİN ÇÖZÜM) ---
        if (activePlatform != null)
        {
            // 1. Platformun hareketini hesapla ve karaktere direkt yürüme olarak (Move) uygula
            Vector3 newGlobalPlatformPoint = activePlatform.TransformPoint(activeLocalPlatformPoint);
            Vector3 moveDiff = newGlobalPlatformPoint - activeGlobalPlatformPoint;

            if (moveDiff.magnitude > 0.0001f)
            {
                controller.Move(moveDiff);
            }

            // 2. Platformun dönüşünü hesapla ve sadece karakterin kendi ekseninde çevir
            Quaternion newGlobalPlatformRotation = activePlatform.rotation * activeLocalPlatformRotation;
            Quaternion rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(activeGlobalPlatformRotation);

            rotationDiff.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.001f)
            {
                transform.Rotate(axis, angle, Space.World);
            }

            // 3. Değerleri bir sonraki kare için hafızaya al
            activeGlobalPlatformPoint = transform.position;
            activeGlobalPlatformRotation = transform.rotation;
            activeLocalPlatformPoint = activePlatform.InverseTransformPoint(transform.position);
            activeLocalPlatformRotation = Quaternion.Inverse(activePlatform.rotation) * transform.rotation;
        }

        // --- ZEMİN VE PLATFORM TESPİTİ ---
        RaycastHit platformHit;
        // Zemin algılama mesafesini biraz uzun tuttuk ki pervane esnasında zıplamadığın sürece tutunsun (1.5f)
        if (Physics.Raycast(groundCheck.position, Vector3.down, out platformHit, 1.5f, groundMask))
        {
            // Pervane kolunda veya merkezinde MovingColliders scriptini ara
            MovingColliders mc = platformHit.collider.GetComponent<MovingColliders>();
            if (mc == null) mc = platformHit.collider.GetComponentInParent<MovingColliders>();

            if (mc != null)
            {
                Transform hitTransform = platformHit.collider.transform;
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

        // --- YENİ: ÖLÜMSÜZLÜK SÜRESİNİ DÜŞÜR ---
        if (iFrames > 0)
        {
            iFrames -= Time.deltaTime;
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isLatched)
        {
            if (latchedLance != null)
            {
                transform.position = latchedLance.position + (Vector3.up * lanceStickOffset);
            }
            else
            {
                DetachAndJump();
                return;
            }

            SetAimMode(false);
            if (Input.GetButtonDown("Jump")) DetachAndJump();
            return;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            CheckForLanceLatch();
        }

        if (Input.GetKeyDown(KeyCode.E) && !isDashing && isGrounded && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

        bool isAiming = Input.GetMouseButton(1);

        float targetFOV = normalFOV;
        float targetOffsetX = 0f;
        float targetOffsetY = 0f;

        if (isLanceEquipped && !isDashing)
        {
            if (isAiming)
            {
                SetAimMode(true);
                if (Input.GetMouseButtonDown(0)) ThrowLance();

                targetFOV = aimFOV;
                targetOffsetX = aimOffsetX;
                targetOffsetY = aimOffsetY;
            }
            else
            {
                SetAimMode(false);
            }
        }
        else if (isDashing)
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
        else
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

            if (!isAiming)
            {
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
            }
            else
            {
                float yawCamera = cam.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0, yawCamera, 0);

                Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
                if (controller.enabled) controller.Move(moveDir * (speed * 0.6f) * Time.deltaTime);
            }
        }

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps && !isDashing)
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

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDashing && hit.gameObject.CompareTag("BreakableWall"))
        {
            if (wallBreakEffect != null) Instantiate(wallBreakEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(hit.gameObject);
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
        }
        else
        {
            if (crosshairUI != null) crosshairUI.SetActive(false);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
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
        newLance.transform.rotation = Quaternion.LookRotation(flightDirection) * Quaternion.Euler(90f, 0f, 0f);

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
        transform.position = lance.position + (Vector3.up * lanceStickOffset);
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
    }

    void OnEnable()
    {
        turnSmoothVelocity = 0f;
        if (Camera.main != null) referenceYaw = Camera.main.transform.eulerAngles.y;

        // --- BUG FİX: KARAKTER UYANDIĞINDA ESKİ PLATFORM HAFIZASINI SİL ---
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
    }

    // --- YENİ EKLENEN HASAR VE ÖLÜM FONKSİYONLARI ---
    public void TakeDamage(float damageAmount)
    {
        if (iFrames > 0) return; // 1 saniyelik ölümsüzlük devredeyse hasar alma!

        currentHealth -= damageAmount;
        iFrames = 1f; // Hasar yedi, 1 saniye dokunulmaz yap

        Debug.Log("🩸 Don Quixote HASAR ALDI! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("💀 Don Quixote ÖLDÜ! 💀");
        // İleride buraya başa dönme veya Game Over ekranı eklenebilir.
    }

    // --- PLATFORM TUTUNMA SİSTEMİ (ARTIK RotateAround Update İÇİNDE ÇALIŞIYOR) ---
    private void OnTriggerEnter(Collider other)
    {
        // SetParent logic'i karakterin scale'ini bozduğu için sildik.
        // Update içindeki Raycast pervaneyi mc.transform.position üzerinden döndürüyor.
    }

    private void OnTriggerExit(Collider other)
    {
        // Boş bırakıldı.
    }
}