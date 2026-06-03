using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("Kalkan Ayarları")]
    [Tooltip("Kalkan aktifken gelen hasarın yüzde kaçını engellesin? (1 = %100 engeller, 0.5 = yarısını engeller)")]
    [Range(0f, 1f)]
    public float damageBlockPercentage = 1f;

    [Header("Görsel Durum")]
    [Tooltip("Kalkanın şu an aktif (blokta) olup olmadığını koda bildirir kanka.")]
    public bool isShieldActive = false;

    // Oyuncu kalkan butonuna bastığında animasyonla birlikte bu fonksiyonu çağıracağız kanka
    public void SetShieldStatus(bool isActive)
    {
        isShieldActive = isActive;
    }

    // Hasar geldiğinde bu fonksiyonu çağırıp filtrelenmiş (kalkanlanmış) hasarı geri döneceğiz
    public float BlockDamage(float incomingDamage)
    {
        if (!isShieldActive) return incomingDamage; // Kalkan kapalıysa hasarı aynen ye aq

        // Kalkan açıksa hasarı hesapla
        float blockedAmount = incomingDamage * damageBlockPercentage;
        float finalDamage = incomingDamage - blockedAmount;

        Debug.Log($"🛡️ KALKAN BLOKLADI! Gelen Hasar: {incomingDamage} | Engellenen: {blockedAmount} | Oyuncunun Yediği: {finalDamage}");

        // Buraya istersen kalkan vurulma efekti veya çıtır bir ses kodu ekleyebilirsin kanka

        return finalDamage;
    }
}