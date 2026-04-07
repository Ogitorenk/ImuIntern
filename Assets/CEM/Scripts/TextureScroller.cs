using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    // Kayma hızları (Inspector'dan değiştirebilirsin)
    public float scrollSpeedX = 0.05f;
    public float scrollSpeedY = 0.05f;
    
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        // Zamanla artan bir offset değeri hesapla
        float offsetX = Time.time * scrollSpeedX;
        float offsetY = Time.time * scrollSpeedY;

        // Materyalin ana dokusunu (MainTex) veya Emission dokusunu kaydır
        // "Particles/Standard Unlit" shader'ında doku ismi genellikle "_MainTex"tir.
        rend.material.SetTextureOffset("_MainTex", new Vector2(offsetX, offsetY));
    }
}