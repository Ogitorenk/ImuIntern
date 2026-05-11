using UnityEngine;
using System.Collections;

public class ZiplinePrefab : MonoBehaviour
{
    [Header("Referanslar (Child Objerler)")]
    public Transform startPoint;
    public Transform endPoint;
    public Transform visualRope;

    // ========================================================
    // --- GÜNCELLENDİ: HİZALAMA AYARLARI BÖLÜNDÜ ---
    // ========================================================
    [Header("Ayarlar (Base - Don Kişot'a Göre Ayarla)")]
    [Tooltip("Genel hız.")]
    public float zipSpeed = 12f;

    [Tooltip("Base Yukarı / Aşağı (Don Kişot tam ipteyken bu kalsın)")]
    public float playerOffset = -2.2f;

    [Tooltip("Base Sağ / Sol")]
    public float playerLateralOffset = 0f;

    [Tooltip("Base İleri / Geri")]
    public float playerForwardOffset = 0f;

    // --- YENİ EKLENDİ: SANCHO'YA ÖZEL EKSTRA AYARLAR ---
    [Header("Sancho Ekstra Ayarları (Base Üzerine Eklenir)")]
    [Tooltip("Sancho'nun eli Don'a göre altta kalıyorsa buraya POZİTİF (örn: 0.5) değer vererek onu yukarı kaydır.")]
    public float sanchoEkstraY = 0f;

    [Tooltip("Sancho'yu sağa/sola ekstra kaydırmak için.")]
    public float sanchoEkstraLateral = 0f;

    [Tooltip("Sancho'yu ileri/geri ekstra kaydırmak için.")]
    public float sanchoEkstraForward = 0f;
    // ========================================================

    [Header("İnteraksiyon")]
    public KeyCode interactKey = KeyCode.F;

    public static bool isAnyPlayerZiplining = false;

    private bool playerInRange = false;
    private GameObject currentPlayer;
    private bool isZipping = false;

    void Start()
    {
        UpdateRopeVisual();
    }

    void OnValidate()
    {
        if (startPoint != null && endPoint != null && visualRope != null)
            UpdateRopeVisual();
    }

    void Update()
    {
        if (playerInRange && !isZipping && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(ZipRoutine());
        }
    }

    void UpdateRopeVisual()
    {
        visualRope.position = (startPoint.position + endPoint.position) / 2f;
        visualRope.up = (endPoint.position - startPoint.position).normalized;

        float dist = Vector3.Distance(startPoint.position, endPoint.position);
        visualRope.localScale = new Vector3(0.1f, dist / 2f, 0.1f);
    }

    IEnumerator ZipRoutine()
    {
        isZipping = true;
        isAnyPlayerZiplining = true;

        CharacterController cc = currentPlayer.GetComponent<CharacterController>();
        DonMovement don = currentPlayer.GetComponent<DonMovement>();
        SanchoMovement sancho = currentPlayer.GetComponent<SanchoMovement>();

        if (cc != null) cc.enabled = false;

        Vector3 zipDirection = (endPoint.position - startPoint.position).normalized;
        zipDirection.y = 0f;

        if (zipDirection != Vector3.zero)
        {
            currentPlayer.transform.rotation = Quaternion.LookRotation(zipDirection);
        }

        if (don != null) don.isZiplining = true;
        if (sancho != null) sancho.isZiplining = true;

        Vector3 rightDirection = Vector3.Cross(Vector3.up, zipDirection).normalized;

        float distance = Vector3.Distance(startPoint.position, endPoint.position);
        float t = 0;

        // --- YENİ EKLENDİ: ŞU ANKİ OYUNCU SANCHO MU? ---
        bool isCurrentPlayerSancho = sancho != null && (DualRealityManager.Instance != null && !DualRealityManager.Instance.isDonActive);
        // ------------------------------------------------

        while (t < 1f)
        {
            if (Input.GetButtonDown("Jump"))
            {
                break;
            }

            t += (zipSpeed / distance) * Time.deltaTime;

            Vector3 targetPos = Vector3.Lerp(startPoint.position, endPoint.position, t);

            // 1. ÖNCE BASE (DON'UN) OFSETLERİNİ UYGULA
            Vector3 finalOffset = (Vector3.up * playerOffset) + (rightDirection * playerLateralOffset) + (zipDirection * playerForwardOffset);

            // ========================================================
            // --- GÜNCELLENDİ: SANCHO İSE EKSTRA OFSETLERİ EKLE ---
            // ========================================================
            if (isCurrentPlayerSancho)
            {
                // Sancho eli altta kalıyor demiştin, sanchoEkstraY'ye pozitif değer verirsen onu yukarı çekeriz.
                finalOffset += (Vector3.up * sanchoEkstraY) + (rightDirection * sanchoEkstraLateral) + (zipDirection * sanchoEkstraForward);
            }
            // ========================================================

            currentPlayer.transform.position = targetPos + finalOffset;

            yield return null;
        }

        if (don != null) don.isZiplining = false;
        if (sancho != null) sancho.isZiplining = false;

        if (cc != null) cc.enabled = true;

        isAnyPlayerZiplining = false;
        isZipping = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            currentPlayer = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (!isZipping) currentPlayer = null;
        }
    }
}