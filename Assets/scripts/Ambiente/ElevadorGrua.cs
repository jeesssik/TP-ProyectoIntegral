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
    float yHiloInicio;
    float yContainerInicio;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        BoxCollider2D piso = GetComponent<BoxCollider2D>();

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
