using UnityEngine;

public class ArrowItem : MonoBehaviour
{
    [Header("Ganimet (Ok) Miktarı")]
    [Tooltip("Yerden alınınca en az kaç ok versin?")]
    public int minArrowAmount = 3;
    [Tooltip("Yerden alınınca en fazla kaç ok versin?")]
    public int maxArrowAmount = 5;

    [Header("Görsel Süzülme Ayarları (Can Potu Stili)")]
    [Tooltip("Kendi etrafında dönme hızı")]
    public float rotationSpeed = 100f;
    [Tooltip("Aşağı yukarı süzülme hızı")]
    public float floatSpeed = 2f;
    [Tooltip("Aşağı yukarı süzülme yüksekliği")]
    public float floatHeight = 0.2f;

    private Vector3 startPos;

    void Start()
    {
        // Doğduğu anki orijinal pozisyonunu kaydet ki hep o hizada süzülsün kanka
        startPos = transform.position;
    }

    void Update()
    {
        // 1. Kendi etrafında dönme efekti
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // 2. Can potundaki gibi aşağı yukarı süzülme efekti (Kutudan düşüp havada havalı duracak)
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Eğer çarpan kişi Player ise (Sancho kontrolü)
        if (other.CompareTag("Player"))
        {
            var sanchoInventory = other.GetComponent<SanchoCombat>();

            if (sanchoInventory != null)
            {
                // 3, 4 veya 5 adet rastgele ok miktarı hesapla (+1 dahil etmek için)
                int randomArrowAmount = Random.Range(minArrowAmount, maxArrowAmount + 1);

                // Hesaplanan rastgele oku Sancho'ya ekle
                bool isPickedUp = sanchoInventory.AddArrows(randomArrowAmount);

                if (isPickedUp)
                {
                    Debug.Log($"🎯 Kutudan şansına {randomArrowAmount} adet ok çıktı ve başarıyla toplandı!");
                    Destroy(gameObject);
                }
            }
        }
    }
}