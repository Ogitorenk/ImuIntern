using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("--- ÇEVRE SESLERÝ ---")]
    public AudioClip[] footstepDirt;
    public AudioClip[] footstepStone;
    public AudioClip[] footstepWood;
    [Range(0f, 1f)] public float footstepVolume = 0.4f;

    public AudioClip donJumpSound;
    [Range(0f, 1f)] public float donJumpVolume = 1f;

    public AudioClip sanchoJumpSound;
    [Range(0f, 1f)] public float sanchoJumpVolume = 1f;

    public AudioClip landSound;
    [Range(0f, 1f)] public float landVolume = 0.8f;

    public AudioClip boxPushSound;
    [Range(0f, 1f)] public float boxPushVolume = 0.5f;

    public AudioClip leverSound;
    [Range(0f, 1f)] public float leverVolume = 1f;

    [Tooltip("Ezici tuzak aþaðý küt diye inerken çalacak smash sesi kanka")]
    public AudioClip crusherSound;
    [Range(0f, 1f)] public float crusherVolume = 1f;

    [Tooltip("Duvar pistonu fýrlarken çalacak push sesi kanka")]
    public AudioClip pusherSound;
    [Range(0f, 1f)] public float pusherVolume = 1f;

    [Header("--- AKSÝYON SESLERÝ ---")]
    public AudioClip donDamageSound;
    [Range(0f, 1f)] public float donDamageVolume = 1f;

    public AudioClip sanchoDamageSound;
    [Range(0f, 1f)] public float sanchoDamageVolume = 1f;

    public AudioClip shieldBlockSound;
    [Range(0f, 1f)] public float shieldBlockVolume = 1f;

    public AudioClip dodgeSound;
    [Range(0f, 1f)] public float dodgeVolume = 0.8f;

    public AudioClip donMeleeSound;
    [Range(0f, 1f)] public float donMeleeVolume = 1f;

    public AudioClip sanchoMeleeSound;
    [Range(0f, 1f)] public float sanchoMeleeVolume = 1f;

    public AudioClip lanceThrowSound;
    [Range(0f, 1f)] public float lanceThrowVolume = 1f;

    public AudioClip arrowShootSound;
    [Range(0f, 1f)] public float arrowShootVolume = 1f;

    public AudioClip drinkPotionSound;
    [Range(0f, 1f)] public float drinkPotionVolume = 1f;

    [Header("--- ÖLÜM SESLERÝ ---")]
    [Tooltip("Don Kiþot öldüðünde çalacak ses kanka")]
    public AudioClip donDeathSound;
    [Range(0f, 1f)] public float donDeathVolume = 1f;

    [Tooltip("Sancho öldüðünde çalacak ses kanka")]
    public AudioClip sanchoDeathSound;
    [Range(0f, 1f)] public float sanchoDeathVolume = 1f;

    // === SPAM ENGELLEME SÝHRÝ ===
    private Dictionary<AudioClip, float> soundCooldowns = new Dictionary<AudioClip, float>();

    [Header("Spam Hassasiyet Ayarý")]
    [Tooltip("Ayný ses klibinin tekrar çalabilmesi için aradan geçmesi gereken minimum saniye kanka.")]
    public float globalSpamCooldown = 0.15f;

    private AudioSource loopAudioSource;
    private AudioSource footstepAudioSource; // === YENÝ: ADIMLAR ÝÇÝN ÖZEL SABÝT KANAL ===

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            loopAudioSource = gameObject.AddComponent<AudioSource>();
            loopAudioSource.loop = true;
            loopAudioSource.playOnAwake = false;

            // Adým seslerinin arkada asýlý kalmasýný önleyen sabit kanal kanka
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
            footstepAudioSource.loop = false;
            footstepAudioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayBoxPushSound(Vector3 position)
    {
        if (boxPushSound == null) return;

        float customBoxCooldown = 1.0f;

        if (soundCooldowns.TryGetValue(boxPushSound, out float lastPlayedTime))
        {
            if (Time.time - lastPlayedTime < customBoxCooldown)
            {
                return;
            }
        }

        soundCooldowns[boxPushSound] = Time.time;

        if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(boxPushSound, position, boxPushVolume);
        }
    }

    public void PlayLoopingSound(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;

        if (loopAudioSource.isPlaying && loopAudioSource.clip == clip)
        {
            return;
        }

        float finalVolume = (volume < 0f) ? GetVolumeForClip(clip) : volume;

        loopAudioSource.clip = clip;
        loopAudioSource.volume = finalVolume;
        loopAudioSource.Play();
    }

    public void StopLoopingSound()
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
            loopAudioSource.clip = null;
        }
    }

    public void PlayCharacterDamageOrDeath(string characterName, float currentHealth, Vector3 position = default)
    {
        if (characterName == "Don")
        {
            if (currentHealth <= 0) PlaySound(donDeathSound, position, donDeathVolume);
            else PlaySound(donDamageSound, position, donDamageVolume);
        }
        else if (characterName == "Sancho")
        {
            if (currentHealth <= 0) PlaySound(sanchoDeathSound, position, sanchoDeathVolume);
            else PlaySound(sanchoDamageSound, position, sanchoDamageVolume);
        }
    }

    public void PlaySound(AudioClip clip, Vector3 position = default, float volume = -1f)
    {
        if (clip == null) return;

        if (soundCooldowns.TryGetValue(clip, out float lastPlayedTime))
        {
            if (Time.time - lastPlayedTime < globalSpamCooldown)
            {
                return;
            }
        }

        soundCooldowns[clip] = Time.time;

        float finalVolume = (volume < 0f) ? GetVolumeForClip(clip) : volume;

        if (position == default)
        {
            if (Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, finalVolume);
            }
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, position, finalVolume);
        }
    }

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
            if (randomClip != null)
            {
                footstepAudioSource.transform.position = position;
                footstepAudioSource.volume = footstepVolume;
                footstepAudioSource.PlayOneShot(randomClip); // Sabit kanal üzerinden çal
            }
        }
    }

    // === ADIM SESÝNÝ ANINDA KESEN SABÝT KANAL METODU ===
    public void StopFootsteps()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop(); // Durduðun an adýmlarý býçak gibi keser kanka!
        }
        soundCooldowns.Clear();
    }

    private float GetVolumeForClip(AudioClip clip)
    {
        if (clip == donJumpSound) return donJumpVolume;
        if (clip == sanchoJumpSound) return sanchoJumpVolume;
        if (clip == landSound) return landVolume;
        if (clip == boxPushSound) return boxPushVolume;
        if (clip == leverSound) return leverVolume;
        if (clip == crusherSound) return crusherVolume;
        if (clip == pusherSound) return pusherVolume;
        if (clip == donDamageSound) return donDamageVolume;
        if (clip == sanchoDamageSound) return sanchoDamageVolume;
        if (clip == shieldBlockSound) return shieldBlockVolume;
        if (clip == dodgeSound) return dodgeVolume;
        if (clip == donMeleeSound) return donMeleeVolume;
        if (clip == sanchoMeleeSound) return sanchoMeleeVolume;
        if (clip == lanceThrowSound) return lanceThrowVolume;
        if (clip == arrowShootSound) return arrowShootVolume;
        if (clip == drinkPotionSound) return drinkPotionVolume;
        if (clip == donDeathSound) return donDeathVolume;
        if (clip == sanchoDeathSound) return sanchoDeathVolume;

        return 1f;
    }
}