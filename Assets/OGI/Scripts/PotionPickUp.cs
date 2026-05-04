using UnityEngine;

public class PotionPickup : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public float rotationSpeed = 100f; // Kendi etrafýnda dönme hýzý
    public float floatSpeed = 2f;      // Süzülme hýzý
    public float floatHeight = 0.2f;   // Süzülme yüksekliði

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Kendi etrafýnda dönme efekti
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Aþaðý yukarý süzülme efekti (Havalý dursun diye)
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don Kiþot mu aldý?
        DonMovement don = other.GetComponent<DonMovement>();
        if (don != null)
        {
            don.healthPotionCount++;
            Debug.Log("<color=green>?? Don Kiþot Can Ýksiri Aldý! Toplam: " + don.healthPotionCount + "</color>");
            Destroy(gameObject);
            return;
        }

        // Sancho mu aldý?
        SanchoMovement sancho = other.GetComponent<SanchoMovement>();
        if (sancho != null)
        {
            sancho.healthPotionCount++;
            Debug.Log("<color=green>?? Sancho Can Ýksiri Aldý! Toplam: " + sancho.healthPotionCount + "</color>");
            Destroy(gameObject);
            return;
        }
    }
}