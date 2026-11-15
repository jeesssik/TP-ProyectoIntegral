using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tótem: al acercarse/pasarlo, cierra la zona invertida, pasa a verde y arma la cascada.
/// Sin collider sólido.
/// </summary>
public class CheckpointTotem : MonoBehaviour
{
    const string RecursosTerminal = "Terminales industriales_ rojo y verde";

    [Header("Visual")]
    public SpriteRenderer totem;
    public Sprite spriteApagado;   // rojo
    public Sprite spriteActivado;  // verde

    [Header("Detección")]
    [Tooltip("Activa cuando Grace llega a esta distancia a la izquierda del borde izquierdo del tótem.")]
    public float anticipoActivacion = 0.8f;

    [Header("Cascada")]
    public PlataformaQueCae[] plataformas;

    [Header("Castigo por saltar el láser")]
    public Transform containerColgado;
    public float velocidadCaidaContainer = 16f;

    bool yaTocado;
    Grace grace;

    void Awake()
    {
        if (totem == null)
            totem = GetComponent<SpriteRenderer>();

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        CargarSpritesSiFaltan();

        if (totem != null && spriteApagado != null)
        {
            totem.color = Color.white;
            totem.sprite = spriteApagado;
        }
    }

    void Start()
    {
        grace = FindObjectOfType<Grace>();
    }

    void Update()
    {
        if (!yaTocado)
            IntentarActivar();
    }

    void IntentarActivar()
    {
        if (grace == null)
        {
            grace = FindObjectOfType<Grace>();
            if (grace == null)
                return;
        }

        if (GruaZona.ZonaAlcanzada)
        {
            yaTocado = true;
            EntradaJuego.InvertidoHorizontal = false;
            LaserInversionControles laserYaPasado = LaserInversionControles.Instancia;
            if (laserYaPasado != null)
                laserYaPasado.CerrarZona();
            if (totem != null && spriteActivado != null)
            {
                totem.color = Color.white;
                totem.sprite = spriteActivado;
            }
            return;
        }

        if (!GraceLlegoAlTotem())
            return;

        yaTocado = true;
        EntradaJuego.InvertidoHorizontal = false;

        LaserInversionControles laser = LaserInversionControles.Instancia;
        if (laser != null)
            laser.CerrarZona();

        if (laser != null && !laser.FueImpactado)
        {
            StartCoroutine(SoltarContainerSobreGrace());
            return;
        }

        if (totem != null && spriteActivado != null)
        {
            totem.color = Color.white;
            totem.sprite = spriteActivado;
        }

        ArmarCascada();
    }

    bool GraceLlegoAlTotem()
    {
        float xActivacion = transform.position.x - anticipoActivacion;
        float yMin = transform.position.y - 2f;
        float yMax = transform.position.y + 12f;

        if (totem != null && totem.sprite != null)
        {
            Bounds b = totem.bounds;
            // Antes del borde izquierdo: controles normales y pasa libre.
            xActivacion = b.min.x - anticipoActivacion;
            yMin = b.min.y - 2f;
            yMax = b.max.y + 2f;
        }

        Vector3 p = grace.transform.position;
        return p.x >= xActivacion && p.y >= yMin && p.y <= yMax;
    }

    /// <summary>Borde izquierdo del tótem: ahí termina la zona invertida del láser.</summary>
    public float XFinZonaInvertida()
    {
        if (totem != null && totem.sprite != null)
            return totem.bounds.min.x;
        return transform.position.x;
    }

    IEnumerator SoltarContainerSobreGrace()
    {
        Transform caja = containerColgado;
        if (caja == null)
        {
            GameObject hallado = GameObject.Find("container Cae_0");
            if (hallado != null)
                caja = hallado.transform;
        }

        if (caja == null || grace == null)
        {
            grace.MorirPorContainer();
            Invoke(nameof(ReiniciarEscena), Grace.DuracionAnimacionMuerte);
            yield break;
        }

        SpriteRenderer sprite = caja.GetComponent<SpriteRenderer>();
        float suelo = AlturaDelPiso(caja.position);
        float xFijo = caja.position.x;
        float duracionCaida = 0f;
        while (true)
        {
            duracionCaida += Time.deltaTime;
            Vector3 p = caja.position;
            p.x = xFijo;
            p.y -= velocidadCaidaContainer * Time.deltaTime;
            caja.position = p;

            float baseCaja = sprite != null ? sprite.bounds.min.y : p.y;
            if (baseCaja <= suelo + 0.08f || p.y < suelo - 2f)
                break;
            yield return null;
        }

        // En el clip el cuerpo llega al piso cerca de 1.05 s. Un poco más rápido para que caiga con el impacto.
        const float clipHastaElPiso = 1.05f;
        float velocidadMuerte = Mathf.Clamp(clipHastaElPiso / Mathf.Max(duracionCaida, 0.2f) * 1.35f, 0.75f, 3.5f);
        if (grace != null)
            grace.MorirAplastadaPorContainer(velocidadMuerte);
        Invoke(nameof(ReiniciarEscena), Grace.DuracionAnimacionMuerte / velocidadMuerte);
    }

    float AlturaDelPiso(Vector3 desde)
    {
        RaycastHit2D golpe = Physics2D.Raycast(desde, Vector2.down, 40f, 1 << 6);
        if (golpe.collider != null)
            return golpe.point.y;
        return grace != null ? grace.transform.position.y : -4f;
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ArmarCascada()
    {
        // Todas las PlataformaQueCae del nivel, no solo las del array del inspector.
        // Duplicar una plataforma alcanza; no hace falta volver a arrastrarla al tótem.
        PlataformaQueCae[] todas = FindObjectsOfType<PlataformaQueCae>();
        for (int i = 0; i < todas.Length; i++)
        {
            if (todas[i] != null)
                todas[i].Armar();
        }
    }

    void CargarSpritesSiFaltan()
    {
        if (spriteApagado != null && spriteActivado != null)
            return;

        Sprite[] todos = Resources.LoadAll<Sprite>(RecursosTerminal);
        if (todos == null || todos.Length == 0)
            return;

        if (spriteApagado == null)
            spriteApagado = Buscar(todos, "_0");
        if (spriteActivado == null)
            spriteActivado = Buscar(todos, "_4");
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
