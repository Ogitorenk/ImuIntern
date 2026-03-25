using UnityEngine;
using Cinemachine;

public class TpsCameraWallCollider : MonoBehaviour
{
    private CinemachineFreeLook freeLookCam;

    [Header("Duvar Ayarlarý")]
    public LayerMask collisionLayers; // Kameranýn çarpacaðý katmanlar (Default, Wall vb.)
    public float cameraRadius = 0.2f; // Kameranýn fiziksel geniþliði
    public float minDistance = 0.5f;  // Duvara çok girince karakterin ensesine ne kadar yaklaþsýn

    private float[] defaultRadius = new float[3];

    void Start()
    {
        freeLookCam = GetComponent<CinemachineFreeLook>();
        // Senin o Star Rail için ayarladýðýn 6, 4, 1.2 gibi deðerleri hafýzaya alýyoruz
        for (int i = 0; i < 3; i++)
        {
            defaultRadius[i] = freeLookCam.m_Orbits[i].m_Radius;
        }
    }

    void LateUpdate() // Kameradan sonra çalýþmasý için LateUpdate þart
    {
        // Karakterin konumundan kameranýn olmasý gereken yere bir ýþýn at
        Vector3 playerPos = freeLookCam.Follow.position + Vector3.up * 1.5f; // Göðüs hizasýndan bak

        for (int i = 0; i < 3; i++)
        {
            float targetDistance = defaultRadius[i];
            Vector3 camDir = (freeLookCam.State.FinalPosition - playerPos).normalized;

            RaycastHit hit;
            // Fiziksel olarak "Burada duvar var mý?" diye soruyoruz
            if (Physics.SphereCast(playerPos, cameraRadius, camDir, out hit, targetDistance, collisionLayers))
            {
                // Duvar varsa, kamerayý duvarýn 0.1 birim önüne çek
                freeLookCam.m_Orbits[i].m_Radius = Mathf.Clamp(hit.distance - 0.1f, minDistance, targetDistance);
            }
            else
            {
                // Duvar yoksa senin ayarladýðýn o güzel Star Rail mesafesine geri dön
                freeLookCam.m_Orbits[i].m_Radius = Mathf.Lerp(freeLookCam.m_Orbits[i].m_Radius, targetDistance, Time.deltaTime * 10f);
            }
        }
    }
}