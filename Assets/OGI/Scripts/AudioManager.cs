using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("--- ÇEVRE SESLERÝ ---")]
    public AudioClip[] footstepDirt;
    public AudioClip[] footstepStone;
    public AudioClip[] footstepWood;

    public AudioClip donJumpSound;
    public AudioClip sanchoJumpSound;
    public AudioClip landSound;
    public AudioClip boxPushSound;
    public AudioClip leverSound;

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

    [Header("--- ÖLÜM SESLERÝ ---")]
    [Tooltip("Don Kiþot öldüðünde çalacak ses kanka")]
    public AudioClip donDeathSound;
    [Tooltip("Sancho öldüðünde çalacak ses kanka")]
    public AudioClip sanchoDeathSound;

    // === SPAM ENGELLEME SÝHRÝ: Her sesin son oynatýlma zamanýný tutan liste ===
    private Dictionary<AudioClip, float> soundCooldowns = new Dictionary<AudioClip, float>();

    [Header("Spam Hassasiyet Ayarý")]
    [Tooltip("Ayný ses klibinin tekrar çalabilmesi için aradan geçmesi gereken minimum saniye kanka.")]
    public float globalSpamCooldown = 0.15f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // === AKILLI HASAR VE ÖLÜM SES SÝSTEMÝ ===
    // Karakter hasar aldýðýnda kendi can scriptinden bu fonksiyonu çaðýr kanka.
    // Örnek kullaným: AudioManager.Instance.PlayCharacterDamageOrDeath("Don", currentHealth, position);
    public void PlayCharacterDamageOrDeath(string characterName, float currentHealth, Vector3 position = default, float volume = 1f)
    {
        if (characterName == "Don")
        {
            if (currentHealth <= 0)
            {
                // Caný sýfýra veya altýna düþtüyse sadece ölüm sesini çal
                PlaySound(donDeathSound, position, volume);
            }
            else
            {
                // Hala yaþýyorsa normal hasar sesini çal
                PlaySound(donDamageSound, position, volume);
            }
        }
        else if (characterName == "Sancho")
        {
            if (currentHealth <= 0)
            {
                PlaySound(sanchoDeathSound, position, volume);
            }
            else
            {
                PlaySound(sanchoDamageSound, position, volume);
            }
        }
    }

    // 2D veya 3D ses çalma fonksiyonu (Konum verilirse 3D, verilmezse 2D çalar kanka)
    public void PlaySound(AudioClip clip, Vector3 position = default, float volume = 1f)
    {
        if (clip == null) return;

        // === SPAM KORUMASI KONTROLÜ KANKA ===
        if (soundCooldowns.TryGetValue(clip, out float lastPlayedTime))
        {
            // Eðer sesten hemen sonra geçen süre belirlediðimiz cooldown'dan küçükse sesi çalma, engelle kanka!
            if (Time.time - lastPlayedTime < globalSpamCooldown)
            {
                return;
            }
        }

        // Zamaný güncelle veya yoksa listeye ekle
        soundCooldowns[clip] = Time.time;

        // Oynatma mantýðý canavar gibi devam ediyor kanka:
        if (position == default)
        {
            if (Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
            }
        }
        else
        {
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
            ClipAndPlayRandom(targetArray, position);
        }
    }

    private void ClipAndPlayRandom(AudioClip[] targetArray, Vector3 position)
    {
        AudioClip randomClip = targetArray[Random.Range(0, targetArray.Length)];
        // Yürüme sesleri PlaySound üzerinden geçeceði için burasý da otomatik spam filtreli kanka!
        PlaySound(randomClip, position, 0.4f);
    }
}