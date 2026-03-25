using UnityEngine;

public class IllusionJumpPad : MonoBehaviour
{
    [Header("Sıçrama Ayarları")]
    [Tooltip("Karakteri ne kadar yükseğe fırlatacak?")]
    public float bounceHeight = 8f;

    [Header("Görseller (Modeller)")]
    public GameObject mushroomModel; // Don Kişot'un göreceği mantar modeli
    public GameObject jumpPadModel;  // Sancho'nun göreceği trambolin modeli

    void Start()
    {
        // Başlangıçta Don Kişot'un aktif olduğunu varsayarak mantarı açıyoruz.
        // Eğer oyun Sancho ile başlıyorsa burayı false yapabilirsin.
        UpdatePerception(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Trigger'a giren Player tagine sahipse
        if (other.CompareTag("Player"))
        {
            // 1. İhtimal: Çarpan kişi Don Kişot mu?
            DonMovement don = other.GetComponent<DonMovement>();
            if (don != null)
            {
                don.ExternalJump(bounceHeight);
                return; // Don zıpladıysa Sancho'yu kontrol etmeye gerek yok, fonksiyondan çık
            }

            // 2. İhtimal: Çarpan kişi Sancho mu?
            SanchoMovement sancho = other.GetComponent<SanchoMovement>();
            if (sancho != null)
            {
                sancho.ExternalJump(bounceHeight);
            }
        }
    }

    // Karakter değiştiğinde modelleri değiştirecek fonksiyon
    public void UpdatePerception(bool isDonQuixoteActive)
    {
        if (mushroomModel != null) mushroomModel.SetActive(isDonQuixoteActive);
        if (jumpPadModel != null) jumpPadModel.SetActive(!isDonQuixoteActive);
    }
    
}