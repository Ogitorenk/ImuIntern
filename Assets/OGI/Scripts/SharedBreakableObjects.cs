using UnityEngine;
using System.Collections;

public class SharedBreakableObject : MonoBehaviour
{
    [Header("--- Görsel ve Fizik ---")]
    [Tooltip("Kýrýlacak olan 3D model (Küp, Vazo vb.)")]
    public GameObject visualModel;
    [Tooltip("Objenin çarpýþma kutusu (BoxCollider vb.)")]
    public Collider objectCollider;

    [Header("--- Parçalanma Efekti ---")]
    [Tooltip("Kýrýlma anýnda patlayacak Particle System")]
    public ParticleSystem breakParticles;

    [Header("--- Yeniden Doðma Ayarlarý ---")]
    [Tooltip("Kýrýldýktan bir süre sonra geri gelsin mi?")]
    public bool respawnable = false;
    public float respawnTime = 3f;

    private bool isBroken = false;

    // Bu fonksiyonu karakterin saldýrý (Attack) kodundan çaðýracaðýz
    public void BreakIt()
    {
        if (isBroken) return;
        isBroken = true;

        // 1. Modeli ve çarpýþmayý kapat (Karakter içinden geçebilsin diye)
        if (visualModel != null) visualModel.SetActive(false);
        if (objectCollider != null) objectCollider.enabled = false;

        // 2. Partikül efektini patlat!
        if (breakParticles != null)
        {
            breakParticles.Play();
        }

        // 3. Yeniden doðacaksa sayacý baþlat, yoksa objeyi tamamen yok et
        if (respawnable)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            // Partikülün bitme süresini hesapla ve sonra objeyi sahneden tamamen sil
            float destroyDelay = breakParticles != null ? breakParticles.main.duration : 0.1f;
            Destroy(gameObject, destroyDelay);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        // Obje geri dönüyor!
        isBroken = false;
        if (visualModel != null) visualModel.SetActive(true);
        if (objectCollider != null) objectCollider.enabled = true;
    }

    // --- TEST ÝÇÝN GEÇÝCÝ KOD ---
    // Karakterin kýlýç sallama/vurma mekaniði henüz yoksa, þimdilik üstüne zýplayýnca veya çarpýnca kýrýlsýn.
    // Eðer Rigidbody ile çarpýþýrsa (Ýleride kullanýþlý olur)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) BreakIt();
    }

    // Eðer Trigger alanýna girerse (CharacterController için en garantisi)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) BreakIt();
    }
}