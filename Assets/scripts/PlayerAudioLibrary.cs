using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAudioLibrary", menuName = "Audio/Player Audio Library")]
public class PlayerAudioLibrary : ScriptableObject
{
    public AudioClip walkStep;
    public AudioClip jump;
    public AudioClip land;
    public AudioClip attack;
    public AudioClip hurt;
    public AudioClip death;
    public AudioClip dash;
    public AudioClip backDodge;
}
