using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zona entre el láser y el tótem: controles invertidos.
/// Se puede entrar y salir (también de un salto). Al cruzar el borde se conserva
/// la dirección medio segundo; después frena y responde al mapeo nuevo.
/// </summary>
[DefaultExecutionOrder(-50)]
public class LaserInversionControles : MonoBehaviour
{
    public static LaserInversionControles Instancia { get; private set; }

    const string RecursosEmisor = "Emisores compactos con haz rojo ajustado";

    [Header("Visual")]
    public SpriteRenderer emisor;
    public Sprite spriteApagado;
    public Sprite spriteEncendido;

    [Header("Zona invertida")]
    [Tooltip("Límite derecho de la zona (el tótem/checkpoint).")]
    public Transform limiteZonaDerecha;
    [Tooltip("Hay que salir esta distancia a la izquierda del láser para apagar la inversión.")]
    public float margenSalida = 0.7f;

    [Header("Collider existente (no crea uno nuevo)")]
    public Collider2D colision;

    [Header("Muerte")]
    [Tooltip("Objeto laser-die: el collider o el sprite marca exactamente dónde mata al pisar.")]
    public Transform laserDie;

    bool invertido;
    bool zonaCerrada;
    bool hazMortal;
    bool yaMato;
    float prenderRojoEn = -1f;
    Grace grace;

    void Awake()
    {
        Instancia = this;
        EntradaJuego.InvertidoHorizontal = false;
        EntradaJuego.SoltarDireccionFija();
        invertido = false;
        zonaCerrada = false;
        hazMortal = false;
        prenderRojoEn = -1f;

        if (emisor == null)
            emisor = GetComponent<SpriteRenderer>();

        if (colision == null)
            colision = GetComponent<Collider2D>();

        if (laserDie == null)
            laserDie = transform.Find("laser-die");

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
        EntradaJuego.ActualizarDireccionFija();
        ActualizarInversionPorZona();
        ActualizarHazMortal();
        IntentarMatarPorHaz();
    }

    public void CerrarZona()
    {
        zonaCerrada = true;
        invertido = false;
        hazMortal = false;
        prenderRojoEn = -1f;
        EntradaJuego.InvertidoHorizontal = false;
        EntradaJuego.SoltarDireccionFija();
        AplicarVisual(encendido: false);
    }

    void ActualizarInversionPorZona()
    {
        if (zonaCerrada)
        {
            invertido = false;
            EntradaJuego.InvertidoHorizontal = false;
            return;
        }

        if (grace == null)
        {
            grace = FindObjectOfType<Grace>();
            if (grace == null)
            {
                invertido = false;
                EntradaJuego.InvertidoHorizontal = false;
                return;
            }
        }

        bool enZona = EstaEnZona(grace.transform.position.x);
        if (enZona == invertido)
        {
            EntradaJuego.InvertidoHorizontal = invertido;
            return;
        }

        float dirMundo = EntradaJuego.Horizontal;
        invertido = enZona;
        EntradaJuego.InvertidoHorizontal = invertido;
        EntradaJuego.ConservarDireccionMundo(dirMundo);

        if (invertido && !hazMortal && prenderRojoEn < 0f)
            prenderRojoEn = Time.time + EntradaJuego.DuracionConservarDireccion;
    }

    void ActualizarHazMortal()
    {
        if (zonaCerrada || hazMortal || prenderRojoEn < 0f)
            return;
        if (Time.time < prenderRojoEn)
            return;

        hazMortal = true;
        AplicarVisual(encendido: true);
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    bool EstaEnZona(float x)
    {
        float xEntrada = XBordeLaser();
        float xSalida = xEntrada - margenSalida;
        float xMax = ObtenerXLimiteDerecho();

        if (invertido)
            return x >= xSalida && x < xMax;

        return x >= xEntrada && x < xMax;
    }

    float XBordeLaser()
    {
        if (colision != null)
            return colision.bounds.min.x;
        return transform.position.x;
    }

    float ObtenerXLimiteDerecho()
    {
        if (limiteZonaDerecha == null)
            return float.PositiveInfinity;

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

        emisor.color = Color.white;
        emisor.sprite = nuevo;
    }

    void IntentarMatarPorHaz()
    {
        if (!hazMortal || zonaCerrada || yaMato || grace == null)
            return;

        Rigidbody2D rb = grace.GetComponent<Rigidbody2D>();
        if (rb != null && rb.velocity.y > 1.5f)
            return;

        if (laserDie == null)
            laserDie = transform.Find("laser-die");
        if (laserDie == null)
            return;

        Collider2D zonaCol = laserDie.GetComponent<Collider2D>();
        Collider2D colGrace = grace.ColisionCuerpo;
        if (zonaCol == null || !zonaCol.enabled || colGrace == null || !colGrace.enabled)
            return;

        Vector2 pie = new Vector2(colGrace.bounds.center.x, colGrace.bounds.min.y + 0.06f);
        Vector2 tamPie = new Vector2(0.22f, 0.16f);

        var filtro = new ContactFilter2D();
        filtro.NoFilter();
        filtro.useTriggers = true;

        var hits = new Collider2D[8];
        int n = Physics2D.OverlapBox(pie, tamPie, 0f, filtro, hits);
        bool toca = false;
        for (int i = 0; i < n; i++)
        {
            if (hits[i] == zonaCol)
            {
                toca = true;
                break;
            }
        }

        if (!toca)
            return;

        yaMato = true;
        grace.MorirPorContainer();
        Invoke(nameof(ReiniciarEscena), Grace.DuracionAnimacionMuerte);
    }

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
