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

    // Asansörü takip etmek için değişken
    private Transform currentPlatform = null;

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

        // --- SENİN HAREKET KODUN (DOKUNULMADI) ---
        if (isGrabbed && playerCC != null)
        {
            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            playerTransform.Rotate(0, horizontal * turnSpeed * Time.deltaTime, 0);

            Vector3 moveDir = playerTransform.forward * vertical * pushSpeed;

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
        Debug.Log("🔍 Sancho C'ye bastı! Kutu etrafı taranıyor...");
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
            {
                playerCC = hit.GetComponentInParent<CharacterController>();
                if (playerCC != null)
                {
                    playerTransform = playerCC.transform;
                    playerMovementScript = playerTransform.GetComponent("SanchoMovement") as MonoBehaviour;

                    if (playerMovementScript != null) playerMovementScript.enabled = false;

                    isGrabbed = true;
                    if (DualRealityManager.Instance != null) DualRealityManager.Instance.canSwitch = false;

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

        if (DualRealityManager.Instance != null) DualRealityManager.Instance.canSwitch = true;

        // --- TRIGGER TEMELLİ ASANSÖR ÇÖZÜMÜ ---
        // Eğer bir asansörün trigger'ı içindeysek ona bağlan, yoksa null yap.
        transform.SetParent(currentPlatform);

        rb.isKinematic = false;
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        playerTransform = null;
        playerCC = null;
        Debug.Log("❌ Sancho Kutuyu Bıraktı.");
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

    // --- ASANSÖRÜN TETİKLEYİCİSİNE GİRDİĞİNDE ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MovingPlatform"))
        {
            currentPlatform = other.transform;
            // Eğer o an tutmuyorsak asansöre yapış
            if (!isGrabbed) transform.SetParent(currentPlatform);
        }
    }

    // --- ASANSÖRÜN TETİKLEYİCİSİNDEN ÇIKTIĞINDA ---
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MovingPlatform"))
        {
            currentPlatform = null;
            // Eğer o an tutmuyorsak bağını kopar
            if (!isGrabbed) transform.SetParent(null);
        }
    }
}