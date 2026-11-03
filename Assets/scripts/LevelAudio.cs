using UnityEngine;

public class LevelAudio : MonoBehaviour
{
    [SerializeField] private AudioClip levelMusic;
    [SerializeField] private AudioClip levelAmbient;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("LevelAudio necesita un AudioManager activo. Ingresá al nivel desde Menu o agregá un bootstrap de audio.", this);
            return;
        }

        if (levelMusic != null)
            AudioManager.Instance.PlayMusic(levelMusic);
        else
            AudioManager.Instance.StopMusic();

        if (levelAmbient != null)
            AudioManager.Instance.PlayAmbient(levelAmbient);
        else
            AudioManager.Instance.StopAmbient();
    }
}