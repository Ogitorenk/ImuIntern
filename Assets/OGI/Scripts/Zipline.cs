using UnityEngine;
using System.Collections;

public class ZiplinePrefab : MonoBehaviour
{
    [Header("Referanslar (Child Objerler)")]
    public Transform startPoint;
    public Transform endPoint;
    public Transform visualRope;

    [Header("Ayarlar")]
    public float zipSpeed = 12f;
    public float playerOffset = -2.2f; // İpin ne kadar altında asılacak?
    public KeyCode interactKey = KeyCode.F;

    private bool playerInRange = false;
    private GameObject currentPlayer;
    private bool isZipping = false;

    void Start()
    {
        UpdateRopeVisual();
    }

    // Editor'de noktaları hareket ettirdiğinde ipin otomatik güncellenmesi için
    void OnValidate()
    {
        if (startPoint != null && endPoint != null && visualRope != null)
            UpdateRopeVisual();
    }

    void Update()
    {
        if (playerInRange && !isZipping && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(ZipRoutine());
        }
    }

    void UpdateRopeVisual()
    {
        // İpi tam iki noktanın ortasına koy
        visualRope.position = (startPoint.position + endPoint.position) / 2f;

        // İpi bitiş noktasına baktır
        visualRope.LookAt(endPoint);

        // İpin uzunluğunu iki nokta arasındaki mesafeye göre ayarla
        // Not: Cylinder varsayılan olarak 2 birim boyundadır, o yüzden mesafeyi 2'ye bölüyoruz
        float dist = Vector3.Distance(startPoint.position, endPoint.position);
        visualRope.localScale = new Vector3(0.1f, 0.1f, dist / 2f);
        // Eğer ipin yönü yanlışsa scale değerlerini (x,y,z) kendi modeline göre kurcala kanka
    }

    IEnumerator ZipRoutine()
    {
        isZipping = true;

        // Karakteri hazırla
        CharacterController cc = currentPlayer.GetComponent<CharacterController>();
        MonoBehaviour moveScript = (MonoBehaviour)currentPlayer.GetComponent("DonMovement") ?? (MonoBehaviour)currentPlayer.GetComponent("SanchoMovement");

        if (moveScript != null) moveScript.enabled = false;

        float distance = Vector3.Distance(startPoint.position, endPoint.position);
        float t = 0;

        while (t < 1f)
        {
            t += (zipSpeed / distance) * Time.deltaTime;

            Vector3 targetPos = Vector3.Lerp(startPoint.position, endPoint.position, t);
            targetPos.y += playerOffset; // İpin altında asılı kalma payı

            currentPlayer.transform.position = targetPos;

            yield return null;
        }

        if (moveScript != null) moveScript.enabled = true;
        isZipping = false;
        Debug.Log("<color=cyan>🚠 Zipline başarıyla tamamlandı!</color>");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            currentPlayer = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (!isZipping) currentPlayer = null;
        }
    }
}