using UnityEngine;

public class SlowPotionPickup : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public float rotationSpeed = 100f;
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Havalý süzülme ve dönme efekti
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don Kiþot mu aldý? (Parent kontrolü dahil)
        DonMovement don = other.GetComponent<DonMovement>();
        if (don == null) don = other.GetComponentInParent<DonMovement>();

        if (don != null)
        {
            don.slowPotionCount++;
            Debug.Log("<color=cyan>? Don Kiþot Zaman Ýksiri Aldý! Toplam: " + don.slowPotionCount + "</color>");
            Destroy(gameObject);
            return;
        }

        // Sancho mu aldý?
        SanchoMovement sancho = other.GetComponent<SanchoMovement>();
        if (sancho == null) sancho = other.GetComponentInParent<SanchoMovement>();

        if (sancho != null)
        {
            sancho.slowPotionCount++;
            Debug.Log("<color=cyan>? Sancho Zaman Ýksiri Aldý! Toplam: " + sancho.slowPotionCount + "</color>");
            Destroy(gameObject);
            return;
        }
    }
}