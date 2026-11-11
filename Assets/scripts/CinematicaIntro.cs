using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Tapa el menú y reproduce el video en un RawImage. Level-1 se carga al terminar.
/// Escape, Espacio o Enter la saltan.
/// </summary>
public class CinematicaIntro : MonoBehaviour
{
    public static bool EnCurso { get; private set; }

    static bool yaSeMostro;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarAlAbrirElJuego()
    {
        EnCurso = false;
        yaSeMostro = false;
    }

    const float EsperaMaximaPreparacion = 20f;
    const float EsperaMaximaArranque = 5f;
    const float DemoraAntesDeSaltar = 0.25f;
    const string RutaClip = "Assets/Prefabs/Grace Bond.mp4";

    VideoClip clip;
    string escenaDestino;
    VideoPlayer reproductor;
    RawImage imagenVideo;
    bool saltar;
    float saltoDesde = float.PositiveInfinity;
    string errorPreparacion;

    public static void Reproducir(VideoClip clip, string escenaDestino)
    {
        if (EnCurso)
            return;

        if (yaSeMostro)
        {
            if (!string.IsNullOrEmpty(escenaDestino))
                SceneManager.LoadScene(escenaDestino);
            return;
        }

        yaSeMostro = true;

        GameObject raiz = new GameObject("CinematicaIntro");
        CinematicaIntro intro = raiz.AddComponent<CinematicaIntro>();
        intro.clip = clip;
        intro.escenaDestino = escenaDestino;
        intro.StartCoroutine(intro.ReproducirSecuencia());
    }

    void Update()
    {
        if (Time.unscaledTime < saltoDesde)
            return;

        if (Input.GetKeyDown(KeyCode.Escape)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter))
            saltar = true;
    }

    IEnumerator ReproducirSecuencia()
    {
        EnCurso = true;
        saltoDesde = Time.unscaledTime + DemoraAntesDeSaltar;
        AudioManager.Instance?.StopMusic();
        AudioManager.Instance?.StopAmbient();

        if (clip == null)
            clip = CargarClipPorDefecto();

        CrearPantalla();

        if (clip == null)
        {
            Debug.LogWarning("CinematicaIntro: no hay video asignado. Se carga el nivel igual.", this);
            CargarEscena();
            yield break;
        }

        reproductor = gameObject.AddComponent<VideoPlayer>();
        reproductor.playOnAwake = false;
        reproductor.isLooping = false;
        reproductor.waitForFirstFrame = true;
        reproductor.skipOnDrop = true;
        reproductor.source = VideoSource.VideoClip;
        reproductor.clip = clip;
        reproductor.renderMode = VideoRenderMode.APIOnly;
        reproductor.audioOutputMode = VideoAudioOutputMode.None;
        reproductor.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        reproductor.errorReceived += (_, mensaje) => errorPreparacion = mensaje;

        reproductor.Prepare();

        float espera = 0f;
        while (!reproductor.isPrepared && errorPreparacion == null && !saltar && espera < EsperaMaximaPreparacion)
        {
            espera += Time.unscaledDeltaTime;
            yield return null;
        }

        if (saltar || !reproductor.isPrepared)
        {
            if (!saltar)
                Debug.LogWarning("CinematicaIntro: no se pudo preparar el video. " + errorPreparacion, this);
            CargarEscena();
            yield break;
        }

        MostrarTextura();

        bool fin = false;
        reproductor.loopPointReached += _ => fin = true;
        reproductor.Play();
        MostrarTextura();

        float sinImagen = 0f;
        int framesParado = 0;
        bool yaReproduciendo = false;

        while (!fin && !saltar)
        {
            MostrarTextura();

            if (reproductor.isPlaying)
            {
                yaReproduciendo = true;
                framesParado = 0;
            }

            if (!yaReproduciendo)
            {
                sinImagen += Time.unscaledDeltaTime;
                if (sinImagen >= EsperaMaximaArranque)
                    break;
            }
            else if (!reproductor.isPlaying)
            {
                framesParado++;
                if (framesParado > 5)
                    break;
            }

            yield return null;
        }

        if (reproductor.isPlaying)
            reproductor.Pause();

        CargarEscena();
    }

    void MostrarTextura()
    {
        if (imagenVideo == null || reproductor == null || reproductor.texture == null)
            return;

        if (imagenVideo.texture != reproductor.texture)
        {
            imagenVideo.texture = reproductor.texture;
            imagenVideo.color = Color.white;
        }
    }

    void CrearPantalla()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject fondo = new GameObject("Fondo", typeof(RectTransform));
        fondo.transform.SetParent(transform, false);
        Estirar(fondo.GetComponent<RectTransform>());
        Image negro = fondo.AddComponent<Image>();
        negro.color = Color.black;
        negro.raycastTarget = true;

        GameObject video = new GameObject("Video", typeof(RectTransform));
        video.transform.SetParent(fondo.transform, false);
        Estirar(video.GetComponent<RectTransform>());
        imagenVideo = video.AddComponent<RawImage>();
        imagenVideo.color = new Color(1f, 1f, 1f, 0f);
        imagenVideo.raycastTarget = false;

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null)
            fuente = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (fuente == null)
            return;

        GameObject aviso = new GameObject("AvisoSaltar", typeof(RectTransform));
        aviso.transform.SetParent(transform, false);
        RectTransform rect = aviso.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-48f, 36f);
        rect.sizeDelta = new Vector2(640f, 48f);

        Text texto = aviso.AddComponent<Text>();
        texto.font = fuente;
        texto.fontSize = 28;
        texto.alignment = TextAnchor.MiddleRight;
        texto.color = new Color(1f, 1f, 1f, 0.82f);
        texto.raycastTarget = false;
        texto.text = "Escape para saltar";
    }

    static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static VideoClip CargarClipPorDefecto()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(RutaClip);
#else
        return null;
#endif
    }

    void CargarEscena()
    {
        if (string.IsNullOrEmpty(escenaDestino))
        {
            Debug.LogError("CinematicaIntro: falta el nombre de la escena.", this);
            return;
        }

        SceneManager.LoadScene(escenaDestino);
    }

    void OnDestroy()
    {
        EnCurso = false;
    }
}
