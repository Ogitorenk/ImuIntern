using UnityEngine;
using UnityEngine.Playables; // Timeline (PlayableDirector) kontrolü için şart kanka!
using System.Reflection;     // isControlled kilidini tetiklemek için

public class CutsceneTrigger : MonoBehaviour
{
    [Header("--- TIMELINE AYARLARI ---")]
    [Tooltip("Sahnede içinde kliplerin dizili olduğu PlayableDirector (CutsceneManager objesi)")]
    [SerializeField] private PlayableDirector cutsceneDirector;

    [Header("--- KALICI HAFIZA AYARLARI ---")]
    [Tooltip("Her sinematik için buraya benzersiz bir isim ver kanka (Örn: CastleEntrance_Intro)")]
    [SerializeField] private string cutsceneID;

    private bool hasTriggered = false;
    private MonoBehaviour currentPlayerScript = null; // İçeri giren karakteri tutar

    private void Awake()
    {
        // Oyun açıldığında, bu sinematiğin kalıcı olarak oynatılıp oynatılmadığını kontrol ediyoruz
        if (!string.IsNullOrEmpty(cutsceneID) && PlayerPrefs.GetInt(cutsceneID, 0) == 1)
        {
            hasTriggered = true; // Eğer 1 ise daha önce oynatılmıştır, yerel kilidi de kapatıyoruz
        }

        if (cutsceneDirector != null)
        {
            // PlayableDirector'ın kendi kendine başlamasını engelliyoruz, kontrol tamamen bizde
            cutsceneDirector.playOnAwake = false;
            
            // Timeline bittiğinde tetiklenecek fonksiyonu sisteme kaydediyoruz (Event Subscription)
            cutsceneDirector.stopped += OnCutsceneFinished;
        }
    }

    private void OnDestroy()
    {
        // Hafıza sızıntısı (Memory Leak) olmaması için sahne kapanırken event kaydını siliyoruz
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnCutsceneFinished;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Diskten gelen bilgiye veya yerel kontrole göre zaten oynatıldıysa hiç başlama kanka
        if (hasTriggered) return;

        // Alana giren oyuncuysa ve ara sahne henüz oynatılmadıysa başlat kanka
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // Eğer bir ID girildiyse, diske "Bu sinematik bir kere oynandı" olarak kaydediyoruz
            if (!string.IsNullOrEmpty(cutsceneID))
            {
                PlayerPrefs.SetInt(cutsceneID, 1);
                PlayerPrefs.Save(); // Değişiklikleri diske pürüzsüzce yaz
            }

            // İçeri giren Don mu Sancho mu hemen yakala
            var don = other.GetComponent<DonMovement>();
            if (don != null) currentPlayerScript = don;
            else
            {
                var sancho = other.GetComponent<SanchoMovement>();
                if (sancho != null) currentPlayerScript = sancho;
            }

            StartCutscene();
        }
    }

    private void StartCutscene()
    {
        Debug.Log("<color=orange>🎬 ARA SAHNE BAŞLADI: Karakter donduruldu, Timeline oynatılıyor!</color>");

        // 1. Oyuncunun hareketini reflection ile kilitle kanka
        SetPlayerControl(false);

        // 2. Timeline'ı (CutsceneManager üzerindeki director'ı) başlat
        if (cutsceneDirector != null)
        {
            cutsceneDirector.Play();
        }
        else
        {
            Debug.LogError("🚨 CutsceneDirector bulunamadı! Güvenlik için kontrol açılıyor.");
            SetPlayerControl(true);
        }
    }

    // Timeline bittiği an Unity bu fonksiyonu otomatik olarak tetikler kanka
    private void OnCutsceneFinished(PlayableDirector director)
    {
        if (director == cutsceneDirector)
        {
            Debug.Log("<color=green>🎬 ARA SAHNE BİTTİ: Kontrol oyuncuya geri devredildi!</color>");
            
            // 3. Kontrolü oyuncuya geri veriyoruz
            SetPlayerControl(true);
        }
    }

    private void SetPlayerControl(bool canControl)
    {
        if (currentPlayerScript == null) return;

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

    // Editörde tetikleyici kutuyu magenta renkli tel kafes olarak görelim
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