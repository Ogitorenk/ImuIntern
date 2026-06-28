using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Giren objenin kendisinde veya ebeveynlerinde "Player" tag'i var mı diye bak kanka
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            Debug.Log("<color=black>💀💀💀 DEATHZONE: Karakter boşluğa düştü! İnfaz işlemi başlatılıyor... 💀💀💀</color>");

            // 2. EN GARANTİ YOL: Objeden bağımsız olarak projede kurduğumuz IDamageable arayüzünü ara kanka
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) damageable = other.GetComponentInChildren<IDamageable>();

            if (damageable != null)
            {
                // Karakteri kesin olarak öldürmek için tek hamlede infaz hasarı çakıyoruz
                damageable.TakeDamage(9999f);
                return;
            }

            // 3. EĞER INTERFACE YAKALAYAMAZSAK (GÜVENLİK DUVARI) - Scriptlerden direkt zorla kanka
            var don = other.GetComponentInParent<DonMovement>();
            if (don != null)
            {
                // DonMovement içindeki gerçek hasar/ölüm fonksiyonunu çağır kanka
                don.TakeDamage(9999f);
                return;
            }

            var sancho = other.GetComponentInParent<SanchoMovement>();
            if (sancho != null)
            {
                sancho.TakeDamage(9999f);
                return;
            }
        }
    }
}