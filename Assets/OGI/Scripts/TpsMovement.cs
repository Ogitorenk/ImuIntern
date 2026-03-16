using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TpsMovement : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float speed = 5f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [Header("Kamera Hedefi (WASD Çakýþmasýný Çözer)")]
    public Transform lookAtTarget;
    public Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);

    [Header("Zýplama Ayarlarý")]
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

    private CharacterController controller;
    private Transform cam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. Zemin Kontrolü
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpCount = 0;
        }

        // 2. Hareket
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        // 3. Zýplama ve Çift Zýplama
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
        }

        // Yerçekimi
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 4. Kamera Hedefini Güncelleme
        if (lookAtTarget != null)
        {
            lookAtTarget.position = transform.position + lookAtOffset;
        }
    }
}