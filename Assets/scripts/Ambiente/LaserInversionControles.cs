using System;
using UnityEngine;

/// <summary>
/// Láser apagado (verde) deja pasar. Al cruzarlo hacia adelante: se enciende (rojo),
/// se vuelve sólido y abre la zona de controles invertidos hasta el tótem.
/// La inversión dura solo mientras Grace está dentro de esa zona.
/// </summary>
public class LaserInversionControles : MonoBehaviour
{
    public static LaserInversionControles Instancia { get; private set; }

    const string RecursosEmisor = "Emisores compactos con haz rojo ajustado";

    [Header("Visual")]
    public SpriteRenderer emisor;
    public Sprite spriteApagado;   // verde
    public Sprite spriteEncendido; // rojo

    [Header("Zona invertida")]
    [Tooltip("Límite derecho de la zona (el tótem/checkpoint).")]
    public Transform limiteZonaDerecha;
    [Tooltip("Activa el láser e inversión esta distancia antes del centro (unidades mundo).")]
    public float anticipoActivacion = 0.45f;

    [Header("Collider existente (no crea uno nuevo)")]
    public Collider2D colision;

    bool barreraActiva;
    bool zonaCerrada;
    Grace grace;

    void Awake()
    {
        Instancia = this;
        EntradaJuego.InvertidoHorizontal = false;
        barreraActiva = false;
        zonaCerrada = false;

        if (emisor == null)
            emisor = GetComponent<SpriteRenderer>();

        // Solo usa el collider que ya esté en el objeto; no agrega ni reemplaza.
        if (colision == null)
            colision = GetComponent<Collider2D>();

        CargarSpritesSiFaltan();
        AplicarVisual(encendido: false);

        if (colision != null)
            colision.isTrigger = true;
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    void Start()
    {
        grace = FindObjectOfType<Grace>();
    }

    void Update()
    {
        IntentarActivarPorAnticipo();
        ActualizarInversionPorZona();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (barreraActiva || zonaCerrada)
            return;

        Grace g = other.GetComponent<Grace>();
        if (g == null)
            g = other.GetComponentInParent<Grace>();
        if (g == null)
            return;

        // Al tocar el haz (antes de terminarlo de cruzar).
        if (g.transform.position.x < transform.position.x - anticipoActivacion)
            return;

        ActivarBarrera();
    }

    void IntentarActivarPorAnticipo()
    {
        if (barreraActiva || zonaCerrada)
            return;

        if (grace == null)
        {
            grace = FindObjectOfType<Grace>();
            if (grace == null)
                return;
        }

        // Unos pixels antes del centro del emisor.
        if (grace.transform.position.x >= transform.position.x - anticipoActivacion)
            ActivarBarrera();
    }

    void ActivarBarrera()
    {
        if (barreraActiva)
            return;

        barreraActiva = true;
        AplicarVisual(encendido: true);

        // El mismo collider deja de ser trigger y tapa el paso atrás.
        if (colision != null)
            colision.isTrigger = false;
    }

    /// <summary>Lo llama el tótem al activarse: se corta la inversión, el láser sigue rojo/sólido.</summary>
    public void CerrarZona()
    {
        zonaCerrada = true;
        EntradaJuego.InvertidoHorizontal = false;
    }

    void ActualizarInversionPorZona()
    {
        if (!barreraActiva || zonaCerrada)
        {
            EntradaJuego.InvertidoHorizontal = false;
            return;
        }

        if (grace == null)
        {
            grace = FindObjectOfType<Grace>();
            if (grace == null)
            {
                EntradaJuego.InvertidoHorizontal = false;
                return;
            }
        }

        float xMin = transform.position.x - anticipoActivacion;
        float xMax = ObtenerXLimiteDerecho();

        float x = grace.transform.position.x;
        // Invertido desde un poco antes del láser hasta el tótem.
        EntradaJuego.InvertidoHorizontal = x >= xMin && x < xMax;
    }

    float ObtenerXLimiteDerecho()
    {
        if (limiteZonaDerecha == null)
            return float.PositiveInfinity;

        // Termina la inversión en el borde izquierdo del tótem, no en el centro:
        // así Grace llega con controles normales y lo pasa sin trabarse.
        var checkpoint = limiteZonaDerecha.GetComponent<CheckpointTotem>();
        if (checkpoint != null)
            return checkpoint.XFinZonaInvertida();

        var sr = limiteZonaDerecha.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            return sr.bounds.min.x;

        return limiteZonaDerecha.position.x;
    }

    void AplicarVisual(bool encendido)
    {
        if (emisor == null)
            return;

        Sprite nuevo = encendido ? spriteEncendido : spriteApagado;
        if (nuevo == null)
            return;

        // Solo Y: el haz rojo ensancha el bounds y si anclamos en X el poste se corre.
        SetSpriteSinCorrerse(emisor, nuevo, soloVertical: true);
    }

    /// <summary>
    /// Cambia el sprite manteniendo el pie anclado.
    /// Por defecto ancla centro-abajo (X e Y). Con soloVertical evita corrimiento por haces asimétricos.
    /// </summary>
    public static void SetSpriteSinCorrerse(SpriteRenderer sr, Sprite nuevo, bool soloVertical = false)
    {
        if (sr == null || nuevo == null)
            return;

        if (sr.sprite == nuevo)
        {
            sr.color = Color.white;
            return;
        }

        Vector3 pieAntes = new Vector3(sr.bounds.center.x, sr.bounds.min.y, sr.transform.position.z);
        sr.color = Color.white;
        sr.sprite = nuevo;
        Vector3 pieDespues = new Vector3(sr.bounds.center.x, sr.bounds.min.y, sr.transform.position.z);
        Vector3 delta = pieAntes - pieDespues;
        if (soloVertical)
            delta.x = 0f;
        sr.transform.position += delta;
    }

    void CargarSpritesSiFaltan()
    {
        if (spriteApagado != null && spriteEncendido != null)
            return;

        Sprite[] todos = Resources.LoadAll<Sprite>(RecursosEmisor);
        if (todos == null || todos.Length == 0)
            return;

        // _1 = verde (apagado), _0 = rojo con haz (encendido).
        if (spriteApagado == null)
            spriteApagado = Buscar(todos, "_1");
        if (spriteEncendido == null)
            spriteEncendido = Buscar(todos, "_0");
    }

    static Sprite Buscar(Sprite[] sprites, string sufijo)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.EndsWith(sufijo, StringComparison.Ordinal))
                return sprites[i];
        }
        return null;
    }
}
