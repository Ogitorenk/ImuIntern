using UnityEngine;

public class LanceObj : MonoBehaviour
{
    private Rigidbody rb;
    public bool isStuck = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Oyuncuya çarparsa saplanmasýn
        if (isStuck || collision.gameObject.CompareTag("Player")) return;

        isStuck = true;
        rb.isKinematic = true;

        ContactPoint contact = collision.contacts[0];

        // Duvara saplanma açýsý (Senin yazdýðýn kýsým, burasý okey)
        Quaternion lookRot = Quaternion.LookRotation(-contact.normal);
        transform.rotation = lookRot * Quaternion.Euler(90f, 0f, 0f);

        transform.SetParent(collision.transform);

        // --- BURADAKÝ TRIGGER OLUÞTURMA KISMINI SÝLEBÝLÝRÝZ ---
        // Çünkü artýk karakter 'C'ye basýnca SphereCast ile etrafý tarýyor.
        // Ama mýzraðýn fiziksel bir Collider'ý (Box veya Capsule) mutlaka kalmalý.
    }

    // --- KRÝTÝK: OnTriggerEnter FONKSÝYONUNU TAMAMEN SÝLDÝK ---
    // Otomatik tutunmaya sebep olan yer burasýydý.
}