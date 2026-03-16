using UnityEngine;
using Cinemachine; // Cinemachine kütüphanesini kullanmak için þart

[RequireComponent(typeof(CinemachineFreeLook))]
public class TpsCameraZoom : MonoBehaviour
{
    [Header("Zoom Ayarlarý")]
    public float zoomSpeed = 5f; // Scroll hassasiyeti
    public float minRadius = 1.5f; // Karaktere en fazla ne kadar yaklaþsýn
    public float maxRadius = 10f; // Karakterden en fazla ne kadar uzaklaþsýn

    private CinemachineFreeLook freeLookCam;
    private float currentRadius;

    void Start()
    {
        freeLookCam = GetComponent<CinemachineFreeLook>();

        // Oyun baþladýðýnda kameranýn o anki orta çember uzaklýðýný referans al
        currentRadius = freeLookCam.m_Orbits[1].m_Radius;
    }

    void Update()
    {
        // Scroll tekerleðinin hareketini oku
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            // Scroll ileri itilirse pozitif, geri çekilirse negatif deðer verir.
            // Yakýnlaþmak için radius'u küçültmemiz gerektiðinden çýkartma yapýyoruz.
            currentRadius -= scroll * zoomSpeed;

            // Deðerin minimum ve maksimum sýnýrlar dýþýna çýkmasýný engelle
            currentRadius = Mathf.Clamp(currentRadius, minRadius, maxRadius);

            // KRÝTÝK NOKTA: 3 çemberin (Top, Middle, Bottom) uzaklýðýný ayný anda güncelle
            // Bu sayede fareyi aþaðý yukarý yaptýðýnda zoom bozulmaz
            freeLookCam.m_Orbits[0].m_Radius = currentRadius;
            freeLookCam.m_Orbits[1].m_Radius = currentRadius;
            freeLookCam.m_Orbits[2].m_Radius = currentRadius;
        }
    }
}