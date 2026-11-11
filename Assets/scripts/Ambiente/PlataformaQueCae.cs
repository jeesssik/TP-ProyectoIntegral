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
        Bounds zona = BoundsDeLaPlataforma();
        SpriteRenderer[] renders = FindObjectsOfType<SpriteRenderer>();

        for (int i = 0; i < renders.Length; i++)
        {
            SpriteRenderer render = renders[i];
            if (!EsPilote(render.gameObject))
                continue;

            Transform pilote = render.transform;
            if (pilote == transform || pilote.IsChildOf(transform))
                continue;

            if (pilote.GetComponentInParent<PlataformaQueCae>() != null)
                continue;

            if (!CubreEnX(zona, pilote.position.x))
                continue;

            if (!EstaCercaEnVertical(zona, render.bounds))
                continue;

            if (DuenaMasCercana(pilote.position.x) != this)
                continue;

            pilote.SetParent(transform, true);
        }
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

    static bool CubreEnX(Bounds zona, float x)
    {
        return x >= zona.min.x - 0.8f && x <= zona.max.x + 0.8f;
    }

    static bool EstaCercaEnVertical(Bounds zona, Bounds pilote)
    {
        float arriba = zona.max.y + 1.5f;
        float abajo = zona.min.y - 3f;
        return pilote.max.y >= abajo && pilote.min.y <= arriba;
    }

    PlataformaQueCae DuenaMasCercana(float x)
    {
        PlataformaQueCae mejor = null;
        float mejorDistancia = float.PositiveInfinity;
        PlataformaQueCae[] todas = FindObjectsOfType<PlataformaQueCae>();

        for (int i = 0; i < todas.Length; i++)
        {
            PlataformaQueCae otra = todas[i];
            Bounds zona = otra.BoundsDeLaPlataforma();
            if (!CubreEnX(zona, x))
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

        if (p.y > yDesaparecer)
            return;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        gameObject.SetActive(false);
    }
}
