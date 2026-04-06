using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public Color activeColor = Color.green;
    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        // Sadece Player tag'li objeler (veya Sancho/Don) girince çalýþsýn
        if (!isActivated && (other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
        {
            isActivated = true;

            // Manager'a "Benim pozisyonumu en son nokta yap" diyoruz
            // Y eksenini biraz yukarý alýyoruz ki karakter yerin dibinde doðmasýn
            Vector3 spawnPoint = transform.position + Vector3.up * 1.5f;
            CheckpointManager.Instance.UpdateCheckpoint(spawnPoint);

            // Ýstersen burada bir efekt veya ses çalabilirsin
            GetComponent<Renderer>().material.color = activeColor;
        }
    }
}