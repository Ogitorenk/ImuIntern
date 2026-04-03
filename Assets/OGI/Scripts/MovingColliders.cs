using UnityEngine;

public class MovingColliders : MonoBehaviour
{
    // Platformun nasıl çalışacağını seçmemizi sağlayan menü
    public enum RotationType { Continuous, ExternalTrigger }

    [Header("--- Çalışma Modu ---")]
    public RotationType rotationType = RotationType.Continuous;

    [Header("--- Dönüş Ayarları ---")]
    public float rotationSpeed = 60f;
    public bool clockwise = true;

    // Şalter veya Plaka tarafından kontrol edilen değişken
    private bool isActivated = false;

    void Update()
    {
        // HAREKET ŞARTI: Ya hep dönüyordur (Continuous) ya da dışarıdan tetiklenmiştir (ExternalTrigger)
        bool shouldRotate = (rotationType == RotationType.Continuous) ||
                            (rotationType == RotationType.ExternalTrigger && isActivated);

        if (shouldRotate)
        {
            // Platformu kendi ekseninde (Y ekseni) yağ gibi döndür
            float direction = clockwise ? 1f : -1f;
            transform.Rotate(0, rotationSpeed * direction * Time.deltaTime, 0);
        }
    }

    // ==========================================
    // --- ŞALTER VE PLAKA İÇİN TETİKLEYİCİLER ---
    // ==========================================

    // Şalter çekildiğinde veya Plakaya basıldığında çağrılır
    public void ActivatePlatform()
    {
        isActivated = true;
        Debug.Log("<color=green>⚙️ Pervane çalışmaya başladı!</color>");
    }

    // Şalter kapandığında veya Plakadan inildiğinde çağrılır
    public void DeactivatePlatform()
    {
        isActivated = false;
        Debug.Log("<color=red>⚙️ Pervane durduruldu.</color>");
    }
}