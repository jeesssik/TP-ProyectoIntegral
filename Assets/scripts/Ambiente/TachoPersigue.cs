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
    Collider2D colisionGolpe;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        colisionGolpe = GetComponent<Collider2D>();

        if (framesGiro == null || framesGiro.Length == 0)
            framesGiro = CargarFrames();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            if (framesGiro != null && framesGiro.Length > 0)
                spriteRenderer.sprite = framesGiro[0];
        }

        if (colisionGolpe != null)
            colisionGolpe.enabled = false;
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
        if (colisionGolpe != null)
            colisionGolpe.enabled = false;
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
        if (colisionGolpe != null)
            colisionGolpe.enabled = true;
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
        Collider2D col = g.GetComponent<Collider2D>();
        if (col != null && col.enabled)
            return col.bounds.min.x;
        return g.transform.position.x;
    }

    void EmpezarCaida()
    {
        estado = Estado.Cayendo;
        velocidadCaidaY = 0f;
        if (colisionGolpe != null)
            colisionGolpe.enabled = false;
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
        if (colisionGolpe != null)
            colisionGolpe.enabled = false;

        g.MorirPorContainer();
        Invoke(nameof(ReiniciarEscena), Grace.DuracionAnimacionMuerte);
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
