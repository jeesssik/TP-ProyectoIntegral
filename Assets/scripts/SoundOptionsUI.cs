using UnityEngine;
using UnityEngine.UI;

public class SoundOptionsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider ambientSlider;
    [SerializeField] Slider sfxSlider;

    [Header("Toggles")]
    [SerializeField] Toggle musicMuteToggle;
    [SerializeField] Toggle ambientMuteToggle;
    [SerializeField] Toggle sfxMuteToggle;

    void OnEnable()
    {
        LoadValuesFromAudioManager();
        AssignListeners();
    }

    void OnDisable()
    {
        RemoveListeners();
    }

    void LoadValuesFromAudioManager()
    {
        if (AudioManager.Instance == null)
            return;

        SetSlider(musicSlider, AudioManager.Instance.GetMusicVolume());
        SetSlider(ambientSlider, AudioManager.Instance.GetAmbientVolume());
        SetSlider(sfxSlider, AudioManager.Instance.GetSFXVolume());

        SetToggle(musicMuteToggle, AudioManager.Instance.GetMusicMuted());
        SetToggle(ambientMuteToggle, AudioManager.Instance.GetAmbientMuted());
        SetToggle(sfxMuteToggle, AudioManager.Instance.GetSFXMuted());
    }

    void AssignListeners()
    {
        if (AudioManager.Instance == null)
            return;

        RemoveListeners();

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (ambientSlider != null)
            ambientSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (musicMuteToggle != null)
            musicMuteToggle.onValueChanged.AddListener(OnMusicMuteChanged);
        if (ambientMuteToggle != null)
            ambientMuteToggle.onValueChanged.AddListener(OnAmbientMuteChanged);
        if (sfxMuteToggle != null)
            sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteChanged);
    }

    void RemoveListeners()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (ambientSlider != null)
            ambientSlider.onValueChanged.RemoveListener(OnAmbientVolumeChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

        if (musicMuteToggle != null)
            musicMuteToggle.onValueChanged.RemoveListener(OnMusicMuteChanged);
        if (ambientMuteToggle != null)
            ambientMuteToggle.onValueChanged.RemoveListener(OnAmbientMuteChanged);
        if (sfxMuteToggle != null)
            sfxMuteToggle.onValueChanged.RemoveListener(OnSFXMuteChanged);
    }

    static void SetSlider(Slider slider, float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(value);
    }

    static void SetToggle(Toggle toggle, bool value)
    {
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    public void OnAmbientVolumeChanged(float value)
    {
        AudioManager.Instance?.SetAmbientVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    public void OnMusicMuteChanged(bool value)
    {
        AudioManager.Instance?.ToggleMusicMute(value);
    }

    public void OnAmbientMuteChanged(bool value)
    {
        AudioManager.Instance?.ToggleAmbientMute(value);
    }

    public void OnSFXMuteChanged(bool value)
    {
        AudioManager.Instance?.ToggleSFXMute(value);
    }
}
