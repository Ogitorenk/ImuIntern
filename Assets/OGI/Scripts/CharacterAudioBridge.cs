using UnityEngine;

public class CharacterAudioBridge : MonoBehaviour
{
    private DonMovement donMovement;
    private SanchoMovement sanchoMovement;

    void Start()
    {
        // Script en üst parent objesindeki hareket kodlarýný otomatik bulur kanka
        donMovement = GetComponentInParent<DonMovement>();
        sanchoMovement = GetComponentInParent<SanchoMovement>();
    }

    // Animasyondan gelen tetikleyiciyi üstteki asýl koda paslýyoruz kanka:
    public void PlayFootstepSound()
    {
        if (donMovement != null && donMovement.enabled)
        {
            donMovement.PlayFootstepSound();
        }
        else if (sanchoMovement != null && sanchoMovement.enabled)
        {
            sanchoMovement.PlayFootstepSound();
        }
    }
}