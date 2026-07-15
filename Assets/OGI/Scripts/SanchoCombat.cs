using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine; // KAMERA İÇİN GEREKLİ KÜTÜPHANE
using TMPro; // TMPro KÜTÜPHANESİ YENİ EKLENDİ!

public class SanchoCombat : MonoBehaviour
{
    [Header("--- SCRIPTABLE OBJECT DATA ---")]
    [SerializeField] private CharacterData sanchoData; // Sancho'nun ok sayısını tutacak olan ortak veri dosyası kanka

    private SanchoMovement sanchoMovement;
    private Animator animator;

    [Header("Görsel Silahlar")]
    public GameObject meleeWeaponPivot;
    [Tooltip("Elde/Sırtta belirecek olan Quiver (Yay/Sadak) objesi")]
    public GameObject bowPivot;

    [Header("Nişan Alma (Kamera Zoom & Kaydırma)")]
    public CinemachineFreeLook normalCamera; // Sancho'nun kullandığı FreeLook Kamera

    public float normalFOV = 40f;
    public float aimFOV = 20f;

    [Tooltip("Nişan alırken karakteri sağa almak için negatif (-1), sola almak için pozitif (1)")]
    public float aimOffsetX = -1f;

    [Tooltip("Nişan alırken kamerayı ne kadar yukarı kaldıracağını belirler (Örn: 0.5 veya 1.2)")]
    public float aimOffsetY = 0.8f;

    public float zoomSpeed = 10f;
    private float currentOffsetX = 0f;
    private float currentOffsetY = 0f;

    private float[] baseOffsetX = new float[3];
    private float[] baseOffsetY = new float[3];

    [Header("Yakın Dövüş Kombo Ayarları")]
    public float comboResetTime = 1.0f;
    public float attack1Duration = 1.0f;
    public float attack2Duration = 1.0f;

    // === YENİ EKLENDİ: STAMINA MALİYETLERİ VE EŞİĞİ ===
    [Header("Stamina Maliyetleri")]
    public float attack1StaminaCost = 10f;
    public float attack2StaminaCost = 15f;
    public float bowShootStaminaCost = 10f; // Ok atmanın stamina maliyeti
    private float minimumAttackThreshold = 10f; // Vuruş için gereken minimum eşik kanka

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    [HideInInspector] public bool isAttacking = false;
    private Coroutine attackResetRoutine;

    [Header("--- Sancho Yakın Dövüş Hasar Ayarları ---")]
    [Tooltip("Sancho'nun önünde duracak ve vuruşun merkez noktasını belirleyecek boş obje")]
    public Transform attackPoint;
    [Tooltip("Vuruşun menzili (Menzil küresinin yarıçapı)")]
    public float attackRange = 1.3f; // Sancho biraz daha kısa boylu olduğu için menzili çıtırık küçük tuttuk kanka
    [Tooltip("Kılıç/Topuz savurunca verilecek yakın dövüş hasarı")]
    public float meleeDamage = 20f; // Sadık yaverimiz 20 vursun şimdilik
    [Tooltip("Sol tık bastıktan kaç saniye sonra hasar düşmana işlesin? (Vuruş gecikmesi)")]
    public float hitDelay = 0.2f;

    [Header("Okçuluk Ayarları")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float arrowForce = 40f;
    public float fireRate = 1.5f;

    [HideInInspector] public bool isAiming = false;
    private float lastFireTime = 0f;

    // === SPAM ENGELLEME SİSTEMİ İÇİN COOLDOWN SÖZLÜĞÜ ===
    private Dictionary<IDamageable, float> enemyHitCooldowns = new Dictionary<IDamageable, float>();
    private float globalHitCooldown = 0.25f; // Saniyede en fazla 4 darbe yiyebilirler kanka erimezler

    [Header("--- UI ENTEGRASYONU ---")]
    [Tooltip("Ok bittiğinde ekrana gelecek küçük uyarı UI nesnesi")]
    [SerializeField] private GameObject noArrowWarningUI;
    [Tooltip("Uyarının ekranda kaç saniye kalacağını belirler")]
    [SerializeField] private float warningDuration = 2.0f;
    private Coroutine warningRoutine;

    [Tooltip("Ekranda '5 / 20' yazacak olan TextMeshPro nesnesi")]
    [SerializeField] private TextMeshProUGUI arrowCounterText;

    void Start()
    {
        sanchoMovement = GetComponent<SanchoMovement>();
        animator = GetComponentInChildren<Animator>();

        if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(false);
        if (bowPivot != null) bowPivot.SetActive(false);

        if (noArrowWarningUI != null) noArrowWarningUI.SetActive(false);

        if (sanchoData != null)
        {
            Debug.Log($"<color=green>🏹 Sancho_Data başarıyla okundu! Mevcut Ok: {sanchoData.arrowCount}</color>");
        }

        UpdateArrowCounterUI();

        if (normalCamera != null)
        {
            normalCamera.m_Lens.FieldOfView = normalFOV;
            currentOffsetX = 0f;
            currentOffsetY = 0f;

            for (int i = 0; i < 3; i++)
            {
                var composer = normalCamera.GetRig(i).GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    baseOffsetX[i] = composer.m_TrackedObjectOffset.x;
                    baseOffsetY[i] = composer.m_TrackedObjectOffset.y;
                }
            }
        }
    }

    void Update()
{
    if (Time.timeScale == 0f) return;

    // --- YENİ EKLENEN KISIM: Diyalog Aktifse Tüm Atak ve Nişan Durumlarını Sıfırla ve Çık ---
    if (DialogueManager.Instance != null && DialogueManager.Instance.IsInteractiveDialogueActive)
    {
        isAiming = false;
        if (animator != null) animator.SetBool("isAiming", false);
        if (bowPivot != null) bowPivot.SetActive(false);
        if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(false);

        HandleCameraZoomAndOffset(); // Kamera merkeze dönsün, yakın kalmasın
        return; // Atak ve Nişan fonksiyonlarının çalışmasını engelle
    }
    // -------------------------------------------------------------------------------------

    if (!sanchoMovement.isControlled || sanchoMovement.currentHealth <= 0 || sanchoMovement.isDrinking ||
        sanchoMovement.isRepairing || sanchoMovement.isZiplining || sanchoMovement.isDodging ||
        sanchoMovement.isCrawling || sanchoMovement.isCrouchToggled || sanchoMovement.isHoldingBox)
    {
        isAiming = false;
        if (animator != null) animator.SetBool("isAiming", false);
        if (bowPivot != null) bowPivot.SetActive(false);
        if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(false);

        HandleCameraZoomAndOffset(); // Güvenlik: Kamera merkeze dönsün
        return;
    }

    HandleAiming();
    HandleMeleeAttack();
    HandleCameraZoomAndOffset(); // Her karede kameranın zoom'unu/kaymasını denetle
}

    void HandleAiming()
    {
        if (Input.GetMouseButton(1) && !isAttacking && sanchoMovement.isGrounded)
        {
            isAiming = true;
            if (bowPivot != null) bowPivot.SetActive(true);
            if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(true);

            if (Input.GetMouseButtonDown(0) && Time.time >= lastFireTime + fireRate)
            {
                // === YENİ: OK ATARKEN STAMINA GÜVENLİK DUVARI ===
                if (sanchoMovement.currentStamina < bowShootStaminaCost)
                {
                    Debug.LogWarning($"<color=orange>⚠️ OK ATILAMADI! </color> Stamina yetersiz! Gereken: {bowShootStaminaCost} | Mevcut: {Mathf.RoundToInt(sanchoMovement.currentStamina)}");
                    return; // Ok atma iptal kanka
                }

                int currentArrows = sanchoData != null ? sanchoData.arrowCount : 0;

                if (currentArrows > 0)
                {
                    // Staminayı pürüzsüzce düşür
                    sanchoMovement.UseStamina(bowShootStaminaCost);
                    Debug.Log($"<color=magenta>🏹 Yay Gerildi! </color> Harcanan Stamina: {bowShootStaminaCost} | <color=green>Kalan Stamina: {Mathf.RoundToInt(sanchoMovement.currentStamina)}</color>");

                    FireArrow();
                }
                else
                {
                    Debug.LogWarning("🏹 Sancho'nun oku bitti! Kutuları kırıp ok toplaman lazım kanka!");
                    TriggerNoArrowWarning();
                }
            }
        }
        else
        {
            isAiming = false;
            if (bowPivot != null) bowPivot.SetActive(false);
            if (sanchoMovement.crosshairUI != null) sanchoMovement.crosshairUI.SetActive(false);
        }
    }

    void HandleCameraZoomAndOffset()
    {
        if (normalCamera == null) return;

        float targetFOV = isAiming ? aimFOV : normalFOV;
        float targetOffsetX = isAiming ? aimOffsetX : 0f;
        float targetOffsetY = isAiming ? aimOffsetY : 0f;

        normalCamera.m_Lens.FieldOfView = Mathf.Lerp(normalCamera.m_Lens.FieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        currentOffsetX = Mathf.Lerp(currentOffsetX, targetOffsetX, Time.deltaTime * zoomSpeed);
        currentOffsetY = Mathf.Lerp(currentOffsetY, targetOffsetY, Time.deltaTime * zoomSpeed);

        for (int i = 0; i < 3; i++)
        {
            var composer = normalCamera.GetRig(i).GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                Vector3 offset = composer.m_TrackedObjectOffset;
                offset.x = baseOffsetX[i] + currentOffsetX;
                offset.y = baseOffsetY[i] + currentOffsetY;
                composer.m_TrackedObjectOffset = offset;
            }
        }
    }

    void FireArrow()
    {
        if (sanchoData != null)
        {
            sanchoData.arrowCount--;
        }

        UpdateArrowCounterUI();

        Debug.Log($"🏹 Ok atıldı! Kalan Ok: {(sanchoData != null ? sanchoData.arrowCount : 0)}");

        lastFireTime = Time.time;
        if (animator != null) animator.SetTrigger("FireArrow");

        // --- HATA DÜZELTİLDİ: AudioManager içindeki 'arrowShootSound' ile milimetrik eşitlendi kanka ---
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.arrowShootSound, transform.position);

        if (arrowPrefab != null && firePoint != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 targetPoint;

            int layerMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

            if (Physics.Raycast(ray, out hit, 200f, layerMask))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(200f);
            }

            Vector3 direction = (targetPoint - firePoint.position).normalized;

            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.LookRotation(direction));
            Rigidbody rb = arrow.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.velocity = direction * arrowForce;
            }
        }
    }

    private void UpdateArrowCounterUI()
    {
        if (arrowCounterText != null)
        {
            int currentArrows = sanchoData != null ? sanchoData.arrowCount : 0;
            int maxArrows = sanchoData != null ? sanchoData.maxArrowCount : 20;
            arrowCounterText.text = $"{currentArrows}/{maxArrows}";
        }
    }

    private void TriggerNoArrowWarning()
    {
        if (noArrowWarningUI == null) return;

        if (warningRoutine != null) StopCoroutine(warningRoutine);

        warningRoutine = StartCoroutine(ShowWarningRoutine());
    }

    private IEnumerator ShowWarningRoutine()
    {
        noArrowWarningUI.SetActive(true);
        yield return new WaitForSeconds(warningDuration);
        noArrowWarningUI.SetActive(false);
    }

    void HandleMeleeAttack()
    {
        if (Input.GetMouseButtonDown(0) && !isAiming && sanchoMovement.isGrounded)
        {
            // === YENİ: YAKIN DÖVÜŞ İÇİN 10 STAMINA EŞİĞİ KONTROLÜ ===
            if (sanchoMovement.currentStamina < minimumAttackThreshold)
            {
                Debug.LogWarning($"<color=red>🛑 SANCHO ATAK ENGELLENDİ! </color> Stamina 10'un altında! | <color=yellow>Mevcut Stamina: {Mathf.RoundToInt(sanchoMovement.currentStamina)}</color>");
                return; // Stamina yoksa direkt koddan çık, spamı kes kanka!
            }

            int nextStep = comboStep;
            if (Time.time - lastAttackTime > comboResetTime) nextStep = 0;

            nextStep++;

            // Kombo adımına göre requiredStamina'yı belirle
            float requiredStamina = (nextStep == 1) ? attack1StaminaCost : attack2StaminaCost;

            // Eğer eşik(10) üstünde ama gereken maliyet(15) karşılanmıyorsa pas geç kanka
            if (sanchoMovement.currentStamina < requiredStamina)
            {
                Debug.LogWarning($"<color=orange>⚠️ SANCHO STAMINA YETERSİZ! </color> Kombo {nextStep} için gereken: {requiredStamina} | <color=yellow>Mevcut Stamina: {Mathf.RoundToInt(sanchoMovement.currentStamina)}</color>");
                return;
            }

            // Stamina engellerini geçtik, artık komboyu mühürle
            comboStep = nextStep;
            lastAttackTime = Time.time;

            if (attackResetRoutine != null) StopCoroutine(attackResetRoutine);

            if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(true);

            // === HAREKETLİ COMBAT SİHRİ ===
            isAttacking = true;
            if (animator != null) animator.SetBool("isAttacking", true);

            // Staminayı pürüzsüzce eksilt
            sanchoMovement.UseStamina(requiredStamina);

            // --- SES ENTEGRASYONU (Sancho Yakın Dövüş/Savurma Ses Tetiği) ---
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.sanchoMeleeSound, transform.position);

            Debug.Log($"<color=cyan>⚔️ Sancho Atak Yaptı (Kombo {comboStep}) -> </color> Harcanan Stamina: {requiredStamina} | <color=green>Kalan Stamina: {Mathf.RoundToInt(sanchoMovement.currentStamina)}</color>");

            if (comboStep == 1)
            {
                animator.ResetTrigger("Attack2");
                animator.SetTrigger("Attack1");

                StartCoroutine(DealMeleeDamageWithDelay(hitDelay));
                attackResetRoutine = StartCoroutine(ResetAttackState(attack1Duration));
            }
            else if (comboStep >= 2)
            {
                animator.ResetTrigger("Attack1");
                animator.SetTrigger("Attack2");
                comboStep = 0;

                StartCoroutine(DealMeleeDamageWithDelay(hitDelay));
                attackResetRoutine = StartCoroutine(ResetAttackState(attack2Duration));
            }
        }
    }

    private IEnumerator DealMeleeDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (attackPoint == null)
        {
            Debug.LogError("🚨 KANKA! SanchoCombat içindeki 'Attack Point' kutusu boş! Sancho'nun önüne boş bir obje açıp bağla!");
            yield break;
        }

        // === BUGFIX: ~0 Katman maskesi çakıldı, kutular hangi layerda olursa olsun zınk diye kırılacak kanka! ===
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider enemyCollider in hitEnemies)
        {
            if (enemyCollider.gameObject.CompareTag("Player")) continue;

            // 1. KIRILABİLİR NESNE DETEKSİYONU
            SharedBreakableObject breakable = enemyCollider.GetComponent<SharedBreakableObject>();
            if (breakable == null) breakable = enemyCollider.GetComponentInParent<SharedBreakableObject>();

            if (breakable != null)
            {
                breakable.BreakIt();
                continue; // Kutuyu patlattıysak düşman arama koduna hiç girme kanka atla
            }

            // 2. DÜŞMAN HASAR KONTROLÜ
            IDamageable enemy = enemyCollider.GetComponent<IDamageable>();
            if (enemy == null) enemy = enemyCollider.GetComponentInParent<IDamageable>();
            if (enemy == null) enemy = enemyCollider.GetComponentInChildren<IDamageable>();

            if (enemy != null)
            {
                // === SPAM ENGELLEYİCİ GÜVENLİK DUVARI ===
                if (enemyHitCooldowns.ContainsKey(enemy) && Time.time < enemyHitCooldowns[enemy])
                {
                    continue; // Cooldown dolmadıysa hasarı pas geç kanka!
                }

                enemy.TakeDamage(meleeDamage);

                // Bir sonraki vuruş zamanını mühürle
                enemyHitCooldowns[enemy] = Time.time + globalHitCooldown;

                Debug.Log($"⚔️ Sancho yakın dövüşle {enemyCollider.name} objesine {meleeDamage} hasar verdi!");
            }
        }
    }

    private IEnumerator ResetAttackState(float delay)
    {
        float safeDelay = Mathf.Max(0f, delay - 0.15f);
        yield return new WaitForSeconds(safeDelay);

        isAttacking = false;

        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
            animator.ResetTrigger("Attack1");
            animator.ResetTrigger("Attack2");
        }

        if (meleeWeaponPivot != null) meleeWeaponPivot.SetActive(false);
        comboStep = 0;
    }

    public bool AddArrows(int amount)
    {
        if (sanchoData != null)
        {
            if (sanchoData.arrowCount >= sanchoData.maxArrowCount)
            {
                Debug.Log("🏹 Sancho'nun ok çantası ağzına kadar dolu! (+20)");
                return false;
            }

            sanchoData.arrowCount += amount;
            sanchoData.arrowCount = Mathf.Clamp(sanchoData.arrowCount, 0, sanchoData.maxArrowCount);
        }

        UpdateArrowCounterUI();

        Debug.Log($"🏹 Yerden Ok Alındı! Mevcut Ok Sayısı: {(sanchoData != null ? sanchoData.arrowCount : 0)}");
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}