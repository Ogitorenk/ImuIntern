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
        // Eðer zaten saplandýysa veya oyuncuya çarptýysa iþlem yapma
        if (isStuck || collision.gameObject.CompareTag("Player")) return;

        // --- YENÝ EKLENEN KONTROL: SADECE DUVARLARA SAPLAN ---
        // Eðer çarptýðý objenin Tag'i "Wall" DEÐÝLSE:
        if (!collision.gameObject.CompareTag("Wall"))
        {
            // 1. Oyuncunun yerdeki baþarýsýz mýzraða tutunmasýný engellemek için tag'i sil
            gameObject.tag = "Untagged";

            // 2. Saplanmasýn, sekmeye devam etsin diye burada kodu kesiyoruz
            // 3. Oyun kasmasýn diye yerdeki mýzraðý 3 saniye sonra yok et
            Destroy(gameObject, 3f);
            return;
        }

        // --- BURADAN AÞAÐISI SADECE "Wall" TAG'ÝNE ÇARPARSA ÇALIÞIR ---

        isStuck = true;
        rb.isKinematic = true;

        ContactPoint contact = collision.contacts[0];

        // Duvara dik açýyla saplanma matematiði
        Quaternion lookRot = Quaternion.LookRotation(-contact.normal);
        transform.rotation = lookRot * Quaternion.Euler(90f, 0f, 0f);

        // Mýzraðý çarptýðý duvarýn alt objesi yap ki duvar hareket ederse mýzrak da etsin
        transform.SetParent(collision.transform);

        // Garanti olsun diye duvara saplanan mýzraðýn tag'ini tekrar Lance yapýyoruz
        gameObject.tag = "Lance";
    }
}