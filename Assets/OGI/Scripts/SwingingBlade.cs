using UnityEngine;
using System.Collections;

public class SwingingBlade : MonoBehaviour
{
    [Header("Bıçak Ayarları")]
    public float damage = 40f;
    public float flingPower = 85f;
    public float flingUpward = 20f;

    [Header("Sallanma Ayarları")]
    public float swingSpeed = 2.5f;
    public float startAngle = 80f;

    private float hitCooldown = 1.0f;
    private float lastHitTime = -10f;
    private float timer = 0f;

    // --- YENİ EKLENDİ: BAŞLANGIÇ ROTASYON HAFIZASI ---
    private float initialY;
    private float initialZ;

    // --- YÖN BULUCU SİSTEM ---
    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private Collider[] bladeColliders;

    void Start()
    {
        // Editörde verdiğin Y (sağ/sol) ve Z (eğim) açılarını hafızaya alıyoruz!
        initialY = transform.localEulerAngles.y;
        initialZ = transform.localEulerAngles.z;

        // Başlangıçta o hafızadaki değerleri kullanarak başla
        transform.localRotation = Quaternion.Euler(startAngle, initialY, initialZ);

        lastPosition = transform.position;
        bladeColliders = GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float currentAngle = startAngle * Mathf.Cos(timer * swingSpeed);

        // X ekseninde sallan, ama Editördeki Y ve Z dönüşlerini SIFIRLAMA!
        transform.localRotation = Quaternion.Euler(currentAngle, initialY, initialZ);

        // BIÇAĞIN DÜNYADAKİ GERÇEK HIZINI HESAPLA
        Vector3 currentPos = bladeColliders[0].bounds.center;
        currentVelocity = (currentPos - lastPosition) / Time.deltaTime;
        lastPosition = currentPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time > lastHitTime + hitCooldown && other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponentInParent<CharacterController>();
            if (cc != null)
            {
                lastHitTime = Time.time;

                // 1. HASAR VER
                if (other.TryGetComponent(out DonMovement don)) don.TakeDamage(damage);
                else if (other.TryGetComponent(out SanchoMovement sancho)) sancho.TakeDamage(damage);

                // --- 2. GERÇEKÇİ FİZİK YÖNÜ HESAPLAMA ---

                // A) Bıçağın savrulma yönü (Yukarı kalkmayı iptal ediyoruz, sadece yatay itiş)
                Vector3 swingDirection = currentVelocity;
                swingDirection.y = 0;
                swingDirection.Normalize();

                // B) Karakteri bıçaktan dışarı doğru iten yön (Karakter bıçağın içine girmesin diye)
                Vector3 outwardPush = (cc.transform.position - bladeColliders[0].bounds.center);
                outwardPush.y = 0;
                outwardPush.Normalize();

                // C) İkisini harmanla! (Bıçağın gidiş yönü daha ağır basar: 1.5f çarpanı)
                // Böylece oyuncu tam bıçağın gittiği yöne ama hafifçe dışa doğru savrulur.
                Vector3 finalDirection = (swingDirection * 1.5f + outwardPush * 0.5f).normalized;

                // 3. FIRLAT
                StartCoroutine(ApplyBladeFling(cc, finalDirection));
            }
        }
    }

    IEnumerator ApplyBladeFling(CharacterController cc, Vector3 direction)
    {
        MonoBehaviour moveScript = cc.GetComponent<DonMovement>();
        if (moveScript == null) moveScript = cc.GetComponent<SanchoMovement>();

        if (moveScript != null) moveScript.enabled = false;
        foreach (var col in bladeColliders) col.enabled = false;

        float duration = 0.6f;
        float elapsed = 0f;
        float vSpeed = flingUpward;

        while (elapsed < duration)
        {
            if (cc != null)
            {
                float currentFling = Mathf.Lerp(flingPower, 0, elapsed / duration);
                vSpeed += Physics.gravity.y * 2.8f * Time.deltaTime;

                // Yeni hesapladığımız kusursuz direction vektörü burada çalışıyor
                Vector3 moveAmount = (direction * currentFling) + (Vector3.up * vSpeed);
                cc.Move(moveAmount * Time.deltaTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (moveScript != null) moveScript.enabled = true;
        foreach (var col in bladeColliders) col.enabled = true;
    }
}