using UnityEngine;

/// <summary>
/// Al pisarla, cae tras un delay. No depende del tótem.
/// Soporta collider sólido, trigger, o solo sprite (bounds).
/// </summary>
public class PlataformaQueCae : MonoBehaviour
{
    [Header("Cuándo cae")]
    [Tooltip("Segundos desde que Grace pisa la plataforma hasta que se suelta.")]
    public float delayTrasPisada = 1f;

    [Header("Caída")]
    public float gravedad = 26f;
    public float rotacionGrados = 55f;
    public float yDesaparecer = -16f;

    Collider2D colision;
    SpriteRenderer spriteRenderer;
    Grace grace;
    bool armada = true;
    bool cayendo;
    bool pendienteCaida;
    float tiempoPendiente;
    float velocidadY;
    float sentidoGiro = 1f;
    readonly System.Collections.Generic.List<Transform> pilotes =
        new System.Collections.Generic.List<Transform>();

    void Awake()
    {
        colision = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        sentidoGiro = Random.value < 0.5f ? -1f : 1f;
    }

    void Start()
    {
        grace = FindObjectOfType<Grace>();
        EngancharPilotes();
    }

    public void Armar()
    {
        armada = true;
    }

    public void Caer()
    {
        if (cayendo)
            return;

        cayendo = true;
        pendienteCaida = false;
        velocidadY = 0.8f;
        EngancharPilotes();

        if (colision != null)
            colision.enabled = false;
    }

    void Update()
    {
        if (cayendo)
        {
            ActualizarCaida();
            return;
        }

        if (!armada)
            return;

        if (!pendienteCaida && GraceEstaPisando())
        {
            pendienteCaida = true;
            tiempoPendiente = 0f;
        }

        if (!pendienteCaida)
            return;

        tiempoPendiente += Time.deltaTime;
        if (tiempoPendiente >= delayTrasPisada)
            Caer();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        RegistrarPisada(collision.collider);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        RegistrarPisada(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        RegistrarPisada(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        RegistrarPisada(other);
    }

    void RegistrarPisada(Collider2D other)
    {
        if (!armada || pendienteCaida || cayendo || other == null)
            return;

        Grace g = Grace.En(other);
        if (g == null)
            return;

        pendienteCaida = true;
        tiempoPendiente = 0f;
    }

    bool GraceEstaPisando()
    {
        if (grace == null)
        {
            grace = FindObjectOfType<Grace>();
            if (grace == null)
                return false;
        }

        Bounds zona = ObtenerZonaPiso();
        Collider2D colGrace = grace.ColisionCuerpo;

        float x = grace.transform.position.x;
        float pieY = colGrace != null && colGrace.enabled
            ? colGrace.bounds.min.y
            : grace.transform.position.y;

        bool enX = x >= zona.min.x - 0.2f && x <= zona.max.x + 0.2f;
        bool enY = pieY >= zona.min.y - 0.5f && pieY <= zona.max.y + 0.7f;
        return enX && enY;
    }

    Bounds ObtenerZonaPiso()
    {
        if (colision != null && colision.enabled)
        {
            Bounds b = colision.bounds;
            float alto = Mathf.Max(0.4f, b.size.y * 0.4f);
            return new Bounds(
                new Vector3(b.center.x, b.max.y - alto * 0.5f, b.center.z),
                new Vector3(b.size.x, alto, 1f));
        }

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds b = spriteRenderer.bounds;
            float alto = Mathf.Clamp(b.size.y * 0.25f, 0.4f, 1.4f);
            return new Bounds(
                new Vector3(b.center.x, b.max.y - alto * 0.5f, b.center.z),
                new Vector3(b.size.x * 0.85f, alto, 1f));
        }

        return new Bounds(transform.position, new Vector3(4f, 1f, 1f));
    }

    void EngancharPilotes()
    {
        Bounds zona = ZonaDeEnganche();
        SpriteRenderer[] renders = FindObjectsOfType<SpriteRenderer>();

        for (int i = 0; i < renders.Length; i++)
        {
            SpriteRenderer render = renders[i];
            if (render == null || !EsPilote(render.gameObject))
                continue;

            Transform pilote = render.transform;
            if (pilote == transform)
                continue;

            if (pilote.IsChildOf(transform))
            {
                Recordar(pilote);
                continue;
            }

            // Ya viaja con otra base que cae.
            PlataformaQueCae duena = pilote.GetComponentInParent<PlataformaQueCae>();
            if (duena != null && duena != this)
                continue;

            Bounds poste = EnPlano(render.bounds);
            if (!Solapa(zona, poste) && !Solapa(zona, EnPlano(new Bounds(pilote.position, Vector3.one * 1.6f))))
                continue;

            if (DuenaMasCercana(poste.center.x) != this)
                continue;

            pilote.SetParent(transform, true);
            Recordar(pilote);
        }
    }

    void Recordar(Transform pilote)
    {
        if (!pilotes.Contains(pilote))
            pilotes.Add(pilote);
    }

    Bounds ZonaDeEnganche()
    {
        Bounds zona = EnPlano(BoundsDeLaPlataforma());
        if (colision != null)
        {
            Bounds piso = EnPlano(colision.bounds);
            zona.Encapsulate(piso.min);
            zona.Encapsulate(piso.max);
        }

        // El poste cuelga varios metros bajo la cubierta.
        zona.Encapsulate(new Vector3(zona.center.x, zona.min.y - 6f, zona.center.z));
        zona.Expand(new Vector3(2.4f, 1.2f, 0f));
        return zona;
    }

    static Bounds EnPlano(Bounds b)
    {
        return new Bounds(new Vector3(b.center.x, b.center.y, 0f), new Vector3(b.size.x, b.size.y, 4f));
    }

    static bool Solapa(Bounds zona, Bounds pilote)
    {
        return pilote.max.x >= zona.min.x && pilote.min.x <= zona.max.x
            && pilote.max.y >= zona.min.y && pilote.min.y <= zona.max.y;
    }

    static bool EsPilote(GameObject objeto)
    {
        return objeto.name.StartsWith("pilote", System.StringComparison.OrdinalIgnoreCase);
    }

    Bounds BoundsDeLaPlataforma()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.bounds;

        if (colision != null)
            return colision.bounds;

        return new Bounds(transform.position, new Vector3(5f, 4f, 1f));
    }

    PlataformaQueCae DuenaMasCercana(float x)
    {
        PlataformaQueCae mejor = null;
        float mejorDistancia = float.PositiveInfinity;
        PlataformaQueCae[] todas = FindObjectsOfType<PlataformaQueCae>();

        for (int i = 0; i < todas.Length; i++)
        {
            PlataformaQueCae otra = todas[i];
            if (otra == null)
                continue;

            Bounds zona = otra.ZonaDeEnganche();
            if (x < zona.min.x || x > zona.max.x)
                continue;

            float distancia = Mathf.Abs(otra.transform.position.x - x);
            if (distancia >= mejorDistancia)
                continue;

            mejorDistancia = distancia;
            mejor = otra;
        }

        return mejor;
    }

    void ActualizarCaida()
    {
        velocidadY -= gravedad * Time.deltaTime;

        Vector3 p = transform.position;
        p.y += velocidadY * Time.deltaTime;
        p.x += sentidoGiro * 0.35f * Time.deltaTime;
        transform.position = p;
        transform.Rotate(0f, 0f, sentidoGiro * rotacionGrados * Time.deltaTime);

        for (int i = 0; i < pilotes.Count; i++)
        {
            Transform pilote = pilotes[i];
            if (pilote != null && pilote.parent != transform)
                pilote.SetParent(transform, true);
        }

        if (p.y > yDesaparecer)
            return;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        gameObject.SetActive(false);
    }
}
