using UnityEngine;

/// <summary>
/// El container colgado y el hilo suben y bajan en el lugar donde están puestos.
/// No mueve la grúa ni a Grace. El paso lo cierra el Box Collider 2D del container.
/// </summary>
public class ElevadorGrua : MonoBehaviour
{
    public Transform hilo;
    public float periodo = 2f;
    public float yBaja = -1.22f;
    public float yAlta = 1.7f;
    public float yOrigenContainer = 6.2f;
    public float yOrigenHilo = 13.0859f;

    Rigidbody2D rb;
    BoxCollider2D piso;
    float yHiloInicio;
    float yContainerInicio;
    float atrapadaDesde = -1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        piso = GetComponent<BoxCollider2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.useFullKinematicContacts = true;
        }

        if (piso != null)
            piso.isTrigger = false;

        yContainerInicio = transform.position.y;
        yHiloInicio = hilo != null ? hilo.position.y : 0f;
        Aplicar(yBaja);
    }

    void FixedUpdate()
    {
        float fase = (Mathf.Sin(Time.time * (Mathf.PI * 2f / Mathf.Max(0.05f, periodo))) + 1f) * 0.5f;
        float y = Mathf.Lerp(yBaja, yAlta, fase);
        Aplicar(y);
        SoltarSiQuedoAtrapada();
    }

    // Si cae al costado, entre el container y el muelle, el contacto lateral
    // la deja quieta en el aire con la animación de salto. La saca del hueco.
    void SoltarSiQuedoAtrapada()
    {
        if (piso == null)
            return;

        Bounds caja = piso.bounds;
        Collider2D[] toques = Physics2D.OverlapBoxAll(caja.center, caja.size, 0f);
        bool atrapada = false;
        for (int i = 0; i < toques.Length; i++)
        {
            Grace grace = Grace.En(toques[i]);
            if (grace == null || !grace.EstaEnElAire)
                continue;

            Rigidbody2D cuerpo = grace.CuerpoRigido;
            if (cuerpo == null)
                continue;

            Collider2D cuerpoCol = grace.ColisionCuerpo;
            float pieY = cuerpoCol != null ? cuerpoCol.bounds.min.y : cuerpo.position.y;
            if (pieY >= caja.max.y - 0.08f)
                continue;

            atrapada = true;
            bool quieta = Mathf.Abs(cuerpo.velocity.y) < 0.45f && Mathf.Abs(cuerpo.velocity.x) < 0.5f;
            if (!quieta)
            {
                atrapadaDesde = -1f;
                continue;
            }

            if (atrapadaDesde < 0f)
                atrapadaDesde = Time.time;
            if (Time.time - atrapadaDesde < 0.12f)
                continue;

            float margen = (cuerpoCol != null ? cuerpoCol.bounds.extents.x : 0.3f) + 0.08f;
            float x = Mathf.Abs(cuerpo.position.x - caja.min.x) <= Mathf.Abs(cuerpo.position.x - caja.max.x)
                ? caja.min.x - margen
                : caja.max.x + margen;
            cuerpo.position = new Vector2(x, cuerpo.position.y);
            cuerpo.velocity = new Vector2(cuerpo.velocity.x, Mathf.Min(cuerpo.velocity.y, -2f));
            atrapadaDesde = -1f;
        }

        if (!atrapada)
            atrapadaDesde = -1f;
    }

    void Aplicar(float y)
    {
        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
        pos.y = y;
        if (rb != null)
            rb.MovePosition(pos);
        else
        {
            Vector3 p = transform.position;
            p.y = y;
            transform.position = p;
        }

        if (hilo != null)
        {
            Vector3 h = hilo.position;
            h.y = yHiloInicio + (y - yContainerInicio);
            hilo.position = h;
        }
    }
}
