using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class DonMovement : MonoBehaviour // <--- ÝSÝM GÜNCELLENDÝ (TpsMovement -> DonMovement)
{
    [Header("Hareket Ayarlarý")]
    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    private float referenceYaw;

    [Header("Zýplama & Fizik")]
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

    [Header("Mýzrak Ayarlarý")]
    public bool isLanceEquipped = true;
    public GameObject lancePrefab;
    public float throwForce = 100f;
    [Tooltip("Mýzraktan atlarken ne kadar uzaða fýrlasýn? (1 normal, 1.5 çok uzak)")]
    public float lanceJumpMultiplier = 1f;
    [HideInInspector] public bool isLatched = false;

    [Header("Niþan Alma (Hybrid Style)")]
    public GameObject crosshairUI;
    [Range(0.1f, 1f)] public float slowMotionAmount = 0.3f;
    public CinemachineFreeLook normalCamera;
    public CinemachineFreeLook aimCamera;

    [Header("Duvar Kýrma (Dash / Omuz Atma)")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.3f;
    [Tooltip("Yeteneðin tekrar kullanýlabilmesi için geçmesi gereken süre (Saniye)")]
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

        if (crosshairUI != null) crosshairUI.SetActive(false);

        if (normalCamera != null) normalCamera.Priority = 10;
        if (aimCamera != null) aimCamera.Priority = 5;
    }

    void Update()
    {
        // --- YENÝ: COOLDOWN SAYACI ---
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // --- 1. TUTUNMA DURUMU ---
        if (isLatched)
        {
            SetAimMode(false);
            if (Input.GetButtonDown("Jump")) DetachAndJump();
            return;
        }

        // --- 2. KESÝN "C" TUÞU ÝLE TUTUNMA ---
        if (Input.GetKeyDown(KeyCode.C))
        {
            CheckForLanceLatch();
        }

        // --- 3. DUVAR KIRMA (HÜCUM) BAÞLATMA ---
        if (Input.GetKeyDown(KeyCode.E) && !isDashing && isGrounded && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

        // --- 4. HYBRID NÝÞAN ALMA (SAÐ TIK) ---
        bool isAiming = Input.GetMouseButton(1);

        if (isLanceEquipped && !isDashing)
        {
            // --- SÝHÝRLÝ EÞÝTLEME (DOÐRU YER) ---
            // Sadece sað týka ÝLK basýldýðý o milisaniye kopyala
            if (Input.GetMouseButtonDown(1) && normalCamera != null && aimCamera != null)
            {
                aimCamera.m_XAxis.Value = normalCamera.m_XAxis.Value;
                aimCamera.m_YAxis.Value = normalCamera.m_YAxis.Value;
            }
            // Sadece sað týk BIRAKILDIÐI o milisaniye kopyala
            else if (Input.GetMouseButtonUp(1) && normalCamera != null && aimCamera != null)
            {
                normalCamera.m_XAxis.Value = aimCamera.m_XAxis.Value;
                normalCamera.m_YAxis.Value = aimCamera.m_YAxis.Value;
            }

            if (isAiming)
            {
                SetAimMode(true);
                if (Input.GetMouseButtonDown(0)) ThrowLance();
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

        // --- 5. HAREKET VE ZEMÝN KONTROLÜ ---
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

        // --- 6. HÜCUM VEYA NORMAL HAREKET UYGULAMASI ---
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
            else
            {
                controller.Move(transform.forward * dashSpeed * Time.deltaTime);
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
                    controller.Move(moveDir.normalized * speed * Time.deltaTime);
                }
            }
            else
            {
                float yawCamera = cam.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0, yawCamera, 0);

                Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
                controller.Move(moveDir * (speed * 0.6f) * Time.deltaTime);
            }
        }

        // --- 7. DÝNAMÝK ZIPLAMA SÝSTEMÝ ---
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
        controller.Move(velocity * Time.deltaTime);
    }

    // --- 8. ÇARPIÞMA (DUVAR KIRMA) KONTROLÜ ---
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDashing && hit.gameObject.CompareTag("BreakableWall"))
        {
            if (wallBreakEffect != null)
            {
                Instantiate(wallBreakEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }

            Destroy(hit.gameObject);

            // --- BUG FIX: Duvar kýrýldýðý an hücumu iptal et ---
            isDashing = false;
            dashTimer = 0f;
        }
    }

    void CheckForLanceLatch()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 4f);
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
            // Crosshair'i anýnda aç
            if (crosshairUI != null) crosshairUI.SetActive(true);

            // Zamaný yavaþlat (Hissiyatý artýrmak için 0.2f veya 0.3f idealdir)
            Time.timeScale = slowMotionAmount;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // Kamerayý anýnda deðiþtir
            if (normalCamera != null) normalCamera.Priority = 5;
            if (aimCamera != null) aimCamera.Priority = 15;
        }
        else
        {
            if (crosshairUI != null) crosshairUI.SetActive(false);
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            if (normalCamera != null) normalCamera.Priority = 15;
            if (aimCamera != null) aimCamera.Priority = 5;
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
        velocity = Vector3.zero;
        jumpCount = 0;
        controller.enabled = false;
        transform.position = lance.position;
        controller.enabled = true;
    }

    void DetachAndJump()
    {
        isLatched = false;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        Vector3 jumpDir = (inputDir.magnitude >= 0.1f) ?
            Quaternion.Euler(0f, Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y, 0f) * Vector3.forward :
            cam.forward;
        jumpDir.y = 0.5f;

        // --- BUG FIX: Sabit 1.5f yerine lanceJumpMultiplier eklendi ---
        velocity = jumpDir.normalized * Mathf.Sqrt(jumpHeight * -2f * gravity) * lanceJumpMultiplier;
        jumpCount = 1;
    }

    void OnEnable()
    {
        // --- YENÝ EKLENEN BUG FIX: Karakter geçiþinde yön sapýtmasýný önler ---
        turnSmoothVelocity = 0f;
        if (Camera.main != null)
        {
            referenceYaw = Camera.main.transform.eulerAngles.y;
        }
        // ----------------------------------------------------------------------

        if (normalCamera != null)
        {
            normalCamera.gameObject.SetActive(true);
            normalCamera.PreviousStateIsValid = false;
        }
        if (aimCamera != null)
        {
            aimCamera.gameObject.SetActive(true);
            aimCamera.PreviousStateIsValid = false;
        }
    }

    void OnDisable()
    {
        if (normalCamera != null) normalCamera.gameObject.SetActive(false);
        if (aimCamera != null) aimCamera.gameObject.SetActive(false);
    }

    // --- YENÝ EKLENDÝ: Zýplama Tahtasý / Mantar için dýþarýdan fýrlatma ---
    public void ExternalJump(float bounceHeight)
    {
        velocity.y = Mathf.Sqrt(bounceHeight * -2f * gravity);
        jumpCount = 1;
    }
}