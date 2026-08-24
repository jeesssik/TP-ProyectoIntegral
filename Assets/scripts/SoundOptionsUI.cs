using UnityEngine;
using UnityEngine.UI;

public class SoundOptionsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider ambientSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Toggles")]
    [SerializeField] private Toggle musicMuteToggle;
    [SerializeField] private Toggle ambientMuteToggle;
    [SerializeField] private Toggle sfxMuteToggle;

    private void OnEnable()
    {
        LoadValuesFromAudioManager();
        AssignListeners(); // 🔥 Forzamos la reconexión segura cada vez que se abre el panel
    }

    private void Start()
    {
        LoadValuesFromAudioManager();
        AssignListeners();
    }

    private void OnDisable()
    {
        RemoveListeners(); // Evitamos duplicados limpiando al cerrar el panel
    }

    private void LoadValuesFromAudioManager()
    {
        if (AudioManager.Instance == null) return;

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
        if (ambientSlider != null) ambientSlider.SetValueWithoutNotify(AudioManager.Instance.GetAmbientVolume());
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSFXVolume());

        if (musicMuteToggle != null) musicMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.GetMusicMuted());
        if (ambientMuteToggle != null) ambientMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.GetAmbientMuted());
        if (sfxMuteToggle != null) sfxMuteToggle.SetIsOnWithoutNotify(AudioManager.Instance.GetSFXMuted());
    }

    // =========================================================================
    // 🔥 ASIGNACIÓN DE EVENTOS AUTOMÁTICA Y SEGURA ENTRE ESCENAS
    // =========================================================================
    private void AssignListeners()
    {
        if (AudioManager.Instance == null) return;

        // Primero limpiamos por si las dudas
        RemoveListeners();

        // Enlazamos tus métodos directamente a los componentes de la UI
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (ambientSlider != null) ambientSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (musicMuteToggle != null) musicMuteToggle.onValueChanged.AddListener(OnMusicMuteChanged);
        if (ambientMuteToggle != null) ambientMuteToggle.onValueChanged.AddListener(OnAmbientMuteChanged);
        if (sfxMuteToggle != null) sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteChanged);
    }

    private void RemoveListeners()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (ambientSlider != null) ambientSlider.onValueChanged.RemoveListener(OnAmbientVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

        if (musicMuteToggle != null) musicMuteToggle.onValueChanged.RemoveListener(OnMusicMuteChanged);
        if (ambientMuteToggle != null) ambientMuteToggle.onValueChanged.RemoveListener(OnAmbientMuteChanged);
        if (sfxMuteToggle != null) sfxMuteToggle.onValueChanged.RemoveListener(OnSFXMuteChanged);
    }

    // =========================================================================
    // TUS FUNCIONES DE SIEMPRE (Se mantienen intactas)
    // =========================================================================
    public void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        Debug.Log("Slider música cambió a: " + value);
        AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnAmbientVolumeChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetAmbientVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetSFXVolume(value);
    }

    public void OnMusicMuteChanged(bool value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.ToggleMusicMute(value);
    }

    public void OnAmbientMuteChanged(bool value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.ToggleAmbientMute(value);
    }

    public void OnSFXMuteChanged(bool value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.ToggleSFXMute(value);
    }
}