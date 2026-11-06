using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Level1IntroVideo : MonoBehaviour
{
    private const string IntroPlayedKey = "Level1IntroVideoPlayed_v3";

    [SerializeField] private VideoClip introVideo;

    private VideoPlayer videoPlayer;
    private RawImage videoImage;
    private RenderTexture videoTexture;
    private GameObject videoCanvas;
    private float previousTimeScale;
    private bool isPlaying;

    private void Awake()
    {
        if (PlayerPrefs.GetInt(IntroPlayedKey, 0) == 1)
            return;

        if (introVideo == null)
            introVideo = Resources.Load<VideoClip>("videoIntro");

        if (introVideo == null)
        {
            Debug.LogError("No se encontró videoIntro.mp4 para la introducción de Level-1.", this);
            return;
        }

        CreateVideoLayer();

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPlaying = true;
        videoPlayer.Prepare();
    }

    private void OnDestroy()
    {
        if (isPlaying)
            Time.timeScale = previousTimeScale;
    }

    private void CreateVideoLayer()
    {
        videoCanvas = new GameObject("Level1IntroVideoCanvas");
        Canvas canvas = videoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = videoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        videoCanvas.AddComponent<GraphicRaycaster>();

        GameObject videoObject = new GameObject("VideoImage");
        videoObject.transform.SetParent(videoCanvas.transform, false);
        videoImage = videoObject.AddComponent<RawImage>();
        videoImage.color = Color.black;
        videoImage.raycastTarget = true;
        videoImage.rectTransform.anchorMin = Vector2.zero;
        videoImage.rectTransform.anchorMax = Vector2.one;
        videoImage.rectTransform.offsetMin = Vector2.zero;
        videoImage.rectTransform.offsetMax = Vector2.zero;

        videoPlayer = videoCanvas.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = introVideo;
        videoTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        videoTexture.Create();
        videoPlayer.targetTexture = videoTexture;
        videoImage.texture = videoTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        PlayerPrefs.SetInt(IntroPlayedKey, 1);
        PlayerPrefs.Save();
        player.Play();
    }

    private void OnVideoFinished(VideoPlayer player)
    {
        FinishVideo();
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogError($"No se pudo reproducir el video de introducción: {message}", this);
        FinishVideo();
    }

    private void FinishVideo()
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        Time.timeScale = previousTimeScale;

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }

        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
            videoTexture = null;
        }

        if (videoCanvas != null)
            Destroy(videoCanvas);
    }
}
