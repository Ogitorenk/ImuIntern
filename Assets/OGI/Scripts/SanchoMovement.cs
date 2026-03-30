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

    // --- YENİ: PLATFORM FİZİĞİ DEĞİŞKENLERİ ---
    private GameObject currentPlatform;
    private Quaternion previousPlatformRotation;

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
        // --- YENİ: PLATFORM FİZİĞİ (DÖNEN ZEMİN TAKİBİ) ---
        RaycastHit platformHit;
        if (Physics.Raycast(groundCheck.position, Vector3.down, out platformHit, 1f, groundMask))
        {
            if (platformHit.collider.gameObject.GetComponent<MovingColliders>())
            {
                if (currentPlatform != platformHit.collider.gameObject)
                {
                    currentPlatform = platformHit.collider.gameObject;
                    previousPlatformRotation = currentPlatform.transform.rotation;
                }

                Quaternion platformRotationDifference = currentPlatform.transform.rotation * Quaternion.Inverse(previousPlatformRotation);
                platformRotationDifference.ToAngleAxis(out float angle, out Vector3 axis);

                if (axis.y > 0.9f || axis.y < -0.9f)
                {
                    transform.RotateAround(currentPlatform.transform.position, Vector3.up, angle);
                }

                previousPlatformRotation = currentPlatform.transform.rotation;
            }
            else { currentPlatform = null; }
        }
        else { currentPlatform = null; }

        // --- YENİ: ÖLÜMSÜZLÜK SÜRESİNİ DÜŞÜR ---
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
        // ORTAK KAMERAYI ASLA KAPATMIYORUZ! BURASI TAMAMEN BOŞ!
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

    void Die()
    {
        Debug.Log("💀 Sancho ÖLDÜ! 💀");
        // İleride buraya başa dönme veya Game Over ekranı eklenebilir.
    }
}