using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class TpsMovement : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    private float referenceYaw;

    [Header("Zýplama & Fizik")]
    public float jumpHeight = 2f;
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
    [HideInInspector] public bool isLatched = false;

    [Header("Niþan Alma (Hybrid Style)")]
    public GameObject crosshairUI;
    [Range(0.1f, 1f)] public float slowMotionAmount = 0.3f;

    [Tooltip("Normal Gezinme Kamerasý (FreeLook)")]
    public CinemachineFreeLook normalCamera;

    [Tooltip("Niþan Alma Kamerasý (Omuz/FPS)")]
    public CinemachineFreeLook aimCamera;

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
        // --- 1. TUTUNMA DURUMU ---
        if (isLatched)
        {
            SetAimMode(false);
            if (Input.GetButtonDown("Jump")) DetachAndJump();
            return;
        }

        // --- 2. KESÝN "C" TUÞU ÝLE TUTUNMA ---
        // Sadece C'ye basýldýðý KAREDE bu kontrolü yapar. Otomatik tutunma imkansýz hale gelir.
        if (Input.GetKeyDown(KeyCode.C))
        {
            CheckForLanceLatch();
        }

        // --- 3. HYBRID NÝÞAN ALMA (SAÐ TIK) ---
        bool isAiming = Input.GetMouseButton(1);

        if (isLanceEquipped)
        {
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

        // --- 4. HAREKET SÝSTEMÝ (DOKUNULMADI) ---
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            velocity.x = 0f;
            velocity.z = 0f;
            jumpCount = 0;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (!isAiming) // NORMAL HAREKET
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
        else // NÝÞAN ALMA HAREKETÝ (STRAFE)
        {
            float yawCamera = cam.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, yawCamera, 0);

            Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
            controller.Move(moveDir * (speed * 0.6f) * Time.deltaTime);
        }

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // TUTUNMA KONTROLÜNÜ AYRI BÝR METODA ALDIM KÝ SADECE C ÝLE ÇALIÞSIN
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
            if (crosshairUI != null) crosshairUI.SetActive(true);
            Time.timeScale = slowMotionAmount;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

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
        // ViewportPointToRay tam orta noktadan ýþýn fýrlatýr
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
        velocity = jumpDir.normalized * Mathf.Sqrt(jumpHeight * -2f * gravity) * 1.5f;
        jumpCount = 1;
    }
}