using UnityEngine;

public class TokenCollectible : MonoBehaviour
{
    [Header("--- BENZERSÝZ KÝMLÝK AYARI (STEAM ÝÇÝN) ---")]
    [Tooltip("Her token'a haritada kendine has, eþsiz bir isim ver kanka. Örn: Lvl1_Token_01")]
    public string tokenUniqueID;

    [Header("--- GÖRSEL VE SES EFEKTLERÝ ---")]
    [Tooltip("Token toplandýðý salise çalacak olan o çýn sesi kanka")]
    public AudioClip collectSound;

    [Tooltip("Token toplandýðýnda arkasýnda býrakacaðý ekstra patlama particle efekti (Opsiyonel)")]
    public GameObject collectParticlePrefab;

    [Header("--- HAREKET ANÝMASYONLARI ---")]
    [Tooltip("Token kendi ekseninde dönsün mu?")]
    public bool rotate = true;
    public float rotationSpeed = 100f;

    [Tooltip("Token tatlý tatlý yukarý aþaðý yüzer gibi hareket etsin mi kanka?")]
    public bool floatMovement = true;
    public float floatFrequency = 1.5f;
    public float floatAmplitude = 0.15f;

    [Header("--- KODDAN SARI PARLAMA (EMISSION) ANÝMASYONU ---")]
    [Tooltip("Objenin kendi kendine sarý sarý parlayýp sönmesini istiyorsan bunu aç kanka")]
    public bool pulseGlow = true;
    public float glowSpeed = 3f;
    [ColorUsage(true, true)]
    public Color glowColor = new Color(1f, 0.85f, 0f, 1f);

    private Vector3 startPosition;
    private Renderer targetRenderer;
    private Material tokenMaterial;

    void Start()
    {
        // --- STEAM / SAVE KORUMASI: Eðer bu spesifik token dün toplandýysa haritada doðmasýn kanka ---
        if (!string.IsNullOrEmpty(tokenUniqueID) && PlayerPrefs.GetInt("PickedToken_" + tokenUniqueID, 0) == 1)
        {
            Destroy(gameObject);
            return;
        }

        startPosition = transform.position;

        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            tokenMaterial = targetRenderer.material;
            tokenMaterial.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        if (rotate)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        }

        if (floatMovement)
        {
            Vector3 tempPos = startPosition;
            tempPos.y += Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = tempPos;
        }

        if (pulseGlow && tokenMaterial != null)
        {
            float emissionIntensity = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f;
            Color finalGlowColor = glowColor * emissionIntensity;
            tokenMaterial.SetColor("_EmissionColor", finalGlowColor);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        // --- HARD DÝSK KÝLÝDÝ: Bu token'ýn toplandýðýný PlayerPrefs'e yazýyoruz ---
        if (!string.IsNullOrEmpty(tokenUniqueID))
        {
            PlayerPrefs.SetInt("PickedToken_" + tokenUniqueID, 1);
        }

        if (TokenManager.Instance != null)
        {
            TokenManager.Instance.AddToken();
        }

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        if (collectParticlePrefab != null)
        {
            GameObject particle = Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
            Destroy(particle, 2f);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (tokenMaterial != null)
        {
            Destroy(tokenMaterial);
        }
    }
}