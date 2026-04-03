using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class UniversalPressurePlate : MonoBehaviour
{
    public enum PlateMode { Hold, TriggerOnce }
    public enum AllowedCharacter { Both, SanchoOnly, DonOnly } // --- YENİ EKLENDİ ---

    [Header("--- Çalışma Modu ---")]
    public PlateMode mode = PlateMode.Hold;

    [Header("--- Kimler Basabilir? (YENİ) ---")]
    [Tooltip("Bu plakayı hangi karakter tetikleyebilir? (Kutular her zaman tetikler)")]
    public AllowedCharacter allowedCharacter = AllowedCharacter.Both;

    [Header("--- Geçerli Tag'ler ---")]
    public List<string> validTags = new List<string> { "Player", "Pushable" };

    [Header("--- Bağlantılar (Unity Events) ---")]
    public UnityEvent onPress;
    public UnityEvent onRelease;

    [Header("--- Görsel Animasyon ---")]
    public Transform plateModel;
    public float pressDepth = 0.1f;
    public float pressSpeed = 8f;

    private Vector3 startPos;
    private Vector3 pressedPos;

    // --- AKILLI AĞIRLIK LİSTESİ ---
    private List<Collider> objectsOnPlate = new List<Collider>();
    private bool isPressed = false;
    private bool hasTriggered = false;

    void Start()
    {
        if (plateModel != null)
        {
            startPos = plateModel.localPosition;
            pressedPos = startPos - new Vector3(0, pressDepth, 0);
        }
    }

    void Update()
    {
        CleanUpList();

        if (plateModel != null)
        {
            Vector3 targetPos = isPressed ? pressedPos : startPos;
            plateModel.localPosition = Vector3.Lerp(plateModel.localPosition, targetPos, Time.deltaTime * pressSpeed);
        }
    }

    private void CleanUpList()
    {
        bool removedAny = false;
        for (int i = objectsOnPlate.Count - 1; i >= 0; i--)
        {
            if (objectsOnPlate[i] == null || !objectsOnPlate[i].gameObject.activeInHierarchy)
            {
                objectsOnPlate.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny && objectsOnPlate.Count == 0 && isPressed)
        {
            Release();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // YENİ: Artık sadece tag'e değil, karakterin kim olduğuna da bakıyoruz
        if (IsValidObject(other))
        {
            if (!objectsOnPlate.Contains(other))
            {
                objectsOnPlate.Add(other);
                if (!isPressed) Press();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsValidObject(other))
        {
            if (objectsOnPlate.Contains(other))
            {
                objectsOnPlate.Remove(other);
                if (objectsOnPlate.Count == 0 && isPressed)
                {
                    Release();
                }
            }
        }
    }

    // --- YENİ EKLENDİ: KARAKTER VE TAG KONTROL MERKEZİ ---
    private bool IsValidObject(Collider other)
    {
        // 1. Önce Tag kontrolü yapalım (Kutu veya Player mi?)
        bool hasValidTag = false;
        foreach (string validTag in validTags)
        {
            if (other.CompareTag(validTag))
            {
                hasValidTag = true;
                break;
            }
        }

        // Tag uymuyorsa direkt reddet
        if (!hasValidTag) return false;

        // 2. Karakter Kimlik Kontrolü (Eğer obje bir "Player" ise)
        if (other.CompareTag("Player"))
        {
            if (allowedCharacter == AllowedCharacter.SanchoOnly)
            {
                // Eğer gelen karakterin üstünde DonMovement varsa onu reddet!
                if (other.GetComponentInParent<DonMovement>() != null) return false;
            }
            else if (allowedCharacter == AllowedCharacter.DonOnly)
            {
                // Eğer gelen karakterin üstünde SanchoMovement varsa onu reddet!
                if (other.GetComponentInParent<SanchoMovement>() != null) return false;
            }
        }

        // Eğer buraya kadar geldiyse ya kutudur, ya da doğru karakterdir. İzin ver!
        return true;
    }

    private void Press()
    {
        isPressed = true;
        if (mode == PlateMode.TriggerOnce && hasTriggered) return;

        hasTriggered = true;
        if (onPress != null) onPress.Invoke();
        Debug.Log("<color=green>🔽 Plakaya Basıldı!</color>");
    }

    private void Release()
    {
        isPressed = false;
        if (mode == PlateMode.Hold)
        {
            if (onRelease != null) onRelease.Invoke();
            Debug.Log("<color=red>🔼 Plakadan İnildi!</color>");
        }
    }
}