using UnityEngine;
using System.Collections;

public class TriggeredSpike : MonoBehaviour
{
    [Header("Hasar ve Hedef")]
    public float damage = 40f;
    [Tooltip("Hareket edecek olan 3D Diken Modeli")]
    public Transform spikeMesh;

    [Header("Tetiklenme Ayarlarý")]
    public float delayBeforeSpike = 0.5f; // Bastýktan kaç saniye sonra çýksýn (Tepki süresi)
    public float upDuration = 1.5f;       // Çýktýktan sonra kaç saniye havada kalsýn
    public float upDistance = 1.5f;       // Ne kadar yukarý çýksýn
    public float moveSpeed = 15f;         // Fýrlama hýzý (Sinsi olduðu için hýzlý olmalý!)

    private Vector3 downPos;
    private Vector3 upPos;
    private bool isUp = false;
    private bool isTriggered = false; // Tuzak zaten çalýþýyorsa tekrar tetiklenmesini önler

    void Start()
    {
        if (spikeMesh != null)
        {
            downPos = spikeMesh.localPosition;
            upPos = downPos + (Vector3.up * upDistance);
        }
    }

    void Update()
    {
        if (spikeMesh != null)
        {
            Vector3 targetPos = isUp ? upPos : downPos;
            spikeMesh.localPosition = Vector3.Lerp(spikeMesh.localPosition, targetPos, Time.deltaTime * moveSpeed);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Oyuncu alana girerse ve tuzak o an çalýþmýyorsa tetikle
        if (other.CompareTag("Player") && !isTriggered)
        {
            StartCoroutine(SpikeRoutine());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Diken havadayken üstünde duruyorsa hasar ver
        if (isUp && other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out DonMovement don)) don.TakeDamage(damage);
            else if (other.TryGetComponent(out SanchoMovement sancho)) sancho.TakeDamage(damage);
        }
    }

    IEnumerator SpikeRoutine()
    {
        isTriggered = true;

        // Bastýktan sonra beklenen o korkutucu yarým saniye
        yield return new WaitForSeconds(delayBeforeSpike);

        // Diken fýrlar!
        isUp = true;

        // Havada bekleme süresi
        yield return new WaitForSeconds(upDuration);

        // Diken iner ve tuzak yeni bir kurban için sýfýrlanýr
        isUp = false;
        isTriggered = false;
    }
}