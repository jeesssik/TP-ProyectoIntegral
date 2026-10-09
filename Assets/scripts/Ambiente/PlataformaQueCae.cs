using UnityEngine;

/// <summary>
/// Plataforma del tramo final: al pisarla (armada), cae tras un delay ajustable.
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
    bool armada;
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

        Grace g = other.GetComponent<Grace>();
        if (g == null)
            g = other.GetComponentInParent<Grace>();
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
        Collider2D colGrace = grace.GetComponent<Collider2D>();

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
