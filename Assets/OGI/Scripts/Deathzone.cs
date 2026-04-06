using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Giren obje Player ise
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            Debug.Log("💀 Karakter boşluğa düştü! Anında infaz.");

            // Karakterlere 9999 hasar vererek kesin ölmelerini (ve Die fonksiyonunun çalışmasını) sağla
            if (other.TryGetComponent(out DonMovement don))
                don.TakeDamage(9999f);
            else if (other.TryGetComponent(out SanchoMovement sancho))
                sancho.TakeDamage(9999f);
            else
            {
                // Eğer modelin içinden bir parça değdiyse ana scripti bul
                var rootDon = other.GetComponentInParent<DonMovement>();
                if (rootDon != null) rootDon.TakeDamage(9999f);

                var rootSancho = other.GetComponentInParent<SanchoMovement>();
                if (rootSancho != null) rootSancho.TakeDamage(9999f);
            }
        }
    }
}