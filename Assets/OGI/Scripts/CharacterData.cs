using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game Data/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("--- SAÐLIK SÝSTEMÝ ---")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("--- ÝKSÝR ENVANTERÝ ---")]
    public int healthPotionCount = 0;
    public int slowPotionCount = 0;

    // --- YENÝ EKLENDÝ: SANCHO OK SÝSTEMÝ ---
    [Header("--- SANCHO OK / SADAK SÝSTEMÝ ---")]
    public int arrowCount = 20; // Baþlangýçta ful baþlasýn kanka
    public int maxArrowCount = 20;

    public void ResetToDefault()
    {
        currentHealth = maxHealth;
        healthPotionCount = 0;
        slowPotionCount = 0;
        arrowCount = maxArrowCount; // Yeni oyunda oklar da fullensin
    }
}