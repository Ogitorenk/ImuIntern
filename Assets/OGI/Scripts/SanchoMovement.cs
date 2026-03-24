using UnityEngine;
using Cinemachine; // <--- KAMERA ÝÇÝN EKLENDÝ

[RequireComponent(typeof(CharacterController))]
public class SanchoMovement : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [Header("Zýplama & Fizik")]
    public float jumpHeight = 2f;
    [Range(0.1f, 0.9f)] public float jumpCutMultiplier = 0.5f;
    public float gravity = -19.62f;
    public int maxJumps = 2;
    private int jumpCount;
    private Vector3 velocity;

    // --- Tab'a basýnca ivme kopyalamak için ---
    public Vector3 CurrentVelocity { get { return velocity; } set { velocity = value; } }

    [Header("Zemin Kontrolü")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;

    [Header("Kamera Sistemi")]
    [Tooltip("Sancho'nun takip kamerasý")]
    public CinemachineFreeLook normalCamera; // <--- KAMERA DEÐÝÞKENÝ EKLENDÝ

    private CharacterController controller;
    private Transform cam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;

        // Sancho aktif olduðunda kamerasýnýn önceliðini yükselt
        if (normalCamera != null) normalCamera.Priority = 10;
    }

    void Update()
    {
        // --- 1. ZEMÝN KONTROLÜ (Havada doðma fix'li) ---
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

        // --- 2. HAREKET (KAMERAYA GÖRE) ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // Sancho kameranýn baktýðý açýya göre döner ve ilerler
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        // --- 3. DÝNAMÝK ZIPLAMA ---
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
        }

        if (Input.GetButtonUp("Jump") && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
        }

        // --- 4. YERÇEKÝMÝ VE ÝVME ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void OnEnable()
    {
        if (normalCamera != null)
        {
            normalCamera.gameObject.SetActive(true);
            // SÝHÝRLÝ SATIR: Sancho'ya geçerken kamerayý anýnda enseye yapýþtýr.
            normalCamera.PreviousStateIsValid = false;
        }
    }

    void OnDisable()
    {
        if (normalCamera != null) normalCamera.gameObject.SetActive(false);
    }

}

