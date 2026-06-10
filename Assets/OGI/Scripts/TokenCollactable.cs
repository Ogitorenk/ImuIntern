using UnityEngine;

public class TokenCollectible : MonoBehaviour
{
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
    public float floatFrequency = 1.5f; // Yukarý aþaðý gitme hýzý
    public float floatAmplitude = 0.15f; // Yukarý aþaðý gitme mesafesi (genliði)

    [Header("--- KODDAN SARI PARLAMA (EMISSION) ANÝMASYONU ---")]
    [Tooltip("Objenin kendi kendine sarý sarý parlayýp sönmesini istiyorsan bunu aç kanka")]
    public bool pulseGlow = true;
    public float glowSpeed = 3f; // Parlama yanýp sönme hýzý
    [ColorUsage(true, true)] // Unity Inspector'ýnda HDR renk seçmemizi saðlar, parlamayý uçurur!
    public Color glowColor = new Color(1f, 0.85f, 0f, 1f); // Saf sarý/altýn rengi

    private Vector3 startPosition;
    private Renderer targetRenderer;
    private Material tokenMaterial;

    void Start()
    {
        // Yukarý aþaðý hareketin sapmamasý için baþlangýç pozisyonunu kaydediyoruz
        startPosition = transform.position;

        // Objenin üzerindeki Renderer ve Materyale ulaþýyoruz ki rengini koddan manipüle edelim
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            // .material diyerek o objeye özel bir materyal klonluyoruz ki sahnedeki diðer her þey sarý parlamasýn kanka
            tokenMaterial = targetRenderer.material;

            // Materyalin Emission özelliðini koddan aktif ediyoruz
            tokenMaterial.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        // 1. KENDÝ EKSENÝNDE DÖNME ANÝMASYONU
        if (rotate)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        }

        // 2. YUKARI AÞAÐI YÜZME (HOVER) ANÝMASYONU
        if (floatMovement)
        {
            Vector3 tempPos = startPosition;
            // Sinüs dalgasý kullanarak pürüzsüz bir git-gel hareketi yaratýyoruz kanka
            tempPos.y += Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = tempPos;
        }

        // 3. KODDAN SARI PARLAMA (GLOW/PULSE) EFEKTÝ
        if (pulseGlow && tokenMaterial != null)
        {
            // Sinüs dalgasýný 0 ile 1 arasýna sýkýþtýrýyoruz ki ýþýk tamamen sönüp parlasýn
            float emissionIntensity = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f;

            // Renk ile þiddeti çarpýp materyale þýrýnga ediyoruz kanka
            Color finalGlowColor = glowColor * emissionIntensity;

            // Standart Unity Standard Shader veya URP/HDRP Lit Shader için ortak emission deðiþken ismi "_EmissionColor" dýr.
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
        // Bellek sýzýntýsý (Memory Leak) olmasýn diye oluþturduðumuz dinamik materyali siliniyor kanka
        if (tokenMaterial != null)
        {
            Destroy(tokenMaterial);
        }
    }
}