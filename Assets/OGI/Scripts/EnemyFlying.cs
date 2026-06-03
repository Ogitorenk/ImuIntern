using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EnemyFlying : MonoBehaviour, IDamageable
{
    [Header("Sağlık Ayarları")]
    public float maxHealth = 50f; // Arı/Ejderha çıtır olsun, havada vurması zor olur
    private float currentHealth;
    private bool isDead = false;
    private bool isTakingDamage = false;

    [Header("Uçuş ve Takip Ayarları")]
    public float flySpeed = 4f;
    public float chaseRange = 20f;
    [Tooltip("Düşman yerden ne kadar yüksekte uçsun? Don'un yüz hizası için ideal: 1.2 ile 1.5 arasıdır kanka.")]
    public float hoverHeight = 1.3f;
    [Tooltip("Düşmanının oyuncuya dönme hızı. Ne kadar yüksekse o kadar seri yüzünü sana çevirir.")]
    public float rotationSpeed = 5f;

    private Transform player;
    private Rigidbody rb;

    [Header("Saldırı Ayarları")]
    public float attackRange = 2.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 3f;
    [Tooltip("İğnesini/Ateşini tam çıkarma anı (Gecikme saniyesi)")]
    public float attackHitDelay = 0.4f;

    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    [Header("Çift Gerçeklik (Modeller)")]
    [Tooltip("Don Kişot'un göreceği Ejderha 3D Modeli")]
    public GameObject dragonModel;
    [Tooltip("Sancho'nun göreceği Arı 3D Modeli")]
    public GameObject beeModel;

    [Header("Çift Gerçeklik (Animatorler)")]
    [Tooltip("Don Kişot'un göreceği Ejderha Animator'ü")]
    public Animator dragonAnimator;
    [Tooltip("Sancho'nun göreceği Arı Animator'ü")]
    public Animator beeAnimator;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        FindActivePlayer();

        if (DualRealityManager.Instance != null)
        {
            UpdateModelVisibility(DualRealityManager.Instance.isDonActive);
        }
        else
        {
            UpdateModelVisibility(true);
        }
    }

    void Update()
    {
        if (isDead) return;

        FindActivePlayer();

        if (player == null)
        {
            SetAnimBool("isFlying", false);
            rb.velocity = Vector3.zero;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isTakingDamage || isAttacking)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        Vector3 lookTargetPosition = player.position + Vector3.up * hoverHeight;
        Vector3 lookDirection = lookTargetPosition - transform.position;

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            Vector3 eulerAngles = targetRotation.eulerAngles;
            eulerAngles.z = 0f;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(eulerAngles), Time.deltaTime * rotationSpeed);
        }

        if (distanceToPlayer <= attackRange)
        {
            rb.velocity = Vector3.zero;
            SetAnimBool("isFlying", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else if (distanceToPlayer <= chaseRange)
        {
            Vector3 targetPosition = player.position + Vector3.up * hoverHeight;
            Vector3 moveDirection = (targetPosition - transform.position).normalized;

            rb.velocity = moveDirection * flySpeed;
            SetAnimBool("isFlying", true);
        }
        else
        {
            rb.velocity = Vector3.zero;
            SetAnimBool("isFlying", true);
        }
    }

    public void UpdateModelVisibility(bool isDonActive)
    {
        if (dragonModel != null)
        {
            dragonModel.SetActive(isDonActive);
        }

        if (beeModel != null)
        {
            beeModel.SetActive(!isDonActive);
        }
    }

    void FindActivePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.activeInHierarchy)
            {
                player = p.transform;
                return;
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        TriggerAnim("Attack");

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && !isTakingDamage && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= attackRange + 0.5f)
            {
                IDamageable target = player.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(attackDamage);
                    Debug.Log($"🐝/🐲 Düşman oyuncuya {attackDamage} hasar vurdu!");
                }
            }
        }

        yield return new WaitForSeconds(1.5f - attackHitDelay);
        isAttacking = false;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} Uçan canavar hasar yedi! Kalan Can: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(DamageRoutine());
        }
    }

    private IEnumerator DamageRoutine()
    {
        isTakingDamage = true;
        TriggerAnim("Damage");

        isAttacking = false;

        yield return new WaitForSeconds(0.5f);

        if (!isDead) isTakingDamage = false;
    }

    // ==============================================================================================
    // --- GÜNCELLENDİ: ÖLÜM ANINDA REZALET ŞEKİLDE AŞAĞI DÜŞME ENGELİ ---
    // ==============================================================================================
    private void Die()
    {
        isDead = true;
        rb.velocity = Vector3.zero;

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

        TriggerAnim("Death");

        Debug.Log($"{gameObject.name} Havada vuruldu ve yere düşüyor...");

        // SİLİNDİ: Destroy satırını buradan sildik kanka, çünkü artık tamamen yukarıdaki OnCollisionEnter (Zemine değme) anı sayacak!
    }

    // ==============================================================================================
    // --- YENİ EKLENDİ: ZEMİNE ÇAKILINCA HAVADA ASILI KALMA/SABİTLENME HİLESİ (GROUND CHECK) ---
    // ==============================================================================================
    // ==============================================================================================
    // --- GÜNCELLENDİ: ZEMİNE UZANMA VE 3 SANİYE SONRA YOK OLMA SİSTEMİ ---
    // ==============================================================================================
    void OnCollisionEnter(Collision collision)
    {
        // Karakter ölüyse VE çarptığı yer senin o yürüdüğün yolların tag'i olan "Ground" ise:
        if (isDead && (collision.gameObject.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground")))
        {
            // 1. Fiziği tamamen donduruyoruz kanka, zemine çivileniyor
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;

            // 2. Don ve Sancho cesede takılmasın diye collider'ı kapatıyoruz
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Debug.Log($"📌 {gameObject.name} Ground zeminine uzandı. 3 saniye sonra yok olacak kanka!");

            // 3. KRİTİK DEĞİŞİKLİK: Eski `Die()` içindeki yok etmeyi iptal edip, tam zemine değdiği andan itibaren 3 saniye saydırıyoruz!
            Destroy(gameObject, 3f);
        }
    }

    private void SetAnimBool(string name, bool value)
    {
        if (dragonAnimator != null && dragonAnimator.gameObject.activeInHierarchy) dragonAnimator.SetBool(name, value);
        if (beeAnimator != null && beeAnimator.gameObject.activeInHierarchy) beeAnimator.SetBool(name, value);
    }

    private void TriggerAnim(string name)
    {
        if (dragonAnimator != null && dragonAnimator.gameObject.activeInHierarchy) dragonAnimator.SetTrigger(name);
        if (beeAnimator != null && beeAnimator.gameObject.activeInHierarchy) beeAnimator.SetTrigger(name);
    }
}