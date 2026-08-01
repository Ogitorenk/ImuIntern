using UnityEngine;
using System.Collections;

public class WallPusher : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    [Tooltip("Oyun baþladýðýnda ilk fýrlamadan önce kaç saniye beklesin? (Pistonlarý sýraya sokmak için)")]
    public float initialDelay = 0f;

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

    [Header("Ses Menzil Ayarlarý (Büyütüldü)")]
    [Tooltip("Piston fýrlama sesinin duyulmaya baþlayacaðý maksimum mesafe kanka.")]
    public float maxAudioDistance = 35f; // Menzil 35 metreye çýkarýldý!

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isAttacking = false;
    private Transform playerTransform;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + (transform.forward * pushDistance);

        StartCoroutine(PusherRoutine());
    }

    void Update()
    {
        // Sahnede aktif olan oyuncuyu canlý tespit etme
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            DonMovement don = FindObjectOfType<DonMovement>();
            if (don != null && don.gameObject.activeInHierarchy)
            {
                playerTransform = don.transform;
            }
            else
            {
                SanchoMovement sancho = FindObjectOfType<SanchoMovement>();
                if (sancho != null && sancho.gameObject.activeInHierarchy)
                {
                    playerTransform = sancho.transform;
                }
                else
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null && playerObj.activeInHierarchy)
                    {
                        playerTransform = playerObj.transform;
                    }
                }
            }
        }
    }

    IEnumerator PusherRoutine()
    {
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            // 1. SALDIRI ANI: MESAFEYE GÖRE PÝSTON SESÝNÝ PATLAT KANKA
            PlayPusherSoundByPlayerDistance();

            isAttacking = true;
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, attackSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;

            yield return new WaitForSeconds(0.5f);

            // 2. GERÝ ÇEKÝLME
            isAttacking = false;
            while (Vector3.Distance(transform.position, startPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPos, retractSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = startPos;
        }
    }

    private void PlayPusherSoundByPlayerDistance()
    {
        if (AudioManager.Instance == null || AudioManager.Instance.pusherSound == null) return;

        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance <= maxAudioDistance)
            {
                float baseVolume = AudioManager.Instance.pusherVolume;

                // DOYGUN SES EÐRÝSÝ: Yakýndayken ses hemen %100 olur, uzaklaþýnca yavaþça düþer kanka!
                float linearProximity = 1f - (distance / maxAudioDistance);
                float boostedProximity = Mathf.Sqrt(linearProximity); // Yakýnlarda ses artýk çok daha gür!

                float finalVolume = Mathf.Clamp01(boostedProximity * baseVolume);

                if (finalVolume > 0.001f)
                {
                    AudioManager.Instance.PlaySound(AudioManager.Instance.pusherSound, transform.position, finalVolume);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isAttacking && (other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
        {
            CharacterController cc = other.GetComponentInParent<CharacterController>();
            if (cc != null)
            {
                if (other.TryGetComponent(out DonMovement don)) don.TakeDamage(damage);
                else if (other.TryGetComponent(out SanchoMovement sancho)) sancho.TakeDamage(damage);

                Vector3 flingDirection = (transform.forward * 1.5f + Vector3.up * 0.5f).normalized;
                StartCoroutine(ApplyKnockback(cc, flingDirection));
            }
        }
    }

    IEnumerator ApplyKnockback(CharacterController cc, Vector3 direction)
    {
        MonoBehaviour moveScript = cc.GetComponent<DonMovement>();
        if (moveScript == null) moveScript = cc.GetComponent<SanchoMovement>();

        if (moveScript != null) moveScript.enabled = false;

        float duration = 0.5f;
        float elapsed = 0f;
        float vSpeed = upwardForce;

        while (elapsed < duration)
        {
            if (cc != null)
            {
                float currentPush = Mathf.Lerp(knockbackForce, 0, elapsed / duration);
                vSpeed += Physics.gravity.y * 3f * Time.deltaTime;

                Vector3 moveAmount = (direction * currentPush) + (Vector3.up * vSpeed);
                cc.Move(moveAmount * Time.deltaTime);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (moveScript != null) moveScript.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxAudioDistance);
    }
}