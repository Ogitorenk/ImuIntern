using UnityEngine;

public class MovingColliders : MonoBehaviour
{
    [Header("Dönüþ Ayarlarý")]
    public float rotationSpeed = 60f; // Dönüþ hýzý (Normal platform için 30-60 idealdir)
    public bool clockwise = true;     // Saat yönünde mi dönsün?

    void Update()
    {
        // Platformu kendi ekseninde (Y ekseni) yað gibi döndür
        float direction = clockwise ? 1f : -1f;
        transform.Rotate(0, rotationSpeed * direction * Time.deltaTime, 0);
    }
}