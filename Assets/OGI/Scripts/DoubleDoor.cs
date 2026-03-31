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

        // Hedef rotasyonları hesapla (Bize doğru açılması için açıları ayarla)
        // Eğer kapı ters açılırsa openAngle değerini eksi yapabilirsin
        leftOpenRot = Quaternion.Euler(0, -openAngle, 0);
        rightOpenRot = Quaternion.Euler(0, openAngle, 0);
    }

    // Bu fonksiyonu Şalter (Lever) veya Plaka'daki UnityEvent'e bağla!
    public void OpenDoors()
    {
        if (!isOpen)
        {
            StopAllCoroutines();
            StartCoroutine(MoveDoors(leftOpenRot, rightOpenRot));
            isOpen = true;
            Debug.Log("<color=cyan>🚪 Kapılar bize doğru açılıyor...</color>");
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
}