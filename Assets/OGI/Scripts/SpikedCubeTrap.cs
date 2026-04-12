using UnityEngine;

public class SpikedCubeTrap : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    [Tooltip("Küpün içindeki Nokta A objesini sürükle")]
    public Transform pointA;
    [Tooltip("Küpün içindeki Nokta B objesini sürükle")]
    public Transform pointB;
    public float speed = 5f;

    [Header("Hasar Ayarlarý")]
    public float damageAmount = 25f;

    // Hedeflerin sabit dünya koordinatlarý
    private Vector3 targetPosA;
    private Vector3 targetPosB;
    private Vector3 currentTarget;

    void Start()
    {
        if (pointA != null && pointB != null)
        {
            // 1. Oyun baþladýðý an A ve B'nin dünya koordinatlarýný hafýzaya al
            targetPosA = pointA.position;
            targetPosB = pointB.position;

            // 2. Child objeleri Parent'tan kopar ki küp hareket edince onlar da peþinden sürüklenmesin!
            pointA.SetParent(null);
            pointB.SetParent(null);

            // Baþlangýç noktasýný A olarak belirliyoruz ve ilk hedef B oluyor
            transform.position = targetPosA;
            currentTarget = targetPosB;
        }
        else
        {
            Debug.LogError("Kanka A veya B noktasýný Inspector'da boþ býraktýn!");
        }
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        // Küpü hedefe doðru yumuþakça hareket ettir
        transform.position = Vector3.MoveTowards(transform.position, currentTarget, speed * Time.deltaTime);

        // Eðer hedefe ulaþtýysak, diðerine dön
        if (Vector3.Distance(transform.position, currentTarget) < 0.1f)
        {
            if (currentTarget == targetPosA)
            {
                currentTarget = targetPosB;
            }
            else
            {
                currentTarget = targetPosA;
            }
        }
    }

    // --- HASAR VERME MEKANÝÐÝ ---
    private void OnTriggerEnter(Collider other)
    {
        // Küpün BoxCollider'ýnda "Is Trigger" AÇIK olmalý!

        // 1. Sancho'ya mý çarptýk?
        SanchoMovement sancho = other.GetComponent<SanchoMovement>();
        if (sancho != null)
        {
            sancho.TakeDamage(damageAmount);
            return; // Çarptýysak çýk, Don'u aramaya gerek yok
        }

        // 2. Don Kiþot'a mý çarptýk?
        DonMovement don = other.GetComponent<DonMovement>();
        if (don != null)
        {
            don.TakeDamage(damageAmount);
        }
    }
}