using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prop hasta que Grace lo pasa; después gira (frames) y persigue.
/// </summary>
public class TachoPersigue : MonoBehaviour
{
    const string RecursosBarril = "Barril rodante con muescas rotatorias";

    [Header("Activación")]
    public float margenPaso = 0.55f;
    public float tiempoAviso = 0.65f;
    public float velocidadAviso = 1.2f;

    [Header("Persecución")]
    public float velocidadInicial = 4.6f;
    public float velocidadMaxima = 6.1f;
    public float aceleracion = 1.1f;

    [Header("Caída al vacío")]
    public float xBordeCaida = 35.2f;
    public float gravedadCaida = 28f;
    public float impulsoCaidaX = 2.5f;
    public float yDesaparecer = -14f;

    [Header("Animación")]
    public Sprite[] framesGiro;
    public float fpsAviso = 8f;
    public float fpsPersiguiendo = 14f;
    public float fpsCaida = 18f;

    enum Estado { Prop, Aviso, Persiguiendo, Cayendo, Muerto }

    Estado estado = Estado.Prop;
    float tiempoEnAviso;
    float velocidad;
    float velocidadCaidaY;
    float tiempoAnim;
    int indiceFrame;
    Grace grace;

    SpriteRenderer spriteRenderer;
    Collider2D[] colisionesGolpe;
    ParticleSystem fuego;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        colisionesGolpe = GetComponents<Collider2D>();

        if (framesGiro == null || framesGiro.Length == 0)
            framesGiro = CargarFrames();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            if (framesGiro != null && framesGiro.Length > 0)
                spriteRenderer.sprite = framesGiro[0];
        }

        SetColisiones(false);
        CrearFuego();
    }

    void Start()
    {
        grace = FindObjectOfType<Grace>();
    }

    void Update()
    {
        if (estado == Estado.Muerto)
            return;

        if (estado == Estado.Prop)
        {
            if (GracePasoCompletamente())
                EmpezarAviso();
            return;
        }

        if (estado == Estado.Aviso)
        {
            ActualizarAviso();
            return;
        }

        if (estado == Estado.Cayendo)
        {
            ActualizarCaida();
            return;
        }

        if (estado != Estado.Persiguiendo)
            return;

        velocidad = Mathf.MoveTowards(velocidad, velocidadMaxima, aceleracion * Time.deltaTime);
        Mover(velocidad);
        AvanzarAnimacion(fpsPersiguiendo);

        if (transform.position.x >= xBordeCaida)
            EmpezarCaida();
    }

    void EmpezarAviso()
    {
        estado = Estado.Aviso;
        tiempoEnAviso = 0f;
        tiempoAnim = 0f;
        SetColisiones(false);
        EncenderFuego();
    }

    void ActualizarAviso()
    {
        tiempoEnAviso += Time.deltaTime;
        Mover(velocidadAviso);
        AvanzarAnimacion(fpsAviso);

        if (tiempoEnAviso < tiempoAviso)
            return;

        estado = Estado.Persiguiendo;
        velocidad = velocidadInicial;
        SetColisiones(true);
    }

    void Mover(float vel)
    {
        transform.position += Vector3.right * (vel * Time.deltaTime);
    }

    void AvanzarAnimacion(float fps)
    {
        if (framesGiro == null || framesGiro.Length == 0 || spriteRenderer == null)
            return;

        tiempoAnim += Time.deltaTime;
        float paso = 1f / Mathf.Max(0.01f, fps);
        while (tiempoAnim >= paso)
        {
            tiempoAnim -= paso;
            indiceFrame = (indiceFrame + 1) % framesGiro.Length;
            spriteRenderer.sprite = framesGiro[indiceFrame];
        }
    }

    bool GracePasoCompletamente()
    {
        if (grace == null)
        {
            grace = FindObjectOfType<Grace>();
            if (grace == null)
                return false;
        }

        float xDerecha = ObtenerXMaxima();
        return ObtenerXMinima(grace) > xDerecha + margenPaso;
    }

    float ObtenerXMaxima()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.bounds.max.x;
        return transform.position.x + 0.9f;
    }

    static float ObtenerXMinima(Grace g)
    {
        Collider2D col = g.ColisionCuerpo;
        if (col != null && col.enabled)
            return col.bounds.min.x;
        return g.transform.position.x;
    }

    void EmpezarCaida()
    {
        estado = Estado.Cayendo;
        velocidadCaidaY = 0f;
        SetColisiones(false);
    }

    void ActualizarCaida()
    {
        velocidadCaidaY -= gravedadCaida * Time.deltaTime;
        Vector3 p = transform.position;
        p.x += impulsoCaidaX * Time.deltaTime;
        p.y += velocidadCaidaY * Time.deltaTime;
        transform.position = p;
        AvanzarAnimacion(fpsCaida);

        if (p.y > yDesaparecer)
            return;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (estado != Estado.Persiguiendo)
            return;

        Grace g = other.GetComponent<Grace>();
        if (g == null)
            g = other.GetComponentInParent<Grace>();
        if (g == null)
            return;

        estado = Estado.Muerto;
        SetColisiones(false);
        if (fuego != null)
            fuego.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        g.MorirPorContainer();
        Invoke(nameof(ReiniciarEscena), Grace.DuracionAnimacionMuerte);
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void SetColisiones(bool activas)
    {
        if (colisionesGolpe == null)
            return;

        for (int i = 0; i < colisionesGolpe.Length; i++)
        {
            if (colisionesGolpe[i] != null)
                colisionesGolpe[i].enabled = activas;
        }
    }

    void EncenderFuego()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.62f, 0.28f);

        if (fuego == null)
            return;

        fuego.Clear(true);
        fuego.Play(true);
    }

    void CrearFuego()
    {
        var go = new GameObject("Fuego");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        go.transform.localScale = new Vector3(
            1f / Mathf.Max(0.01f, Mathf.Abs(transform.localScale.x)),
            1f / Mathf.Max(0.01f, Mathf.Abs(transform.localScale.y)),
            1f);

        fuego = go.AddComponent<ParticleSystem>();
        var main = fuego.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 180;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5.5f, 9.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startColor = new Color(1f, 0.55f, 0.12f, 0.95f);
        main.gravityModifier = -0.7f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = fuego.emission;
        emission.enabled = true;
        emission.rateOverTime = 90f;

        var shape = fuego.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.28f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colorOverLifetime = fuego.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradiente = new Gradient();
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.45f), 0f),
                new GradientColorKey(new Color(1f, 0.38f, 0.04f), 0.4f),
                new GradientColorKey(new Color(0.35f, 0.04f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.75f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradiente;

        var sizeOverLifetime = fuego.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

        var renderer = fuego.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CrearMaterialFuego();

        fuego.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static Material CrearMaterialFuego()
    {
        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.mainTexture = CrearTexturaFuego();
        return material;
    }

    static Texture2D CrearTexturaFuego()
    {
        const int tam = 32;
        var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float centro = (tam - 1) * 0.5f;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = (x - centro) / centro;
                float dy = (y - centro) / centro;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return tex;
    }

    static Sprite[] CargarFrames()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(RecursosBarril);
        if (sprites == null || sprites.Length == 0)
            sprites = Resources.LoadAll<Sprite>("Tacho/giro");

        if (sprites == null || sprites.Length == 0)
            return Array.Empty<Sprite>();

        Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites;
    }
}
