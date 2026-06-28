using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public Color activeColor = Color.green;
    private bool isActivated = false;

    [Header("Doğma (Spawn) Noktası Ayarları")]
    [Tooltip("Kutunun merkezinden ne kadar yukarıda doğsun?")]
    [SerializeField] private float yOffset = 1.5f;
    [Tooltip("Kutunun önünden (Z ekseni) ne kadar ileride doğsun? Negatif değer arkası yapar.")]
    [SerializeField] private float forwardOffset = 2.0f; 
    [Tooltip("Kutunun sağına/soluna (X ekseni) kaydırmak istersen kullanabilirsin.")]
    [SerializeField] private float rightOffset = 0.0f;

    [Header("Animasyon Ayarları")]
    [SerializeField] private Animator flagAnimator; 

    [Header("Ses Efekt Ayarları")]
    [SerializeField] private AudioClip saveSoundEffect; 
    [Range(0f, 1f)][SerializeField] private float soundVolume = 0.7f; 

    private void Start()
    {
        if (flagAnimator == null)
        {
            flagAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActivated && (other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
        {
            ActivateCheckpoint();
        }
    }

    public void ActivateCheckpoint()
    {
        isActivated = true;

        // --- YENİ SPAWN HESAPLAMA MANTIĞI ---
        // transform.forward -> Objelerin mavi okudur (önü). Objeyi sahnede döndürsen bile hep önüne göre hesaplar.
        // transform.right -> Objelerin kırmızı okudur (sağı).
        // transform.up -> Objelerin yeşil okudur (yukarısı).
        Vector3 spawnPoint = transform.position 
                             + (transform.up * yOffset) 
                             + (transform.forward * forwardOffset) 
                             + (transform.right * rightOffset);

        // Hesaplanan bu yeni noktayı Manager'a gönderiyoruz
        CheckpointManager.Instance.UpdateCheckpoint(spawnPoint, this);

        // --- ANIMASYONU TETİKLİYORUZ ---
        if (flagAnimator != null)
        {
            flagAnimator.SetBool("isActivated", true);
        }

        // --- SES EFEKTİNİ PATLATIYORUZ ---
        if (saveSoundEffect != null)
        {
            AudioSource.PlayClipAtPoint(saveSoundEffect, transform.position, soundVolume);
        }

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = activeColor;
    }

    public void DeactivateCheckpoint()
    {
        if (!isActivated) return;
        
        isActivated = false;

        if (flagAnimator != null)
        {
            flagAnimator.SetBool("isActivated", false);
        }

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = Color.white;
    }

    // 👁️ GİZLİ SİLAH: Sahnede doğma noktasını elle görmene yarar!
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 previewSpawnPoint = transform.position 
                                    + (transform.up * yOffset) 
                                    + (transform.forward * forwardOffset) 
                                    + (transform.right * rightOffset);
        
        // Doğma noktasını sahnede mavi bir küre olarak çizer
        Gizmos.DrawWireSphere(previewSpawnPoint, 0.5f);
        // Kutudan doğma noktasına bir çizgi çeker
        Gizmos.DrawLine(transform.position, previewSpawnPoint);
    }
}