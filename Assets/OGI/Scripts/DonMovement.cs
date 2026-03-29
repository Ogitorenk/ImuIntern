using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class DonMovement : MonoBehaviour
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
    public float lanceJumpMultiplier = 1f;
    public float latchRadius = 1.5f;
    public float lanceStickOffset = 1.1f;

    [HideInInspector] public bool isLatched = false;
    private Transform latchedLance;

    [Header("Niþan Alma (Tek Kamera Zoom)")]
    public GameObject crosshairUI;
    [Range(0.1f, 1f)] public float slowMotionAmount = 0.3f;
    public CinemachineFreeLook normalCamera;

    public float normalFOV = 40f;
    public float aimFOV = 20f;

    [Tooltip("Niþan alýrken karakteri saða almak için negatif (-1), sola almak için pozitif (1)")]
    public float aimOffsetX = -1f;

    // --- YENÝ EKLENEN YUKARI BAKMA AYARI ---
    [Tooltip("Niþan alýrken kamerayý ne kadar yukarý kaldýracaðýný belirler (Örn: 0.5 veya 1.2)")]
    public float aimOffsetY = 0.8f;

    public float zoomSpeed = 10f;
    private float currentOffsetX = 0f;
    private float currentOffsetY = 0f;

    // Senin orijinal kamera ayarlarýný ezmemek için hafýza
    private float[] baseOffsetX = new float[3];
    private float[] baseOffsetY = new float[3];

    [Header("Duvar Kýrma (Dash / Omuz Atma)")]
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

            // Oyun baþlarken senin kendi kamera offset'lerini hafýzaya alýyor
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
                targetOffsetY = aimOffsetY; // Yukarý çýkma miktarýný hedefle
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

        // --- YAÐ GÝBÝ ZOOM, SAÐA KAYMA VE YUKARI ÇIKMA ---
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
                    // Senin orijinal deðerinin üstüne bizim verdiðimiz deðeri ekler
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
}