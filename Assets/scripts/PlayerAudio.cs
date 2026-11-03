using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] PlayerAudioLibrary library;
    [SerializeField] float walkStepInterval = 0.45f;
    [SerializeField] float runStepInterval = 0.3f;

    Grace player;
    float nextWalkStepTime;

    void Awake()
    {
        player = GetComponent<Grace>();
    }

    void Update()
    {
        if (player == null || !player.EstaMoviendose)
        {
            nextWalkStepTime = 0f;
            return;
        }

        if (Time.time < nextWalkStepTime)
            return;

        PlayWalkStep(player.EstaCorriendo ? 0.9f : 1f);
        nextWalkStepTime = Time.time +
            (player.EstaCorriendo ? runStepInterval : walkStepInterval);
    }

    public void PlayWalkStep(float volumeMultiplier = 1f) =>
        Play(library != null ? library.walkStep : null, volumeMultiplier);

    public void PlayJump(float volumeMultiplier = 1f) =>
        Play(library != null ? library.jump : null, volumeMultiplier);

    public void PlayLand(float volumeMultiplier = 1f) =>
        Play(library != null ? library.land : null, volumeMultiplier);

    public void PlayAttack(float volumeMultiplier = 1f) =>
        Play(library != null ? library.attack : null, volumeMultiplier);

    public void PlayHurt(float volumeMultiplier = 1f) =>
        Play(library != null ? library.hurt : null, volumeMultiplier);

    public void PlayDeath(float volumeMultiplier = 1f) =>
        Play(library != null ? library.death : null, volumeMultiplier);

    public void PlayDash(float volumeMultiplier = 1f) =>
        Play(library != null ? library.dash : null, volumeMultiplier);

    public void PlayBackDodge(float volumeMultiplier = 1f) =>
        Play(library != null ? library.backDodge : null, volumeMultiplier);

    void Play(AudioClip clip, float volumeMultiplier)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip, volumeMultiplier);
    }
}
