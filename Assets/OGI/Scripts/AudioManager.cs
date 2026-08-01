using UnityEngine;
using UnityEngine.Audio; // Mixer desteði için eklendi kanka
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // ========================================================
    // --- YENÝ EKLENDÝ: SETTINGS (MENÜ) MÝXER BAÐLANTILARI ---
    // ========================================================
    [Header("--- AUDIO MIXER KANALLARI (MENÜ ÝÇÝN) ---")]
    [Tooltip("MainMixer üzerindeki Master grubu kanka")]
    public AudioMixerGroup masterGroup;
    [Tooltip("MainMixer üzerindeki SFX grubu kanka")]
    public AudioMixerGroup sfxGroup;

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
    private AudioSource footstepAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            loopAudioSource = gameObject.AddComponent<AudioSource>();
            loopAudioSource.loop = true;
            loopAudioSource.playOnAwake = false;

            footstepAudioSource = gameObject.AddComponent<AudioSource>();
            footstepAudioSource.loop = false;
            footstepAudioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // === MÝXER KANALLARINI BÝLEÞENLERE ÝÐNELEME ===
        if (loopAudioSource != null && sfxGroup != null) loopAudioSource.outputAudioMixerGroup = sfxGroup;
        if (footstepAudioSource != null && sfxGroup != null) footstepAudioSource.outputAudioMixerGroup = sfxGroup;
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

        PlaySoundWithGroup(boxPushSound, position, boxPushVolume);
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

        PlaySoundWithGroup(clip, position, finalVolume);
    }

    // === MÝXER DESTEKLÝ DÝNAMÝK SES ÇALICI (ÇIKISI SFX KANALINA YÖNLENDÝRÝR) ===
    private void PlaySoundWithGroup(AudioClip clip, Vector3 position, float volume)
    {
        Vector3 targetPos = (position == default && Camera.main != null) ? Camera.main.transform.position : position;

        GameObject tempGO = new GameObject("TempAudioSource_" + clip.name);
        tempGO.transform.position = targetPos;

        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        if (sfxGroup != null) audioSource.outputAudioMixerGroup = sfxGroup; // Mixer'ýn SFX kanalýna baðlar!

        audioSource.Play();
        Destroy(tempGO, clip.length);
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
                footstepAudioSource.PlayOneShot(randomClip);
            }
        }
    }

    public void StopFootsteps()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
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