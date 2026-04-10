using UnityEngine;
using System.Collections;

public class WallPusher : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    [Tooltip("Oyun baþladýðýnda ilk fýrlamadan önce kaç saniye beklesin? (Pistonlarý sýraya sokmak için)")]
    public float initialDelay = 0f; // --- YENÝ EKLENDÝ ---

    [Tooltip("Piston ne kadar uzaða fýrlasýn?")]
    public float pushDistance = 4f;
    [Tooltip("Ýleri fýrlama hýzý (Çok hýzlý olmalý)")]
    public float attackSpeed = 25f;
    [Tooltip("Geri çekilme hýzý (Yavaþ olmalý)")]
    public float retractSpeed = 5f;
    [Tooltip("Ýki saldýrý arasýndaki bekleme süresi")]
    public float waitTime = 2f;

    [Header("Hasar ve Fýrlatma")]
    public float damage = 30f;
    [Tooltip("Karakteri boþluða itme gücü")]
    public float knockbackForce = 60f;
    [Tooltip("Karakteri hafif havaya kaldýrýr ki ayaklarý yerden kesilip uçsun")]
    public float upwardForce = 15f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isAttacking = false; // Sadece ileri fýrlarken hasar versin

    void Start()
    {
        startPos = transform.position;
        // Pistonun "Z" ekseni (Mavi Ok) nereye bakýyorsa oraya uzar.
        targetPos = startPos + (transform.forward * pushDistance);

        StartCoroutine(PusherRoutine());
    }

    IEnumerator PusherRoutine()
    {
        // --- YENÝ EKLENDÝ: ÝLK BAÞLAMA GECÝKMESÝ ---
        // Sadece oyun baþladýðýnda 1 kere çalýþýr ve pistonu sýraya sokar
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        while (true)
        {
            // Yuvasýnda bekle
            yield return new WaitForSeconds(waitTime);

            // 1. SALDIRI (Hýzlýca ileri atýl)
            isAttacking = true;
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, attackSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos; // Tam uca oturt

            // Uçta çok kýsa bekle
            yield return new WaitForSeconds(0.5f);

            // 2. GERÝ ÇEKÝLME (Yavaþça yuvaya dön)
            isAttacking = false; // Geri dönerken oyuncuya çarpýp fýrlatmasýn
            while (Vector3.Distance(transform.position, startPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPos, retractSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = startPos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Sadece piston ÝLERÝ doðru atýlýrken oyuncuya deðerse fýrlat
        if (isAttacking && other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponentInParent<CharacterController>();
            if (cc != null)
            {
                // Hasar Ver
                if (other.TryGetComponent(out DonMovement don)) don.TakeDamage(damage);
                else if (other.TryGetComponent(out SanchoMovement sancho)) sancho.TakeDamage(damage);

                // FIRLATMA YÖNÜ: Pistonun baktýðý yön + hafif yukarý
                Vector3 flingDirection = (transform.forward * 1.5f + Vector3.up * 0.5f).normalized;

                // Oyuncuyu uçuracak Coroutine'i baþlat
                StartCoroutine(ApplyKnockback(cc, flingDirection));
            }
        }
    }

    IEnumerator ApplyKnockback(CharacterController cc, Vector3 direction)
    {
        MonoBehaviour moveScript = cc.GetComponent<DonMovement>();
        if (moveScript == null) moveScript = cc.GetComponent<SanchoMovement>();

        // Oyuncunun kendi hareketini kes (Havada çaresiz kalsýn)
        if (moveScript != null) moveScript.enabled = false;

        float duration = 0.5f;
        float elapsed = 0f;
        float vSpeed = upwardForce; // Havaya zýplatma

        while (elapsed < duration)
        {
            if (cc != null)
            {
                // Güç zamanla azalarak (Lerp) sýfýra iner
                float currentPush = Mathf.Lerp(knockbackForce, 0, elapsed / duration);
                vSpeed += Physics.gravity.y * 3f * Time.deltaTime; // Yerçekimi etkisi

                Vector3 moveAmount = (direction * currentPush) + (Vector3.up * vSpeed);
                cc.Move(moveAmount * Time.deltaTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Yere düþünce hareketi geri ver
        if (moveScript != null) moveScript.enabled = true;
    }
}