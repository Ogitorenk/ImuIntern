using UnityEngine;

public class RealityObject : MonoBehaviour
{
    public enum TargetDimension { SadeceDonKisot, SadeceSancho }

    [Header("Bu obje kime ait?")]
    public TargetDimension belongsTo;

    [Tooltip("Hayalet modundayken ne kadar saydam olsun? (0 görünmez, 1 katý)")]
    [Range(0f, 1f)] public float ghostAlpha = 0.3f;

    private Material mat;
    private Color originalColor;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            originalColor = mat.color;
        }
    }

    void Update()
    {
        // GameManager sahnede yoksa hata vermesin diye güvenlik
        if (DualRealityManager.Instance == null || mat == null) return;

        // O an hangi karakter aktif?
        bool isDonActive = DualRealityManager.Instance.isDonActive;

        // Bu obje aktif karaktere mi ait?
        bool isMyTurn = (belongsTo == TargetDimension.SadeceDonKisot && isDonActive) ||
                        (belongsTo == TargetDimension.SadeceSancho && !isDonActive);

        // Rengin Alpha (saydamlýk) kanalýný duruma göre deðiþtir
        Color targetColor = originalColor;
        targetColor.a = isMyTurn ? 1f : ghostAlpha;
        mat.color = targetColor;
    }
}