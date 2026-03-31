using UnityEngine;
using System.Collections;

public class SwingingLog : MonoBehaviour
{
    [Header("Hasar Ayarları")]
    public float damage = 50f;

    [Header("Ekstrem Fırlatma Ayarları")]
    public float flingPower = 80f;
    public float flingUpward = 20f;

    [Header("Sallanma Ayarları")]
    public float swingSpeed = 3f;
    public float startAngle = 90f;

    private bool isTriggered = false;
    private float timer = 0f;

    // --- COOLDOWN SİSTEMİ ---
    private float hitCooldown = 3f; // 3 saniye bekleme
    private float lastHitTime = -10f;

    private Collider[] logColliders;

    void Start()
    {
        transform.localRotation = Quaternion.Euler(startAngle, 0, 0);

        // Kütüğün ve menteşenin içindeki tüm fiziksel alanları bul
        logColliders = GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        if (isTriggered)
        {
            timer += Time.deltaTime;
            float currentAngle = startAngle * Mathf.Cos(timer * swingSpeed);
            transform.localRotation = Quaternion.Euler(currentAngle, 0, 0);
        }
    }

    public void ReleaseLog()
    {
        isTriggered = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 3 saniye geçmeden bir daha tetiklenme!
        if (isTriggered && Time.time > lastHitTime + hitCooldown && (other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
        {
            CharacterController cc = other.GetComponentInParent<CharacterController>();
            MonoBehaviour playerScript = null;

            if (cc != null)
            {
                lastHitTime = Time.time;
                Debug.Log($"<color=red>💥 DARBE:</color> 3 saniyelik cooldown başladı.");

                // 1. HASAR VER
                DonMovement don = cc.GetComponent<DonMovement>();
                SanchoMovement sancho = cc.GetComponent<SanchoMovement>();

                if (don != null)
                {
                    don.TakeDamage(damage);
                    playerScript = don;
                }
                else if (sancho != null)
                {
                    sancho.TakeDamage(damage);
                    playerScript = sancho;
                }

                // --- 2. DİNAMİK YÖN HESAPLAMASI (İşte Büyü Burada) ---
                // Tavandaki menteşeyi değil, oyuncuya çarpan GERÇEK odunun merkezini bul
                Vector3 actualLogCenter = logColliders[0].bounds.center;

                // Kütüğün merkezinden -> Oyuncuya doğru bir hat çiz (Nereden vurursa tersine iter)
                Vector3 dynamicDirection = (cc.transform.position - actualLogCenter);
                dynamicDirection.y = 0; // Sadece yatayda uçsun
                dynamicDirection = dynamicDirection.normalized; // Gücü 1'e sabitle ki senin 80'lik flingPower'ı bozmasın

                // 3. FIRLAT
                StartCoroutine(ApplyExtremeFling(cc, dynamicDirection, playerScript));
            }
        }
    }

    IEnumerator ApplyExtremeFling(CharacterController cc, Vector3 direction, MonoBehaviour playerScript)
    {
        // Karakterin fren yapmasını engelle
        if (playerScript != null) playerScript.enabled = false;

        // Kütüğü 1 saniyeliğine "Hayalet" yap (Üstüne binmemen için)
        foreach (Collider col in logColliders)
        {
            if (col != null) col.enabled = false;
        }

        float duration = 0.8f;
        float elapsed = 0f;
        float verticalVelocity = flingUpward;

        while (elapsed < duration)
        {
            if (cc != null && cc.enabled)
            {
                // İtme gücünü zamanla yumuşatarak azalt (Gerçekçi uçuş)
                float currentHorizontalForce = Mathf.Lerp(flingPower, 0, elapsed / duration);
                Vector3 moveVector = direction * currentHorizontalForce;

                // Parabolik uçuş için yerçekimini ekle
                verticalVelocity += Physics.gravity.y * 2.5f * Time.deltaTime;
                moveVector.y = verticalVelocity;

                cc.Move(moveVector * Time.deltaTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Uçuş bittiğinde karakterin kontrollerini geri ver
        if (playerScript != null) playerScript.enabled = true;

        // Kütüğü tekrar katı hale getir
        foreach (Collider col in logColliders)
        {
            if (col != null) col.enabled = true;
        }
    }
}