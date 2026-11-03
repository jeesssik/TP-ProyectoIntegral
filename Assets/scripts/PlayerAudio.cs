using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] PlayerAudioLibrary library;
    [SerializeField] float walkStepInterval = 0.45f;
    [SerializeField] float runStepInterval = 0.3f;
    [SerializeField] float walkStepDuration = 0.18f;
    [SerializeField, Min(1)] int walkStepVariantCount = 4;
    [SerializeField, Range(0f, 1f)] float walkStepVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] float runStepVolume = 0.4f;
    [SerializeField, Range(0.8f, 1.2f)] float walkStepMinPitch = 0.94f;
    [SerializeField, Range(0.8f, 1.2f)] float walkStepMaxPitch = 1f;
    [SerializeField, Range(1000f, 22000f)] float walkStepLowPassCutoff = 4200f;

    Grace player;
    float nextWalkStepTime;
    int lastWalkStepVariant = -1;

    void Awake()
    {
        player = GetComponent<Grace>();

        if (library == null)
            Debug.LogError("PlayerAudio no tiene asignada una PlayerAudioLibrary.", this);
        else if (library.walkStep == null)
            Debug.LogError("PlayerAudioLibrary no tiene asignado el clip de pasos.", library);
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

        PlayWalkStep(player.EstaCorriendo ? runStepVolume : walkStepVolume);
        nextWalkStepTime = Time.time +
            (player.EstaCorriendo ? runStepInterval : walkStepInterval);
    }

    public void PlayWalkStep(float volumeMultiplier = 1f)
    {
        AudioClip walkStep = library != null ? library.walkStep : null;
        if (walkStep == null)
        {
            Play(null, volumeMultiplier);
            return;
        }

        int variantCount = Mathf.Max(1, walkStepVariantCount);
        int variant = ChooseWalkStepVariant(variantCount);
        float segmentLength = walkStep.length / variantCount;
        float startTime = variant * segmentLength;
        float minPitch = Mathf.Min(walkStepMinPitch, walkStepMaxPitch);
        float maxPitch = Mathf.Max(walkStepMinPitch, walkStepMaxPitch);
        float pitch = Random.Range(minPitch, maxPitch);

        Play(walkStep, volumeMultiplier, walkStepDuration, startTime, pitch, walkStepLowPassCutoff);
    }

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

    int ChooseWalkStepVariant(int variantCount)
    {
        if (variantCount == 1)
            return 0;

        int variant = Random.Range(0, variantCount);
        if (variant == lastWalkStepVariant)
            variant = (variant + 1) % variantCount;

        lastWalkStepVariant = variant;
        return variant;
    }

    void Play(
        AudioClip clip,
        float volumeMultiplier,
        float duration = 0f,
        float startTime = 0f,
        float pitch = 1f,
        float lowPassCutoff = 22000f)
    {
        if (clip == null)
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogError("No existe un AudioManager activo para reproducir SFX.", this);
            return;
        }

        if (duration > 0f)
            AudioManager.Instance.PlaySFXForDuration(
                clip,
                duration,
                volumeMultiplier,
                startTime,
                pitch,
                lowPassCutoff);
        else
            AudioManager.Instance.PlaySFX(clip, volumeMultiplier);
    }
}
