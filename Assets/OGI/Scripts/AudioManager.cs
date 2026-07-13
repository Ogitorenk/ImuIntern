using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("--- ÇEVRE SESLERÝ ---")]
    public AudioClip[] footstepDirt;
    public AudioClip[] footstepStone;
    public AudioClip[] footstepWood;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip boxPushSound;
    public AudioClip leverSound;

    // === YENÝ EKLENDÝ: EZÝCÝ TUZAK SESÝ KLÝBÝ ===
    [Tooltip("Ezici tuzak aþaðý küt diye inerken çalacak smash sesi kanka")]
    public AudioClip crusherSound;

    [Header("--- AKSÝYON SESLERÝ ---")]
    public AudioClip donDamageSound;
    public AudioClip sanchoDamageSound;
    public AudioClip shieldBlockSound;
    public AudioClip dodgeSound;
    public AudioClip donMeleeSound;
    public AudioClip sanchoMeleeSound;
    public AudioClip lanceThrowSound;
    public AudioClip arrowShootSound;
    public AudioClip drinkPotionSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Level designer sahneleri arasýnda geçiþ yaparken ses sisteminin zýnk diye kopmasýný engelliyoruz kanka
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 2D veya 3D ses çalma fonksiyonu (Konum verilirse 3D, verilmezse 2D çalar kanka)
    public void PlaySound(AudioClip clip, Vector3 position = default, float volume = 1f)
    {
        if (clip == null) return;

        if (position == default)
        {
            // Arayüz veya genel sesler için 2D çal kanka
            if (Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
            }
        }
        else
        {
            // Karakter sesleri ve akýllý 3D menzilli tuzaklar için tam koordinatýnda belirlenen volume ile çal kanka
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }

    // Yürüme sesleri tek düze olmasýn diye diziden rastgele ses seçen özel fonksiyon
    public void PlayFootstep(string groundTag, Vector3 position)
    {
        AudioClip[] targetArray = null;

        switch (groundTag)
        {
            case "Ground_Dirt": targetArray = footstepDirt; break;
            case "Ground_Stone": targetArray = footstepStone; break;
            case "Ground_Wood": targetArray = footstepWood; break;
        }

        if (targetArray != null && targetArray.Length > 0)
        {
            AudioClip randomClip = targetArray[Random.Range(0, targetArray.Length)];
            PlaySound(randomClip, position, 0.4f); // Yürüme sesi kafayý ütülemesin diye volume çýtýr kýsýk kanka
        }
    }
}