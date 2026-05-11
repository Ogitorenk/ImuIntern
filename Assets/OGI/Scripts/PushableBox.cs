using UnityEngine;
using System.Collections.Generic;

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
    private Animator playerAnimator;

    private Transform currentPlatform = null;
    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;

    private Transform grabbedPlatform = null;
    private Vector3 grabbedLocalPos;
    private Vector3 grabbedGlobalPos;
    private Quaternion grabbedLocalRot;
    private Quaternion grabbedGlobalRot;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    public static List<PushableBox> allBoxes = new List<PushableBox>();

    void Awake()
    {
        if (!allBoxes.Contains(this)) allBoxes.Add(this);
    }

    void OnDestroy()
    {
        if (allBoxes.Contains(this)) allBoxes.Remove(this);
    }

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        rb = GetComponent<Rigidbody>();
        // GÜNCELLENDİ: Oyuncu yürüyerek çarpıp itemesin diye tank gibi ağır yaptık
        rb.mass = 99999f;
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

        if (!isDon && Input.GetKeyDown(KeyCode.F))
        {
            if (isGrabbed) ReleaseBox();
            else TryGrab();
        }

        if (isGrabbed && playerCC != null)
        {
            HandleGrabbedPlatformTracking();

            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            if (Mathf.Abs(vertical) > 0.01f)
            {
                float turnAmount = horizontal * turnSpeed * Mathf.Sign(vertical) * Time.deltaTime;
                playerTransform.Rotate(0, turnAmount, 0);
            }

            Vector3 moveDir = playerTransform.forward * vertical * pushSpeed;

            // ========================================================
            // --- YENİ EKLENDİ: KUTU DUVAR ÇARPIŞMA RADARI (BOXCAST) ---
            // ========================================================
            if (Mathf.Abs(vertical) > 0.01f)
            {
                Vector3 direction = playerTransform.forward * Mathf.Sign(vertical);
                float distance = (pushSpeed * Time.deltaTime) + 0.15f;
                Collider col = GetComponent<Collider>();

                if (col != null)
                {
                    // Yere sürtüp takılmasın diye radarın boyutunu %85'e çektik
                    Vector3 extents = col.bounds.extents * 0.85f;
                    RaycastHit[] hits = Physics.BoxCastAll(col.bounds.center, extents, direction, transform.rotation, distance);

                    foreach (var hit in hits)
                    {
                        // Radar kendine, oyuncuya, triggerlara veya hareketli platformlara takılmasın
                        if (hit.collider.gameObject != gameObject &&
                            hit.collider.transform.root != playerTransform.root &&
                            !hit.collider.isTrigger &&
                            !hit.collider.CompareTag("MovingPlatform"))
                        {
                            // Aha duvar! İleri gitmeyi anında iptal et.
                            moveDir.x = 0;
                            moveDir.z = 0;
                            break;
                        }
                    }
                }
            }
            // ========================================================

            float yVel = -9.81f;
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

            if (playerAnimator != null)
            {
                playerAnimator.SetFloat("PushPull", vertical, 0.1f, Time.deltaTime);
            }

            UpdateGrabbedPlatformMemory();
        }
    }

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

    private void HandleGrabbedPlatformTracking()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerTransform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2f))
        {
            Transform hitTransform = null;

            MovingIllusionPlatform mip = hit.collider.GetComponent<MovingIllusionPlatform>();
            if (mip == null) mip = hit.collider.GetComponentInParent<MovingIllusionPlatform>();
            if (mip != null) hitTransform = mip.movingBody;

            if (hitTransform != null)
            {
                if (grabbedPlatform != hitTransform)
                {
                    grabbedPlatform = hitTransform;
                    grabbedGlobalPos = playerTransform.position;
                    grabbedLocalPos = grabbedPlatform.InverseTransformPoint(playerTransform.position);
                    grabbedGlobalRot = playerTransform.rotation;
                    grabbedLocalRot = Quaternion.Inverse(grabbedPlatform.rotation) * playerTransform.rotation;
                }

                Vector3 newGlobalPos = grabbedPlatform.TransformPoint(grabbedLocalPos);
                Vector3 moveDiff = newGlobalPos - grabbedGlobalPos;

                if (moveDiff.magnitude > 0.0001f)
                {
                    playerCC.Move(moveDiff);
                }

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

            // ========================================================
            // --- GÜNCELLENDİ: TUTMADAN İTTİRMEYİ KÖKTEN ENGELLEME ---
            // ========================================================
            if (!isDon)
            {
                // Sancho modunda kutu düşebilsin ama oyuncu çarparak itemesin diye X ve Z kilitli!
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            }
            else
            {
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
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

                    if (sm != null && !sm.isGrounded)
                    {
                        Debug.Log("🚫 Sancho havada, kutu tutulamaz!");
                        return;
                    }

                    playerMovementScript = playerTransform.GetComponent("SanchoMovement") as MonoBehaviour;
                    playerAnimator = playerTransform.GetComponentInChildren<Animator>();

                    if (playerMovementScript != null) playerMovementScript.enabled = false;

                    isGrabbed = true;
                    if (DualRealityManager.Instance != null) DualRealityManager.Instance.canSwitch = false;

                    if (playerAnimator != null) playerAnimator.SetBool("isHoldingBox", true);

                    // Tutulduğu an kilitleri açıp karakterin hareketine uyumlu hale getiriyoruz
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
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
        grabbedPlatform = null;

        if (DualRealityManager.Instance != null) DualRealityManager.Instance.canSwitch = true;

        if (playerAnimator != null) playerAnimator.SetBool("isHoldingBox", false);

        transform.SetParent(null);
        rb.isKinematic = false;

        if (playerMovementScript != null) playerMovementScript.enabled = true;

        if (currentPlatform != null)
        {
            UpdatePlatformOffset();
        }

        playerTransform = null;
        playerCC = null;
        playerAnimator = null;
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

    public void ResetToOriginalPosition()
    {
        if (isGrabbed)
        {
            ReleaseBox();
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public static void ResetAllBoxes()
    {
        foreach (var box in allBoxes)
        {
            if (box != null) box.ResetToOriginalPosition();
        }
    }
}