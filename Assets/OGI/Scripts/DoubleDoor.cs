using UnityEngine;
using System.Collections;

public class DoubleDoor : MonoBehaviour
{
    [Header("Menteşeler")]
    public Transform leftHinge;
    public Transform rightHinge;

    [Header("Ayarlar")]
    public float openAngle = 90f; // Bize doğru açılması için genelde 90 veya -90
    public float openSpeed = 2f;

    // --- YENİ: ETKİLEŞİM AYARLARI ---
    [Header("Etkileşim")]
    public bool canBeOpenedDirectly = true; // Kapı doğrudan F ile açılabilsin mi? (Şalterle açılacaksa tikini kaldır)
    public KeyCode interactKey = KeyCode.F; // Açma tuşu
    private bool playerInRange = false;     // Oyuncu kapının dibinde mi?

    private bool isOpen = false;
    private Quaternion leftClosedRot;
    private Quaternion rightClosedRot;
    private Quaternion leftOpenRot;
    private Quaternion rightOpenRot;

    void Start()
    {
        // Başlangıç rotasyonlarını kaydet
        leftClosedRot = leftHinge.localRotation;
        rightClosedRot = rightHinge.localRotation;

        // Hedef rotasyonları hesapla 
        leftOpenRot = Quaternion.Euler(0, -openAngle, 0);
        rightOpenRot = Quaternion.Euler(0, openAngle, 0);
    }

    // --- YENİ: KULAK (F TUŞUNU DİNLER) ---
    void Update()
    {
        // Oyuncu menzildeyse, doğrudan açılmaya izni varsa ve F'ye bastıysa
        if (canBeOpenedDirectly && playerInRange && Input.GetKeyDown(interactKey))
        {
            if (!isOpen)
            {
                OpenDoors();
            }
            else
            {
                CloseDoors(); // İstersen ikinci basışta kapatır
            }
        }
    }

    // Bu fonksiyonu Şalter (Lever) veya Plaka'daki UnityEvent'e bağla!
    public void OpenDoors()
    {
        if (!isOpen)
        {
            StopAllCoroutines();
            StartCoroutine(MoveDoors(leftOpenRot, rightOpenRot));
            isOpen = true;
            Debug.Log("<color=cyan>🚪 Kapılar açılıyor...</color>");
        }
    }

    // Kapıyı kapatmak istersen bunu da kullanabilirsin
    public void CloseDoors()
    {
        if (isOpen)
        {
            StopAllCoroutines();
            StartCoroutine(MoveDoors(leftClosedRot, rightClosedRot));
            isOpen = false;
            Debug.Log("<color=cyan>🚪 Kapılar kapanıyor...</color>");
        }
    }

    IEnumerator MoveDoors(Quaternion targetLeft, Quaternion targetRight)
    {
        float elapsed = 0;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            leftHinge.localRotation = Quaternion.Slerp(leftHinge.localRotation, targetLeft, elapsed);
            rightHinge.localRotation = Quaternion.Slerp(rightHinge.localRotation, targetRight, elapsed);
            yield return null;
        }
    }

    // --- YENİ: GÖZLER (OYUNCUYU GÖRÜR) ---
    private void OnTriggerEnter(Collider other)
    {
        // Don Kişot veya Sancho kapının yanına geldiğinde anlar
        if (other.GetComponent<SanchoMovement>() != null || other.GetComponent<DonMovement>() != null)
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<SanchoMovement>() != null || other.GetComponent<DonMovement>() != null)
        {
            playerInRange = false;
        }
    }
}