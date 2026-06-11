using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public Color activeColor = Color.green;
    private bool isActivated = false;

    [Header("Ses Efekt Ayarları")]
    [SerializeField] private AudioClip saveSoundEffect; // Dinleme sesini buraya atacaksın kanka
    [Range(0f, 1f)][SerializeField] private float soundVolume = 0.7f; // Ses seviyesi kontrolü

    private void OnTriggerEnter(Collider other)
    {
        // Sadece Player tag'li objeler (veya Sancho/Don) girince çalışsın
        if (!isActivated && (other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
        {
            isActivated = true;

            // Manager'a "Benim pozisyonumu en son nokta yap" diyoruz
            // Y eksenini biraz yukarı alıyoruz ki karakter yerin dibinde doğmasın
            Vector3 spawnPoint = transform.position + Vector3.up * 1.5f;

            // Bu fonksiyon hem pozisyonu hem de Don/Sancho'nun can/iksir değerlerini tek seferde kaydedecek kanka!
            CheckpointManager.Instance.UpdateCheckpoint(spawnPoint);

            // --- SES EFEKTİNİ PATLATIYORUZ ---
            if (saveSoundEffect != null)
            {
                AudioSource.PlayClipAtPoint(saveSoundEffect, transform.position, soundVolume);
            }

            GetComponent<Renderer>().material.color = activeColor;
        }
    }
}