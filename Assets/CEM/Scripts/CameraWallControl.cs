using UnityEngine;

public class CameraWallControl : MonoBehaviour
{
    [Header("Hedef Tanımlamaları")]
    public Transform karakter;          // Takip edilen karakter (Don / Sancho)
    
    [Header("Çarpışma Ayarları")]
    public LayerMask engelKatmanlari;   // Duvar ve zeminlerin olduğu katman (Ground, Wall)
    public float kameraYaricapi = 0.2f; // Kameranın etrafındaki hayali koruma alanı
    public float yumusamaHizi = 10f;    // Duvara çarpma ve çıkma anındaki akıcılık hızı

    private Vector3 varsayilanOfset;
    private float maksimumMesafe;

    void Start()
    {
        if (karakter == null) return;
        
        // Oyun başındaki o çok beğendiğin ideal kamera mesafesini kaydeder
        varsayilanOfset = transform.position - karakter.position;
        maksimumMesafe = varsayilanOfset.magnitude;
    }

    void LateUpdate()
    {
        if (karakter == null) return;

        // Kameranın gitmek istediği normal (orijinal) pozisyon
        Vector3 hedefKameraPozisyonu = karakter.position + (transform.rotation * varsayilanOfset.normalized * maksimumMesafe);
        
        // Karakterden kameraya doğru bir ışın (Ray) gönderiyoruz
        Vector3 rayYonu = hedefKameraPozisyonu - karakter.position;
        RaycastHit hit;

        float anlikMesafe = maksimumMesafe;

        // Karakter ile kamera arasında seçtiğin katmanlarda bir engel var mı?
        if (Physics.SphereCast(karakter.position, kameraYaricapi, rayYonu.normalized, out hit, maksimumMesafe, engelKatmanlari))
        {
            // Eğer engel varsa, kamerayı engelin vurduğu noktaya çek (biraz pay bırakarak)
            anlikMesafe = Mathf.Clamp(hit.distance - kameraYaricapi, 0.1f, maksimumMesafe);
        }

        // Kameranın yeni pozisyonunu hesapla
        Vector3 yeniPozisyon = karakter.position + (rayYonu.normalized * anlikMesafe);

        // Şak diye ışınlanmak yerine, Lerp ile yumuşacık geçiş yapmasını sağlıyoruz
        transform.position = Vector3.Lerp(transform.position, yeniPozisyon, Time.deltaTime * yumusamaHizi);
    }
}