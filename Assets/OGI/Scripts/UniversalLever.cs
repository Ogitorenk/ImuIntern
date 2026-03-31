using UnityEngine;
using UnityEngine.Events;

public class UniversalLever : MonoBehaviour
{
    [Header("Ayarlar")]
    public KeyCode interactionKey = KeyCode.F; // Artik F ile calisiyor
    public UnityEvent onActivate;

    [Header("Görsel Geri Bildirim")]
    public Transform leverHandle;
    public Vector3 pulledRotation = new Vector3(45f, 0, 0);

    private bool isPulled = false;
    private bool playerInRange = false;

    void Update()
    {
        // Oyuncu menzildeyse, şalter henüz çekilmediyse ve "F"ye bastıysa
        if (playerInRange && !isPulled && Input.GetKeyDown(interactionKey))
        {
            Pull();
        }
    }

    private void Pull()
    {
        isPulled = true;

        if (leverHandle != null)
        {
            leverHandle.localRotation = Quaternion.Euler(pulledRotation);
        }

        if (onActivate != null)
        {
            onActivate.Invoke();
        }

        Debug.Log("<color=orange>🕹️ Şalter F ile çekildi!</color>");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}