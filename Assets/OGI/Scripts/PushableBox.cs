using UnityEngine;

// Bu satır sayesinde kodu objeye attığın an Unity otomatik Rigidbody ekler!
[RequireComponent(typeof(Rigidbody))]
public class PushableBox : MonoBehaviour
{
    [Header("--- İllüzyon Modelleri ---")]
    public GameObject sanchoKutuModeli;
    public GameObject donOrsModeli;

    [Header("--- İtme/Çekme Ayarları ---")]
    public float pushSpeed = 3f;
    public float grabDistance = 1.2f;

    private bool isGrabbed = false;
    private Transform playerTransform;
    private CharacterController playerCC;
    private MonoBehaviour playerMovementScript;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // --- KUTU FİZİĞİ AYARLARI ---
        rb.mass = 50f; // 50 kilo
        rb.drag = 0f;  // Yerde buz gibi kaymasın diye sürtünme
        rb.constraints = RigidbodyConstraints.FreezeRotation; // İttirirken takla atıp devrilmesin

        if (DualRealityManager.Instance != null)
        {
            UpdatePerception(DualRealityManager.Instance.isDonActive);
        }
    }

    void Update()
    {
        bool isDon = DualRealityManager.Instance != null && DualRealityManager.Instance.isDonActive;
        UpdatePerception(isDon);

        // Don aktifse örs olur, zorla bıraktırırız
        if (isDon && isGrabbed)
        {
            ReleaseBox();
            return;
        }

        // C tuşu ile Tutma/Bırakma (Sadece Sancho)
        if (!isDon && Input.GetKeyDown(KeyCode.C))
        {
            if (isGrabbed) ReleaseBox();
            else TryGrab();
        }

        
        if (isGrabbed && playerCC != null)
        {
            float vertical = Input.GetAxis("Vertical");   // W ve S
            float horizontal = Input.GetAxis("Horizontal"); // A ve D (YENİ)

            // --- YENİ: A VE D İLE KARAKTERİ (VE KUTUYU) DÖNDÜR ---
            float turnSpeed = 60f; // Dönüş hızı (İstersen bunu en yukarıya public float olarak da ekleyebilirsin)
            playerTransform.Rotate(0, horizontal * turnSpeed * Time.deltaTime, 0);

            // --- İLERİ / GERİ İTME ---
            Vector3 moveDir = playerTransform.forward * vertical * pushSpeed;
            moveDir.y = -9.81f; // Yerçekimi

            playerCC.Move(moveDir * Time.deltaTime);
        }
    }

    private void UpdatePerception(bool isDon)
    {
        if (donOrsModeli != null) donOrsModeli.SetActive(isDon);
        if (sanchoKutuModeli != null) sanchoKutuModeli.SetActive(!isDon);

        // --- İLLÜZYON FİZİĞİ ---
        if (rb != null && !isGrabbed)
        {
            if (isDon)
            {
                rb.isKinematic = true; // Don için örs: Sabit kalır, itilemez, havadaysa düşmez
            }
            else
            {
                rb.isKinematic = false; // Sancho için kutu: Yerçekimi çalışır, yere düşer
            }
        }
    }

    private void TryGrab()
    {
        Debug.Log("🔍 Sancho C'ye bastı! Kutu etrafı taranıyor...");

        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
        bool playerBulundu = false;

        foreach (var hit in hits)
        {
            // Karakterin kendisine veya herhangi bir child (alt) objesine değdiysek
            if (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
            {
                playerBulundu = true;

                // Karakterin ana objesini bul (CharacterController'ın olduğu en üst obje)
                playerCC = hit.GetComponentInParent<CharacterController>();

                if (playerCC != null)
                {
                    playerTransform = playerCC.transform;

                    // Scripti bul ve kapat (Artık kesinlikle bulacak)
                    playerMovementScript = playerTransform.GetComponent("SanchoMovement") as MonoBehaviour;

                    if (playerMovementScript != null)
                    {
                        playerMovementScript.enabled = false;
                    }
                    else
                    {
                        Debug.Log("⚠️ DİKKAT: Karakter bulundu ama 'SanchoMovement' scripti bulunamadı!");
                    }

                    isGrabbed = true;
                    rb.isKinematic = true; // Fizik sapıtmasın diye anlık yerçekimini kapatıyoruz

                    AlignPlayerToBox();

                    transform.SetParent(playerTransform, true);
                    Debug.Log("✅ KUTU BAŞARIYLA TUTULDU!");
                    return; // Tutma başarılı, aramayı bitir
                }
            }
        }

        if (!playerBulundu)
        {
            Debug.Log("❌ Kutu etrafında 'Player' tagine sahip hiçbir şey bulamadı! Sancho'nun tagi Untagged kalmış olabilir.");
        }
    }

    private void ReleaseBox()
    {
        if (!isGrabbed) return;

        isGrabbed = false;
        transform.SetParent(null);

        rb.isKinematic = false; // Bırakınca yerçekimi tekrar başlar

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
            if (dot > maxDot)
            {
                maxDot = dot;
                snapDirection = dir;
            }
        }

        Vector3 targetPos = transform.position + (snapDirection * grabDistance);
        targetPos.y = playerTransform.position.y;

        playerCC.enabled = false;
        playerTransform.position = targetPos;
        playerTransform.rotation = Quaternion.LookRotation(-snapDirection);
        playerCC.enabled = true;
    }
}