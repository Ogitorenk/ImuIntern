using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class DonMovement : MonoBehaviour
{
    // --- YENİ EKLENDİ: ÖZEL SAHNE KONTROL ŞALTERİ ---
    [Header("Özel Bölüm Kontrolü")]
    public bool isControlled = true;

    // --- YENİ EKLENEN SAĞLIK SİSTEMİ ---
    [Header("Sağlık Sistemi")]
    public float maxHealth = 100f;
    public float currentHealth;
    private float iFrames = 0f;

    [Header("Envanter (Can İksiri)")]
    public int healthPotionCount = 0;
    public float healthPotionHealAmount = 20f;
    public KeyCode healKey = KeyCode.Alpha1; // 1 Tuşu     

    // --- YENİ EKLENDİ: ZAMAN İKSİRİ ---
    [Header("Envanter (Zaman İksiri)")]
    public int slowPotionCount = 0;
    public float slowTimeAmount = 0.5f; // Zamanın hızı (%50 yavaşlar)
    public float slowTimeDuration = 5f; // Gerçek hayatta 5 saniye sürer
    public KeyCode slowTimeKey = KeyCode.Alpha2; // 2 Tuşu
    public static bool isTimePotionActive = false; // Çakışmaları önleyen global şalter

    // --- YENİ EKLENDİ: İKSİR İÇME KİLİDİ ---
    [HideInInspector] public bool isDrinking = false;

    // --- YENİ: PLATFORM FİZİĞİ DEĞİŞKENLERİ ---
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
    public float normalHeight = 2f;
    public float crouchHeight = 1f;
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

    [Header("Zemin Kontrolü")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;
    private bool wasGrounded;

    // --- YENİ EKLENDİ: İNİŞ SERSEMLEMESİ (LAND STUN) ---
    [Header("İniş Ayarları (Land)")]
    [Tooltip("Karakter yere indiğinde kaç saniye boyunca hareket edemeyip animasyonun bitmesini beklesin?")]
    public float landStunDuration = 0.15f; // GÜNCELLENDİ: Kaymayı önlemek için süre kısaltıldı!
    private float landStunTimer = 0f;

    // --- YENİ EKLENDİ: ERKEN İNİŞ ---
    [Tooltip("Yere ne kadar mesafe kala iniş animasyonu başlasın?")]
    public float nearGroundDistance = 1.2f;
    private bool isNearGround;

    [Header("Mızrak Ayarları")]
    public bool isLanceEquipped = true;
    public GameObject lancePrefab;
    public float throwForce = 100f;

    // --- YENİ EKLENDİ: FIRLATMA SENKRONİZASYONU ---
    [Tooltip("Tıkladıktan kaç saniye sonra mızrak elden çıksın?")]
    public float throwDelay = 0.2f;
    private bool isThrowing = false; // Spam yapmayı engeller

    public float lanceJumpMultiplier = 1f;
    public float latchRadius = 1.5f;

    // --- GÜNCELLENDİ: Üstüne Çıkmak Yerine Altından Asılma Mesafesi ---
    [Tooltip("Karakter mızrağın neresinden tutunacak? (-1.5 mızrağın altı demektir)")]
    public float lanceHangOffset = -1.5f;

    // --- YENİ EKLENDİ: MIZRAK ROTASYON VE DUVAR OFFSET AYARLARI ---
    [Tooltip("Yeni prefab ters duruyorsa bu değerlerle oyna. Eski mızrak için X=90'dı. Yenisinde hepsini 0 yapıp test edebilirsin.")]
    public Vector3 lanceRotationOffset = new Vector3(90f, 0f, 0f);

    [Tooltip("Karakterin duvara girmemesi için mızraktan dışarı doğru (geriye) mesafesi.")]
    public float lanceWallOffset = 0.8f;

    // --- YEPYENİ EKLENDİ: KENDİ Z EKSENİNDE (İLERİ/GERİ) KAYDIRMA ---
    [Tooltip("Karakterin kendi Z ekseninde (ileri/geri) mızrağa göre konumu. Elleri hizalamak için kullan.")]
    public float lanceForwardOffset = 0f;

    [HideInInspector] public bool isLatched = false;
    private Transform latchedLance;
    [HideInInspector] public bool isZiplining = false; // Zipline teline takıldık mı?

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
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;

        animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        currentSpeed = speed;

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

        if (iFrames > 0)
        {
            iFrames -= Time.deltaTime;
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isControlled && Input.GetKeyDown(healKey))
        {
            UseHealthPotion();
        }

        if (isControlled && Input.GetKeyDown(slowTimeKey))
        {
            UseSlowPotion();
        }

        if (isLatched)
        {
            if (latchedLance != null)
            {
                // --- GÜNCELLENDİ: DUVAR YÖNÜNDE OFFSET VE KENDİ Z EKSENİNDE OFFSET UYGULAMASI ---
                Vector3 pushAwayDir = -latchedLance.forward; // Varsayılan geri itme yönü
                LanceObj lanceScript = latchedLance.GetComponent<LanceObj>();

                if (lanceScript != null)
                {
                    // Mızrak duvara saplandığında kaydettiği duvar normalini (dışarı yönü) kullan!
                    pushAwayDir = lanceScript.wallNormal;
                }

                // Hem Yüksekliği, Hem Duvardan Mesafeyi, Hem de Z ekseni (İleri/Geri) elleri hizalamayı ekliyoruz.
                transform.position = latchedLance.position + (Vector3.up * lanceHangOffset) + (pushAwayDir * lanceWallOffset) + (transform.forward * lanceForwardOffset);
            }
            else
            {
                DetachAndJump();
                return;
            }

            SetAimMode(false);
            if (isControlled && Input.GetButtonDown("Jump")) DetachAndJump();
            return;
        }

        // --- YENİ EKLENDİ: Zipline'da kayarken zıplama ile teli bırakma ---
        if (isZiplining && isControlled && Input.GetButtonDown("Jump"))
        {
            isZiplining = false;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount = 1;
            if (animator != null) animator.SetTrigger("Jump");
        }

        // GÜNCELLENDİ: İçerken tutunma iptal
        if (isControlled && Input.GetKeyDown(KeyCode.C) && !isDrinking)
        {
            CheckForLanceLatch();
        }

        // GÜNCELLENDİ: İçerken dash iptal
        if (isControlled && Input.GetKeyDown(KeyCode.E) && !isDashing && isGrounded && dashCooldownTimer <= 0f && !isDrinking)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

        if (landStunTimer > 0)
        {
            landStunTimer -= Time.deltaTime;
        }

        // GÜNCELLENDİ: Zipline'dayken veya içerken eğilme/yürüme iptal
        if (!isDashing && !isLatched && !isZiplining && !isDrinking)
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

            if (isCrouching) currentSpeed = crouchSpeed;
            else if (isWalking) currentSpeed = walkSpeed;
            else if (isControlled && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) currentSpeed = sprintSpeed;
            else currentSpeed = speed;

            if (isTimePotionActive)
            {
                currentSpeed = currentSpeed * 1.5f;
            }

            float targetScaleY = isCrouching ? crouchHeight : normalHeight;
            Vector3 targetScale = new Vector3(transform.localScale.x, targetScaleY, transform.localScale.z);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * crouchTransitionSpeed);
        }

        // GÜNCELLENDİ: İçerken aim alma iptal
        bool isAiming = isControlled && Input.GetMouseButton(1) && !isDrinking;
        float targetFOV = normalFOV;
        float targetOffsetX = 0f;
        float targetOffsetY = 0f;

        // GÜNCELLENDİ: Zipline'dayken veya içerken mızrak fırlatma iptal
        if (isLanceEquipped && !isDashing && !isZiplining && !isDrinking)
        {
            if (isAiming)
            {
                SetAimMode(true);

                // --- GÜNCELLENDİ: ARTIK DİREKT FIRLATMAK YERİNE COROUTINE ÇAĞIRIYORUZ ---
                if (isControlled && Input.GetMouseButtonDown(0) && !isThrowing)
                {
                    StartCoroutine(ThrowRoutine());
                }

                targetFOV = aimFOV;
                targetOffsetX = aimOffsetX;
                targetOffsetY = aimOffsetY;
            }
            else
            {
                SetAimMode(false);
            }
        }
        else if (isDashing || isZiplining || isDrinking)
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

        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (!isGrounded && velocity.y < 0)
        {
            isNearGround = Physics.Raycast(groundCheck.position, Vector3.down, nearGroundDistance, groundMask);
        }
        else
        {
            isNearGround = isGrounded;
        }

        // --- GÜNCELLENDİ: Yüksek yere zıplama bug'ı için velocity şartı kaldırıldı ---
        if (!wasGrounded && isGrounded && !isZiplining)
        {
            if (animator != null) animator.SetTrigger("Land");
            landStunTimer = landStunDuration;
        }

        // --- YENİ EKLENDİ: Zipline'dayken yerçekimini kapat ---
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

        // GÜNCELLENDİ: İksir içerken WASD iptal
        float horizontal = (isControlled && !isDrinking) ? Input.GetAxisRaw("Horizontal") : 0f;
        float vertical = (isControlled && !isDrinking) ? Input.GetAxisRaw("Vertical") : 0f;
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        float animSpeed = 0f;

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
        else if (isZiplining || isDrinking) // GÜNCELLENDİ: İçerken animSpeed 0 olur
        {
            // Zipline'da manuel hareket yok, ray bizi taşıyacak (animasyon hızı 0)
            animSpeed = 0f;
        }
        else
        {
            if (!isAiming)
            {
                if ((isControlled && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f) || inputDir.magnitude < 0.1f)
                {
                    referenceYaw = cam.eulerAngles.y;
                }

                if (inputDir.magnitude >= 0.1f)
                {
                    if (landStunTimer > 0)
                    {
                        animSpeed = 0f;
                    }
                    else
                    {
                        animSpeed = currentSpeed;
                        float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + referenceYaw;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);

                        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                        if (controller.enabled) controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
                    }
                }
            }
            else
            {
                float yawCamera = cam.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0, yawCamera, 0);

                if (inputDir.magnitude >= 0.1f && landStunTimer <= 0)
                {
                    animSpeed = currentSpeed * 0.6f;
                    Vector3 moveDir = (transform.forward * vertical + transform.right * horizontal).normalized;
                    if (controller.enabled) controller.Move(moveDir * (currentSpeed * 0.6f) * Time.deltaTime);
                }
            }
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isNearGround", isNearGround);
            animator.SetFloat("VerticalVelocity", velocity.y);

            // --- YENİ EKLENDİ: Zipline sinyalini Animatör'e gönder ---
            animator.SetBool("isZiplining", isZiplining);

            // --- YENİ EKLENDİ: MIZRAK ANİMASYON SİNYALLERİ ---
            animator.SetBool("isAiming", isAiming);
            animator.SetBool("isLanceHanging", isLatched);

            if (isAiming)
            {
                animator.SetFloat("AimSpeed", vertical, 0.1f, Time.deltaTime);
            }
        }

        // GÜNCELLENDİ: Zipline'dayken ve iksir içerken buradan zıplayamasın
        if (isControlled && Input.GetButtonDown("Jump") && jumpCount < maxJumps && !isDashing && landStunTimer <= 0 && !isZiplining && !isDrinking)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;

            if (animator != null) animator.SetTrigger("Jump");
        }

        if (isControlled && Input.GetButtonUp("Jump") && velocity.y > 0f && !isZiplining)
        {
            velocity.y *= jumpCutMultiplier;
        }

        if (!isZiplining) // Zipline'dayken yerçekimini kapat
        {
            velocity.y += gravity * Time.deltaTime;
        }

        if (controller.enabled) controller.Move(velocity * Time.deltaTime);
    }

    // --- YENİ EKLENDİ: GECİKMELİ FIRLATMA İÇİN COROUTINE ---
    private System.Collections.IEnumerator ThrowRoutine()
    {
        isThrowing = true; // Spam'ı engelle

        if (animator != null) animator.SetTrigger("Throw");

        // Animasyondaki el ileri gitme anını bekle (Inspector'dan ayarlanır)
        yield return new WaitForSeconds(throwDelay);

        ThrowLance(); // Mızrağı tam bu anda fırlat!

        // Animasyon bitene kadar biraz daha kilitli tut ki tuşa basıp durmasın
        yield return new WaitForSeconds(0.4f);

        isThrowing = false; // Tekrar atmaya hazır
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

            if (!isTimePotionActive)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
            else
            {
                Time.timeScale = slowTimeAmount;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
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

        // GÜNCELLENDİ: Artık rotasyon ofsetini Inspector'daki ayarlardan okuyor
        newLance.transform.rotation = Quaternion.LookRotation(flightDirection) * Quaternion.Euler(lanceRotationOffset);

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

        Vector3 pushAwayDir = -lance.forward;
        LanceObj lanceScript = lance.GetComponent<LanceObj>();

        if (lanceScript != null)
        {
            pushAwayDir = lanceScript.wallNormal;
        }

        // ========================================================
        // --- GÜNCELLENDİ: ATLANILAN YÖNE GÖRE DİNAMİK YAN DÖNME ---
        // ========================================================
        // Karakterin mızrağa atlarkenki baktığı yönü (transform.forward), 
        // duvarın yüzeyine paralel olacak şekilde yansıtıyoruz (ProjectOnPlane).
        Vector3 lookDirection = Vector3.ProjectOnPlane(transform.forward, pushAwayDir);
        lookDirection.y = 0f; // Karakter dimdik dursun, yana yatmasın

        // Güvenlik kilidi: Eğer oyuncu duvara çapraz değil de tam 90 derece bodoslama atladıysa
        // yön bulamaz, o zaman mecburen rastgele bir yan tarafa döndürüyoruz ki hata vermesin.
        if (lookDirection.magnitude < 0.05f)
        {
            lookDirection = Vector3.Cross(Vector3.up, pushAwayDir);
        }

        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
        }
        // ========================================================

        transform.position = lance.position + (Vector3.up * lanceHangOffset) + (pushAwayDir * lanceWallOffset) + (transform.forward * lanceForwardOffset);

        // --- YENİ EKLENDİ: SADECE BİR KERE ÇALIŞAN KAPI ZİLİ (TRIGGER) ---
        if (animator != null) animator.SetTrigger("LanceCatch");
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

        if (animator != null) animator.SetTrigger("Jump");
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

        Debug.Log("🩸 Don Quixote HASAR ALDI! Kalan Can: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("💀 Don Öldü! Tüm fizik ve durumlar sıfırlanıyor...");

        // 1. DON'A ÖZEL DURUM KİLİTLERİNİ SIFIRLA
        isDrinking = false;
        isLatched = false;
        isThrowing = false; // Mızrak atarken ölürse kilitli kalmasın

        // 2. CANLARI FULLE
        if (DualRealityManager.Instance != null)
        {
            DualRealityManager.Instance.ResetAllHealth();
        }

        // 3. KUTULARI YERİNE GÖNDER
        PushableBox.ResetAllBoxes();

        // 4. FİZİK SIFIRLAMA VE IŞINLAMA
        velocity = Vector3.zero; // Düşüş/Zıplama hızını sıfırla

        Vector3 respawnPos = CheckpointManager.Instance.GetLastCheckpoint();

        controller.enabled = false;
        transform.position = respawnPos;
        controller.Move(Vector3.zero); // İçeride kalmış eski itme gücünü temizle
        controller.enabled = true;

        velocity = Vector3.zero; // Garanti sıfırlama
    }

    // --- GÜNCELLENDİ: İKSİR İÇME KONTROLLERİ ---
    public void UseHealthPotion()
    {
        // Yerde değilsek, zaten içiyorsak, dash atıyorsak vs. engelle
        if (!isGrounded || isDrinking || isZiplining || isDashing || isLatched) return;

        if (healthPotionCount > 0 && currentHealth < maxHealth)
        {
            StartCoroutine(DrinkPotionRoutine(true));
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
        // Yerde değilsek, zaten içiyorsak vs. engelle
        if (!isGrounded || isDrinking || isZiplining || isDashing || isLatched) return;

        if (slowPotionCount > 0 && !isTimePotionActive)
        {
            StartCoroutine(DrinkPotionRoutine(false));
        }
        else if (isTimePotionActive)
        {
            Debug.Log("Zaman zaten yavaş kanka!");
        }
        else
        {
            Debug.Log("Hiç zaman iksirin kalmamış!");
        }
    }

    // --- YENİ EKLENDİ: İKSİR İÇME ANİMASYON VE GECİKME COROUTINE'İ ---
    private System.Collections.IEnumerator DrinkPotionRoutine(bool isHealthPotion)
    {
        isDrinking = true; // Kilidi kapat, WASD ve zıplama iptal olsun

        // Karakterin anlık hızını sıfırla ki buzda kayar gibi içmesin
        velocity.x = 0f;
        velocity.z = 0f;

        // Animator'e sinyal yolla
        if (animator != null) animator.SetTrigger("DrinkPotion");

        // 1 saniye bekle
        yield return new WaitForSeconds(2f);

        // 1 saniye sonra efekti ver
        if (isHealthPotion)
        {
            healthPotionCount--;
            currentHealth += healthPotionHealAmount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log("💚 İksir İçildi! Yeni Can: " + currentHealth + " | Kalan İksir: " + healthPotionCount);
        }
        else
        {
            slowPotionCount--;
            StartCoroutine(SlowTimeRoutine());
            Debug.Log("⏳ Zaman İksiri İçildi! Kalan İksir: " + slowPotionCount);
        }

        isDrinking = false; // Kilidi aç, karakter tekrar hareket etsin
    }

    private System.Collections.IEnumerator SlowTimeRoutine()
    {
        isTimePotionActive = true;
        Time.timeScale = slowTimeAmount;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(slowTimeDuration);

        isTimePotionActive = false;

        if (!Input.GetMouseButton(1))
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        Debug.Log("⏳ Zaman normale döndü!");
    }
}