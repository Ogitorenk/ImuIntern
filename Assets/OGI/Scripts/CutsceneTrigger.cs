using UnityEngine;
using Cinemachine; // Cinemachine bileşenlerini kontrol etmek için şart kanka!
using System.Collections;
using System.Reflection; // isControlled kilidini tetiklemek için

public class CutsceneTrigger : MonoBehaviour
{
    [Header("--- CINEMACHINE KAMERA AYARLARI ---")]
    [Tooltip("Sahnede gezinmesini istediğin o Dolly Camera (Virtual Camera)")]
    public CinemachineVirtualCamera cutsceneCamera;

    [Tooltip("Dolly kameranın bağlı olduğu ray hattı bileşeni (Cinemachine Smooth Path)")]
    public CinemachineSmoothPath smoothPath;

    [Header("--- SİNEMATİK AYARLARI ---")]
    [Tooltip("Kameranın ray üzerindeki hareket hızı (Yüksek değer = daha hızlı kamera hareketi)")]
    public float cameraSpeed = 2f;

    [Tooltip("Kamera rayın sonuna gelse bile ara sahne toplam kaç saniye sürsün? (Güvenlik süresi)")]
    public float maxCutsceneDuration = 5f;

    private bool hasTriggered = false;
    private CinemachineTrackedDolly dollyComponent;
    private MonoBehaviour currentPlayerScript = null; // İçeri giren karakteri tutar

    void Start()
    {
        // Oyun başında ara sahne kamerasının önceliğini (Priority) sıfır yapıyoruz ki oyun kamerası aktif kalsın
        if (cutsceneCamera != null)
        {
            cutsceneCamera.Priority = 0;

            // Kameranın içindeki Dolly bileşenine ulaşıyoruz kanka
            dollyComponent = cutsceneCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
            if (dollyComponent != null)
            {
                dollyComponent.m_PathPosition = 0f; // Rayın başına koy
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Alana giren oyuncuysa ve ara sahne henüz oynatılmadıysa başlat kanka
        if (!hasTriggered && other.CompareTag("Player"))
        {
            // İçeri giren Don mu Sancho mu hemen yakala
            var don = other.GetComponent<DonMovement>();
            if (don != null) currentPlayerScript = don;
            else
            {
                var sancho = other.GetComponent<SanchoMovement>();
                if (sancho != null) currentPlayerScript = sancho;
            }

            StartCoroutine(PlayCutsceneRoutine());
        }
    }

    private IEnumerator PlayCutsceneRoutine()
    {
        hasTriggered = true;
        Debug.Log("<color=orange>🎬 ARA SAHNE BAŞLADI: Karakter donduruldu, kamera harekete geçiyor!</color>");

        // 1. Karakteri tamamen dondur, asla kıpırdayamasın
        SetPlayerControl(false);

        if (cutsceneCamera != null && dollyComponent != null && smoothPath != null)
        {
            // 2. Sinematik kameranın önceliğini arttırarak ana kamera yapıyoruz (Pürüzsüz geçiş sağlar)
            cutsceneCamera.Priority = 20;

            float currentPathPos = 0f;
            float maxPathPos = smoothPath.MaxPos; // Rayın son noktasını otomatik bul kanka
            float timer = 0f;

            // Kamera rayın sonuna gelene kadar veya maksimum süre dolana kadar kamerayı yürüt
            while (currentPathPos < maxPathPos && timer < maxCutsceneDuration)
            {
                // Kamerayı ray üzerinde kaydırıyoruz
                currentPathPos += cameraSpeed * Time.deltaTime;
                dollyComponent.m_PathPosition = currentPathPos;

                timer += Time.deltaTime;
                yield return null; // Bir sonraki kareyi bekle kanka
            }
        }
        else
        {
            // Eğer kameralar bağlanmadıysa oyun çökmesin diye güvenlik olarak süreyi bekle kanka
            yield return new WaitForSeconds(maxCutsceneDuration);
        }

        // 3. Ara sahne bitti! Sinematik kamerayı kapatıp kontrolü oyuncuya geri veriyoruz
        EndCutscene();
    }

    private void EndCutscene()
    {
        Debug.Log("<color=green>🎬 ARA SAHNE BİTTİ: Kontrol oyuncuya geri devredildi!</color>");

        if (cutsceneCamera != null)
        {
            cutsceneCamera.Priority = 0; // Sinematik kamerayı devreden çıkar, oyun kamerası geri gelsin
        }

        // Karakterin hareket kilidini aç, koşmaya devam etsin kanka!
        SetPlayerControl(true);
    }

    private void SetPlayerControl(bool canControl)
    {
        if (currentPlayerScript == null) return;

        // Reflection kullanarak Don veya Sancho'nun isControlled kilidini pürüzsüzce yönetiyoruz
        try
        {
            System.Type type = currentPlayerScript.GetType();
            FieldInfo field = type.GetField("isControlled");

            if (field != null)
            {
                field.SetValue(currentPlayerScript, canControl);
            }

            // Karakter donduğunda animasyonda asılı kalmasın, hızı sıfırlansın
            Animator anim = currentPlayerScript.GetComponentInChildren<Animator>();
            if (anim != null && !canControl)
            {
                anim.SetFloat("Speed", 0f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("🚨 Ara sahnede oyuncu kilitlenirken hata oluştu kanka: " + e.Message);
        }
    }

    // Editörde tetikleyici kutuyu yeşil tel kafes olarak görelim
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}