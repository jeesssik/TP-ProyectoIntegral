using UnityEngine;

public class LevelAudio : MonoBehaviour
{
    [SerializeField] private AudioClip levelMusic;
    [SerializeField] private AudioClip levelAmbient;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
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
}