using UnityEngine;

public class LevelAudio : MonoBehaviour
{
    [SerializeField] private AudioClip levelMusic;
    [SerializeField] private AudioClip levelAmbient;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(levelMusic);
            AudioManager.Instance.PlayAmbient(levelAmbient);
        }
    }
}