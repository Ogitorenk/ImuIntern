using UnityEngine;

public class StaticSpike : MonoBehaviour
{
    [Tooltip("Karakter bu dikene deðdiðinde kaç caný gitsin?")]
    public float damage = 20f;

    // Karakter dikene ilk deðdiði an çalýþýr
    private void OnTriggerEnter(Collider other)
    {
        DealDamage(other);
    }

    // Karakter dikenin üstünde beklemeye devam ettiði sürece çalýþýr
    private void OnTriggerStay(Collider other)
    {
        DealDamage(other);
    }

    // Hasar verme iþlemini yapan ortak fonksiyon
    private void DealDamage(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Eðer deðen kiþi Don Kiþot ise
            if (other.TryGetComponent(out DonMovement don))
            {
                don.TakeDamage(damage);
            }
            // Eðer deðen kiþi Sancho ise
            else if (other.TryGetComponent(out SanchoMovement sancho))
            {
                sancho.TakeDamage(damage);
            }
        }
    }
}