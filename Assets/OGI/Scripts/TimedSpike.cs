using UnityEngine;
using System.Collections;

public class TimedSpike : MonoBehaviour
{
    [Header("Hasar ve Hedef")]
    public float damage = 30f;
    [Tooltip("Sadece yukarý aþaðý hareket edecek olan 3D Diken Modeli")]
    public Transform spikeMesh;

    [Header("Zamanlama ve Mesafe")]
    public float upDistance = 1.5f;  // Diken ne kadar yukarý çýkacak?
    public float upTime = 2f;        // Yukarýda ne kadar bekleyecek?
    public float downTime = 2f;      // Yerin altýnda ne kadar saklanacak?
    public float moveSpeed = 8f;     // Çýkýþ/Ýniþ hýzý (Yumuþaklýk)

    private Vector3 downPos;
    private Vector3 upPos;
    private bool isUp = false;

    void Start()
    {
        if (spikeMesh != null)
        {
            // Dikenin baþlangýç pozisyonunu "Aþaðýda" kabul ediyoruz
            downPos = spikeMesh.localPosition;
            upPos = downPos + (Vector3.up * upDistance);
        }

        StartCoroutine(SpikeRoutine());
    }

    void Update()
    {
        if (spikeMesh != null)
        {
            // Dikeni hedef pozisyona yað gibi kaydýr
            Vector3 targetPos = isUp ? upPos : downPos;
            spikeMesh.localPosition = Vector3.Lerp(spikeMesh.localPosition, targetPos, Time.deltaTime * moveSpeed);
        }
    }

    IEnumerator SpikeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(downTime);
            isUp = true; // Dikeni çýkar
            yield return new WaitForSeconds(upTime);
            isUp = false; // Dikeni indir
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Sadece diken yukarýdaysa ve deðen "Player" ise hasar ver
        if (isUp && other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out DonMovement don)) don.TakeDamage(damage);
            else if (other.TryGetComponent(out SanchoMovement sancho)) sancho.TakeDamage(damage);
        }
    }
}