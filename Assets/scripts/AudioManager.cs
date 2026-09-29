using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    const string PrefMusicVolume = "MusicVolume";
    const string PrefAmbientVolume = "AmbientVolume";
    const string PrefSfxVolume = "SFXVolume";
    const string PrefMusicMuted = "MusicMuted";
    const string PrefAmbientMuted = "AmbientMuted";
    const string PrefSfxMuted = "SFXMuted";

    [Header("Sources")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource ambientSource;
    [SerializeField] AudioSource sfxSource;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Mute")]
    public bool musicMuted;
    public bool ambientMuted;
    public bool sfxMuted;

    [Header("Player SFX Clips")]
    public AudioClip playerWalkStep;
    public AudioClip playerJump;
    public AudioClip playerLand;
    public AudioClip playerAttack;
    public AudioClip playerHurt;
    public AudioClip playerDeath;
    public AudioClip playerDash;
    public AudioClip backDodge;

    public float GetMusicVolume() => musicVolume;
    public float GetAmbientVolume() => ambientVolume;
    public float GetSFXVolume() => sfxVolume;
    public bool GetMusicMuted() => musicMuted;
    public bool GetAmbientMuted() => ambientMuted;
    public bool GetSFXMuted() => sfxMuted;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAudioSettings();
        ApplyVolumes();
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null || sfxMuted || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        ApplyVolumes();
        musicSource.Play();
    }

    public void PlayAmbient(AudioClip clip)
    {
        if (clip == null || ambientSource == null)
            return;

        ambientSource.clip = clip;
        ambientSource.loop = true;
        ApplyVolumes();
        ambientSource.Play();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        ApplyAndSave();
    }

    public void SetAmbientVolume(float value)
    {
        ambientVolume = value;
        ApplyAndSave();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        ApplyAndSave();
    }

    public void ToggleMusicMute(bool muted)
    {
        musicMuted = muted;
        ApplyAndSave();
    }

    public void ToggleAmbientMute(bool muted)
    {
        ambientMuted = muted;
        ApplyAndSave();
    }

    public void ToggleSFXMute(bool muted)
    {
        sfxMuted = muted;
        ApplyAndSave();
    }

    void ApplyAndSave()
    {
        ApplyVolumes();
        SaveAudioSettings();
    }

    void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicMuted ? 0f : musicVolume;
        if (ambientSource != null)
            ambientSource.volume = ambientMuted ? 0f : ambientVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxMuted ? 0f : sfxVolume;
    }

    void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat(PrefMusicVolume, musicVolume);
        PlayerPrefs.SetFloat(PrefAmbientVolume, ambientVolume);
        PlayerPrefs.SetFloat(PrefSfxVolume, sfxVolume);
        PlayerPrefs.SetInt(PrefMusicMuted, musicMuted ? 1 : 0);
        PlayerPrefs.SetInt(PrefAmbientMuted, ambientMuted ? 1 : 0);
        PlayerPrefs.SetInt(PrefSfxMuted, sfxMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    void LoadAudioSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, 1f);
        ambientVolume = PlayerPrefs.GetFloat(PrefAmbientVolume, 1f);
        sfxVolume = PlayerPrefs.GetFloat(PrefSfxVolume, 1f);
        musicMuted = PlayerPrefs.GetInt(PrefMusicMuted, 0) == 1;
        ambientMuted = PlayerPrefs.GetInt(PrefAmbientMuted, 0) == 1;
        sfxMuted = PlayerPrefs.GetInt(PrefSfxMuted, 0) == 1;
    }
}
