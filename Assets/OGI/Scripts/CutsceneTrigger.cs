using UnityEngine;
using UnityEngine.Playables;
using System.Reflection;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("--- TIMELINE AYARLARI ---")]
    [SerializeField] private PlayableDirector cutsceneDirector;

    [Header("--- TETİKLEME AYARLARI ---")]
    [SerializeField] private bool playAutomaticallyOnStart = false;

    [Header("--- KALICI HAFIZA AYARLARI ---")]
    [SerializeField] private string cutsceneID;

    private bool hasTriggered = false;
    private MonoBehaviour currentPlayerScript = null;

    private void Awake()
    {
        // Hafıza kontrolü
        if (!string.IsNullOrEmpty(cutsceneID) && PlayerPrefs.GetInt(cutsceneID, 0) == 1)
        {
            hasTriggered = true;
        }

        if (cutsceneDirector != null)
        {
            cutsceneDirector.playOnAwake = false; // Kendi kendine başlamasın
            cutsceneDirector.stopped += OnCutsceneFinished;

            // ========================================================
            // --- YENİ DÜZELTME: İLK KARE KİLİTLENMESİNİ ENGELLEME ---
            // ========================================================
            if (!hasTriggered && !playAutomaticallyOnStart)
            {
                // Timeline'ı 0'a çek, zamanı durdur ve sahnedeki etkisini sıfırla
                cutsceneDirector.time = 0;
                cutsceneDirector.Stop(); 
            }
        }
    }

    private void Start()
    {
        if (hasTriggered)
        {
            // Eğer daha önce oynandıysa sahnede kalıcı hasar bırakma, son haline sar
            FastForwardToEnd();
            return;
        }

        if (playAutomaticallyOnStart)
        {
            FindPlayerInScene();
            if (currentPlayerScript != null)
            {
                TriggerCutscene();
            }
        }
    }

    private void FastForwardToEnd()
    {
        if (cutsceneDirector != null)
        {
            cutsceneDirector.time = cutsceneDirector.duration;
            cutsceneDirector.Evaluate(); 
            
            FindPlayerInScene();
            SetPlayerControl(true);
            
            Debug.Log($"<color=green>✔ {cutsceneID} atlandı ve sahne son haline getirildi.</color>");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || playAutomaticallyOnStart) return;

        if (other.CompareTag("Player"))
        {
            currentPlayerScript = other.GetComponent<DonMovement>();
            if (currentPlayerScript == null) currentPlayerScript = other.GetComponent<SanchoMovement>();
            TriggerCutscene();
        }
    }

    private void TriggerCutscene()
    {
        hasTriggered = true;
        if (!string.IsNullOrEmpty(cutsceneID))
        {
            PlayerPrefs.SetInt(cutsceneID, 1);
            PlayerPrefs.Save();
        }
        StartCutscene();
    }

    private void StartCutscene()
    {
        SetPlayerControl(false);
        if (cutsceneDirector != null) 
        {
            cutsceneDirector.Play();
        }
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        if (director == cutsceneDirector)
        {
            SetPlayerControl(true);
        }
    }

    private void FindPlayerInScene()
    {
        var don = Object.FindFirstObjectByType<DonMovement>();
        if (don != null) currentPlayerScript = don;
        else
        {
            var sancho = Object.FindFirstObjectByType<SanchoMovement>();
            if (sancho != null) currentPlayerScript = sancho;
        }
    }

    private void SetPlayerControl(bool canControl)
    {
        if (currentPlayerScript == null) return;
        try
        {
            System.Type type = currentPlayerScript.GetType();
            FieldInfo field = type.GetField("isControlled");
            if (field != null) field.SetValue(currentPlayerScript, canControl);

            Animator anim = currentPlayerScript.GetComponentInChildren<Animator>();
            if (anim != null && !canControl) anim.SetFloat("Speed", 0f);
        }
        catch (System.Exception e) { Debug.LogError("Kontrol hatası: " + e.Message); }
    }

    [ContextMenu("Hafızayı Sıfırla (Sadece bu ID)")]
    public void ResetThisID()
    {
        PlayerPrefs.DeleteKey(cutsceneID);
        Debug.Log($"Hafıza Silindi: {cutsceneID}.");
    }

    public void TriggerFromExternalScript()
    {
        if (hasTriggered) return;

        hasTriggered = true;

        if (!string.IsNullOrEmpty(cutsceneID))
        {
            PlayerPrefs.SetInt(cutsceneID, 1);
            PlayerPrefs.Save();
        }

        FindPlayerInScene();
        StartCutscene();
    }
}