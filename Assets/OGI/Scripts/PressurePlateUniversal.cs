using UnityEngine;
using UnityEngine.Events; // Kritik satır: Farklı scriptleri tetiklemek için şart

public class PressurePlateUniversal : MonoBehaviour
{
    [Header("Tetiklenecek Olaylar")]
    [Tooltip("Plakaya basıldığında çalışacak her şeyi buraya ekle (Kütük, Bıçak, Kapı vb.)")]
    public UnityEvent onActivate;

    private bool isPressed = false;
    private Vector3 originalPos;

    void Start()
    {
        originalPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Eğer basan Player ise ve daha önce basılmadıysa
        if (!isPressed && other.CompareTag("Player"))
        {
            isPressed = true;

            // --- EVRENSEL TETİKLEME ---
            // Inspector'da listeye eklediğin her şeyi tek seferde çalıştırır
            if (onActivate != null)
            {
                onActivate.Invoke();
            }

            // Görsel efekt: Plaka aşağı çöker
            transform.position = originalPos - new Vector3(0, 0.1f, 0);

            Debug.Log("<color=yellow>⚠️ PLAKA TETİKLENDİ!</color> Bağlı mekanizmalar çalışıyor.");
        }
    }

    // İstersen plakadan inince eski haline gelmesi için Reset fonksiyonu da ekleyebiliriz ama 
    // şimdilik tek seferlik (One-shot) sistemin devam ediyor.
}