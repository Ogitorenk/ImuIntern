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

    private float initialY;
    private float initialZ;

    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private Collider[] bladeColliders;

    void Start()
    {
        initialY = transform.localEulerAngles.y;
        initialZ = transform.localEulerAngles.z;

        transform.localRotation = Quaternion.Euler(startAngle, initialY, initialZ);

        lastPosition = transform.position;
        bladeColliders = GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        float currentAngle = startAngle * Mathf.Cos(timer * swingSpeed);

        transform.localRotation = Quaternion.Euler(currentAngle, initialY, initialZ);

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

                // --- YENİ EKLENDİ: Hasar yemeden önceki konumumuzu kaydediyoruz ---
                Vector3 posBeforeHit = cc.transform.position;

                bool isDead = false; // Adam öldü mü kontrolü

                // 1. HASAR VER VE ÖLDÜ MÜ BAK
                if (other.TryGetComponent(out DonMovement don))
                {
                    don.TakeDamage(damage);
                    if (don.currentHealth <= 0) isDead = true;
                }
                else if (other.TryGetComponent(out SanchoMovement sancho))
                {
                    sancho.TakeDamage(damage);
                    if (sancho.currentHealth <= 0) isDead = true;
                }

                // --- SİHİRLİ KİLİT BURASI ---
                // Eğer hasar yedikten sonra karakterin yeri aniden değiştiyse (Ölüp Checkpoint'e ışınlandıysa)
                if (Vector3.Distance(cc.transform.position, posBeforeHit) > 2f)
                {
                    Debug.Log("🚫 Bıçak öldürdü (veya ışınladı), fırlatma İPTAL!");
                    return; // Direkt çık, fırlatma koduna hiç girme!
                }

                // EĞER KARAKTER BU VURUŞLA ÖLMEDİYSE FIRLAT! (Öldüyse zaten ışınlandı, elleme)
                if (!isDead)
                {
                    // A) Bıçağın savrulma yönü 
                    Vector3 swingDirection = currentVelocity;
                    swingDirection.y = 0;
                    swingDirection.Normalize();

                    // B) Karakteri bıçaktan dışarı doğru iten yön 
                    Vector3 outwardPush = (cc.transform.position - bladeColliders[0].bounds.center);
                    outwardPush.y = 0;
                    outwardPush.Normalize();

                    // C) İkisini harmanla! 
                    Vector3 finalDirection = (swingDirection * 1.5f + outwardPush * 0.5f).normalized;

                    // 3. FIRLAT
                    StartCoroutine(ApplyBladeFling(cc, finalDirection));
                }
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

        Vector3 lastFramePos = cc.transform.position;

        while (elapsed < duration)
        {
            if (cc == null || !cc.enabled) break;

            if (Vector3.Distance(cc.transform.position, lastFramePos) > 5f)
            {
                break;
            }

            float currentFling = Mathf.Lerp(flingPower, 0, elapsed / duration);
            vSpeed += Physics.gravity.y * 2.8f * Time.deltaTime;

            Vector3 moveAmount = (direction * currentFling) + (Vector3.up * vSpeed);
            cc.Move(moveAmount * Time.deltaTime);

            lastFramePos = cc.transform.position;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (moveScript != null) moveScript.enabled = true;
        foreach (var col in bladeColliders) col.enabled = true;
    }
}