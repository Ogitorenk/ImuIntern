using UnityEngine;

public class OneTimeElevatorMusic : MonoBehaviour
{
    [Header("Bileşenler")]
    [Tooltip("Çalınacak asansör müziğinin bağlı olduğu AudioSource")]
    [SerializeField] private AudioSource musicSource;

    private bool hasPlayed = false;

    /// <summary>
    /// Bu fonksiyonu Şalterin onActivate eventine bağlayacağız.
    /// </summary>
    public void PlayElevatorMusic()
    {
        // Eğer müzik zaten bir kere çaldıysa veya AudioSource atanmadıysa çalıştırma
        if (hasPlayed || musicSource == null) return;

        musicSource.Play();
        hasPlayed = true; // Bir daha çalınmasını engelle
        
        Debug.Log("<color=green>🎵 Asansör tamir edildi ve ilk kez çalıştı! Müzik başlıyor...</color>");
    }

    /// <summary>
    /// (İsteğe Bağlı) Asansör durduğunda müziği kapatmak istersen kullanabilirsin.
    /// </summary>
    public void StopElevatorMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("<color=red>⏹️ Asansör müziği durduruldu.</color>");
        }
    }
}