using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableBox : MonoBehaviour
{
    [Header("--- İllüzyon Modelleri ---")]
    public GameObject sanchoKutuModeli;
    public GameObject donOrsModeli;

    [Header("--- İtme/Çekme Ayarları ---")]
    public float pushSpeed = 3f;
    public float grabDistance = 1.2f;
    public float turnSpeed = 60f;

    [Header("--- Yeni: Basamak Ayarları ---")]
    public float stepHeight = 0.5f;
    public float stepSmooth = 5f;

    private bool isGrabbed = false;
    private Transform playerTransform;
    private CharacterController playerCC;
    private MonoBehaviour playerMovementScript;
    private Rigidbody rb;

    // --- KUTU YERDEYKEN PLATFORM TAKİBİ DEĞİŞKENLERİ ---
    private Transform currentPlatform = null;
    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;

    // --- YENİ EKLENDİ: KUTU TUTULURKEN (PLAYER İÇİN) PLATFORM TAKİBİ DEĞİŞKENLERİ ---
    private Transform grabbedPlatform = null;
    private Vector3 grabbedLocalPos;
    private Vector3 grabbedGlobalPos;
    private Quaternion grabbedLocalRot;
    private Quaternion grabbedGlobalRot;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 50f;
        rb.drag = 0f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (DualRealityManager.Instance != null)
        {
            UpdatePerception(DualRealityManager.Instance.isDonActive);
        }
    }

    void Update()
    {
        bool isDon = DualRealityManager.Instance != null && DualRealityManager.Instance.isDonActive;
        UpdatePerception(isDon);

        if (isDon && isGrabbed)
        {
            ReleaseBox();
            return;
        }

        if (!isDon && Input.GetKeyDown(KeyCode.C))
        {
            if (isGrabbed) ReleaseBox();
            else TryGrab();
        }

        // --- KUTU TUTULURKEN OYUNCU HAREKETİ ---
        if (isGrabbed && playerCC != null)
        {
            // 1. ÖNCE PLATFORMUN HAREKETİNİ OYUNCUYA UYGULA (Kapanan Scriptin Yerine Biz Taşıyoruz)
            HandleGrabbedPlatformTracking();

            // 2. KULLANICI GİRDİSİNİ (WASD) UYGULA
            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            playerTransform.Rotate(0, horizontal * turnSpeed * Time.deltaTime, 0);

            Vector3 moveDir = playerTransform.forward * vertical * pushSpeed;

            float yVel = -9.81f; // Yerçekimi
            if (vertical != 0)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.1f, playerTransform.forward * vertical, 0.8f))
                {
                    if (!Physics.Raycast(transform.position + Vector3.up * stepHeight, playerTransform.forward * vertical, 0.9f))
                    {
                        yVel = stepSmooth;
                    }
                }
            }

            moveDir.y = yVel;
            playerCC.Move(moveDir * Time.deltaTime);

            // 3. BİR SONRAKİ KARE İÇİN HAFIZAYI GÜNCELLE
            UpdateGrabbedPlatformMemory();
        }
    }

    // --- KUTU YERDEYKEN KUSURSUZ ASANSÖR TAKİBİ ---
    void FixedUpdate()
    {
        if (!isGrabbed && currentPlatform != null && rb != null && !rb.isKinematic)
        {
            Vector3 platformDeltaPosition = currentPlatform.position - lastPlatformPosition;

            if (platformDeltaPosition.magnitude > 0.00001f)
            {
                rb.MovePosition(rb.position + platformDeltaPosition);
            }

            Quaternion platformDeltaRotation = currentPlatform.rotation * Quaternion.Inverse(lastPlatformRotation);
            platformDeltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 0.001f)
            {
                rb.MoveRotation(platformDeltaRotation * rb.rotation);
            }

            lastPlatformPosition = currentPlatform.position;
            lastPlatformRotation = currentPlatform.rotation;
        }
    }

    // --- İŞTE SENİ DÜŞMEKTEN KURTARACAK O SİHİRLİ FONKSİYON ---
    private void HandleGrabbedPlatformTracking()
    {
        RaycastHit hit;
        // Oyuncunun merkezinden aşağı ışın atarak üzerinde olduğumuz platformu bul
        if (Physics.Raycast(playerTransform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2f))
        {
            Transform hitTransform = null;

            // MovingIllusionPlatform Kontrolü
            MovingIllusionPlatform mip = hit.collider.GetComponent<MovingIllusionPlatform>();
            if (mip == null) mip = hit.collider.GetComponentInParent<MovingIllusionPlatform>();
            if (mip != null) hitTransform = mip.movingBody;

            if (hitTransform != null)
            {
                // Platforma yeni bindiysek veya platform değiştiyse hafızayı kur
                if (grabbedPlatform != hitTransform)
                {
                    grabbedPlatform = hitTransform;
                    grabbedGlobalPos = playerTransform.position;
                    grabbedLocalPos = grabbedPlatform.InverseTransformPoint(playerTransform.position);
                    grabbedGlobalRot = playerTransform.rotation;
                    grabbedLocalRot = Quaternion.Inverse(grabbedPlatform.rotation) * playerTransform.rotation;
                }

                // Platformun yer değişimini (Delta) bul ve karaktere uygula
                Vector3 newGlobalPos = grabbedPlatform.TransformPoint(grabbedLocalPos);
                Vector3 moveDiff = newGlobalPos - grabbedGlobalPos;

                if (moveDiff.magnitude > 0.0001f)
                {
                    playerCC.Move(moveDiff);
                }

                // Platformun dönüşünü (Rotation) bul ve karaktere uygula
                Quaternion newGlobalRot = grabbedPlatform.rotation * grabbedLocalRot;
                Quaternion rotationDiff = newGlobalRot * Quaternion.Inverse(grabbedGlobalRot);
                rotationDiff.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 0.001f)
                {
                    playerTransform.Rotate(axis, angle, Space.World);
                }
            }
            else
            {
                grabbedPlatform = null;
            }
        }
        else
        {
            grabbedPlatform = null;
        }
    }

    private void UpdateGrabbedPlatformMemory()
    {
        if (grabbedPlatform != null)
        {
            grabbedGlobalPos = playerTransform.position;
            grabbedLocalPos = grabbedPlatform.InverseTransformPoint(playerTransform.position);
            grabbedGlobalRot = playerTransform.rotation;
            grabbedLocalRot = Quaternion.Inverse(grabbedPlatform.rotation) * playerTransform.rotation;
        }
    }

    private void UpdatePerception(bool isDon)
    {
        if (donOrsModeli != null) donOrsModeli.SetActive(isDon);
        if (sanchoKutuModeli != null) sanchoKutuModeli.SetActive(!isDon);

        if (rb != null && !isGrabbed)
        {
            rb.isKinematic = isDon;
        }
    }

    private void TryGrab()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
            {
                playerCC = hit.GetComponentInParent<CharacterController>();
                if (playerCC != null)
                {
                    playerTransform = playerCC.transform;
                    SanchoMovement sm = playerTransform.GetComponent<SanchoMovement>();

                    // --- YENİ EKLENDİ: ZIPLARKEN TUTMA KİLİDİ ---
                    if (sm != null && !sm.isGrounded)
                    {
                        Debug.Log("🚫 Sancho havada, kutu tutulamaz!");
                        return; // Havadaysa işlemi anında kes.
                    }
                    // -------------------------------------------

                    playerMovementScript = playerTransform.GetComponent("SanchoMovement") as MonoBehaviour;

                    // Sancho'nun hareket scripti kapandığı için yukarıdaki HandleGrabbedPlatformTracking devreye giriyor!
                    if (playerMovementScript != null) playerMovementScript.enabled = false;

                    isGrabbed = true;
                    if (DualRealityManager.Instance != null) DualRealityManager.Instance.canSwitch = false;

                    if (sm != null) sm.isHoldingBox = true;

                    rb.isKinematic = true;
                    AlignPlayerToBox();
                    transform.SetParent(playerTransform, true);
                    return;
                }
            }
        }
    }

    private void ReleaseBox()
    {
        if (!isGrabbed) return;
        isGrabbed = false;
        grabbedPlatform = null; // Bıraktığında platform hafızasını sil

        if (DualRealityManager.Instance != null) DualRealityManager.Instance.canSwitch = true;

        if (playerTransform != null)
        {
            SanchoMovement sm = playerTransform.GetComponent<SanchoMovement>();
            if (sm != null) sm.isHoldingBox = false;
        }

        transform.SetParent(null);
        rb.isKinematic = false;

        if (playerMovementScript != null) playerMovementScript.enabled = true;

        if (currentPlatform != null)
        {
            UpdatePlatformOffset();
        }

        playerTransform = null;
        playerCC = null;
    }

    private void AlignPlayerToBox()
    {
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        dirToPlayer.y = 0;
        Vector3 snapDirection = transform.forward;
        float maxDot = -1f;
        Vector3[] directions = { transform.forward, -transform.forward, transform.right, -transform.right };

        foreach (Vector3 dir in directions)
        {
            float dot = Vector3.Dot(dirToPlayer, dir);
            if (dot > maxDot) { maxDot = dot; snapDirection = dir; }
        }

        Vector3 targetPos = transform.position + (snapDirection * grabDistance);
        targetPos.y = playerTransform.position.y;

        playerCC.enabled = false;
        playerTransform.position = targetPos;
        playerTransform.rotation = Quaternion.LookRotation(-snapDirection);
        playerCC.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MovingPlatform"))
        {
            currentPlatform = other.transform;
            if (!isGrabbed)
            {
                UpdatePlatformOffset();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MovingPlatform"))
        {
            currentPlatform = null;
        }
    }

    private void UpdatePlatformOffset()
    {
        if (currentPlatform != null)
        {
            lastPlatformPosition = currentPlatform.position;
            lastPlatformRotation = currentPlatform.rotation;
        }
    }
}