using UnityEngine;

public class PuenteRompible : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public BoxCollider2D colliderPuente;
    public SpriteRenderer spriteRenderer;
    public Sprite spriteRoto;
    public bool soloCasiRompe;

    [Header("Detección")]
    public float umbralImpactoSalto = -6.5f;
    public float tiempoHastaColapso = 0.2f;

    [Header("Collider curvo")]
    [Tooltip("Cuánto baja el centro de las tablas respecto al puente sano.")]
    public float profundidadCurva = 0.58f;
    public float espesorTablas = 0.45f;

    static readonly float[] Hundimiento =
    {
        0.00f, 0.06f, 0.22f, 0.52f, 0.88f, 1.00f, 0.72f, 0.32f, 0.08f, 0.00f
    };

    bool agrietado;
    bool colapsado;
    Grace jugadorSobrePuente;
    PolygonCollider2D colliderHundido;
    Vector2[] curvaLocal;
    float ignorarAterrizajeHasta;

    void Awake()
    {
        if (colliderPuente == null)
            colliderPuente = GetComponent<BoxCollider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
            animator.enabled = false;

        EdgeCollider2D colliderCurvo = GetComponent<EdgeCollider2D>();
        if (colliderCurvo != null)
            colliderCurvo.enabled = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluarJugador(collision, true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        EvaluarJugador(collision, false);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Grace>() == jugadorSobrePuente)
            jugadorSobrePuente = null;
    }

    void Update()
    {
        if (colapsado || jugadorSobrePuente == null)
            return;

        if (!soloCasiRompe && DebeColapsarPorCorrer(jugadorSobrePuente))
            IniciarColapso();
    }

    void EvaluarJugador(Collision2D collision, bool esEntrada)
    {
        if (colapsado)
            return;

        Grace grace = collision.gameObject.GetComponent<Grace>();
        if (grace == null || !EstaPisandoDesdeArriba(collision, grace))
            return;

        bool colapsarPorSalto =
            esEntrada &&
            Time.time >= ignorarAterrizajeHasta &&
            grace.VieneDeUnSalto &&
            DebeColapsarPorAterrizaje(collision.relativeVelocity.y);

        jugadorSobrePuente = grace;
        MostrarRoto();

        if (soloCasiRompe)
            return;

        if (colapsarPorSalto || DebeColapsarPorCorrer(grace))
            IniciarColapso();
    }

    Collider2D ColliderActivo()
    {
        if (colliderHundido != null && colliderHundido.enabled)
            return colliderHundido;
        return colliderPuente;
    }

    bool EstaPisandoDesdeArriba(Collision2D collision, Grace grace)
    {
        Collider2D activo = ColliderActivo();
        if (activo == null || !activo.enabled)
            return false;

        Rigidbody2D rb = collision.rigidbody;
        if (rb != null && rb.velocity.y > 1.2f)
            return false;

        Collider2D cuerpo = grace.ColisionCuerpo;
        float pieY = cuerpo != null && cuerpo.enabled
            ? cuerpo.bounds.min.y
            : collision.transform.position.y;

        Bounds cubierta = activo.bounds;
        const float insetBorde = 0.35f;
        float xMin = cubierta.min.x + insetBorde;
        float xMax = cubierta.max.x - insetBorde;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 punto = collision.GetContact(i).point;
            if (punto.x < xMin || punto.x > xMax)
                continue;

            float ySuperficie = AlturaMundoEnX(punto.x);
            if (punto.y < ySuperficie - 0.3f)
                continue;
            if (punto.y > pieY + 0.4f)
                continue;
            return true;
        }

        return false;
    }

    bool DebeColapsarPorCorrer(Grace grace)
    {
        if (soloCasiRompe || grace == null)
            return false;

        return grace.EstaCorriendo && EntradaJuego.HayMovimientoHorizontal;
    }

    bool DebeColapsarPorAterrizaje(float impactoVertical)
    {
        if (soloCasiRompe)
            return false;

        return impactoVertical < umbralImpactoSalto;
    }

    void MostrarRoto()
    {
        if (agrietado)
            return;

        agrietado = true;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play("casiRompe", 0, 0f);
        }

        if (!soloCasiRompe && spriteRenderer != null && spriteRoto != null)
            spriteRenderer.sprite = SpriteAlineadoAlSano(spriteRenderer.sprite, spriteRoto);

        AplicarColliderHundido();
        RecostarJugador(jugadorSobrePuente);
        ignorarAterrizajeHasta = Time.time + 0.4f;
    }

    void AplicarColliderHundido()
    {
        if (colliderPuente == null)
            return;

        Vector2 offset = colliderPuente.offset;
        Vector2 size = colliderPuente.size;
        float yTop = offset.y + size.y * 0.5f;
        float xMin = offset.x - size.x * 0.5f + 0.12f;
        float xMax = offset.x + size.x * 0.5f - 0.12f;

        curvaLocal = new Vector2[Hundimiento.Length];
        for (int i = 0; i < Hundimiento.Length; i++)
        {
            float t = i / (Hundimiento.Length - 1f);
            curvaLocal[i] = new Vector2(
                Mathf.Lerp(xMin, xMax, t),
                yTop - profundidadCurva * Hundimiento[i]);
        }

        var path = new Vector2[curvaLocal.Length * 2];
        for (int i = 0; i < curvaLocal.Length; i++)
            path[i] = curvaLocal[i];
        for (int i = 0; i < curvaLocal.Length; i++)
        {
            Vector2 p = curvaLocal[curvaLocal.Length - 1 - i];
            path[curvaLocal.Length + i] = new Vector2(p.x, p.y - espesorTablas);
        }

        if (colliderHundido == null)
            colliderHundido = gameObject.AddComponent<PolygonCollider2D>();

        colliderHundido.isTrigger = false;
        colliderHundido.pathCount = 1;
        colliderHundido.SetPath(0, path);
        colliderHundido.enabled = true;
        colliderPuente.enabled = false;
    }

    void RecostarJugador(Grace grace)
    {
        if (grace == null)
            return;

        Rigidbody2D rb = grace.GetComponent<Rigidbody2D>();
        Collider2D cuerpo = grace.ColisionCuerpo;
        if (rb == null || cuerpo == null)
            return;

        float pieY = cuerpo.bounds.min.y;
        float ySuperficie = AlturaMundoEnX(cuerpo.bounds.center.x);
        float dy = ySuperficie - pieY;
        if (Mathf.Abs(dy) < 0.02f || dy > 1.4f || dy < -1.4f)
            return;

        rb.position = new Vector2(rb.position.x, rb.position.y + dy);
        if (rb.velocity.y > 0f)
            rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    float AlturaMundoEnX(float xMundo)
    {
        if (colliderPuente == null)
            return transform.position.y;

        Vector2 offset = colliderPuente.offset;
        Vector2 size = colliderPuente.size;
        float yTop = offset.y + size.y * 0.5f;
        float xMin = offset.x - size.x * 0.5f + 0.12f;
        float xMax = offset.x + size.x * 0.5f - 0.12f;

        float xLocal = transform.InverseTransformPoint(new Vector3(xMundo, 0f, 0f)).x;
        float t = Mathf.InverseLerp(xMin, xMax, xLocal);
        t = Mathf.Clamp01(t);

        float f = t * (Hundimiento.Length - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, Hundimiento.Length - 2);
        float u = f - i;
        float h = Mathf.Lerp(Hundimiento[i], Hundimiento[i + 1], u);
        float yLocal = yTop - profundidadCurva * h;
        if (!agrietado)
            yLocal = yTop;

        return transform.TransformPoint(new Vector3(xLocal, yLocal, 0f)).y;
    }

    static Sprite SpriteAlineadoAlSano(Sprite sano, Sprite roto)
    {
        float pivotY = roto.pivot.y / roto.rect.height;

        if (sano != null && sano.texture == roto.texture && roto.rect.height > 0f)
        {
            float yTexturaPivoteSano = sano.rect.y + sano.pivot.y;
            pivotY = (yTexturaPivoteSano - roto.rect.y) / roto.rect.height;
        }

        return Sprite.Create(
            roto.texture,
            roto.rect,
            new Vector2(0.5f, pivotY),
            roto.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect
        );
    }

    void IniciarColapso()
    {
        if (colapsado)
            return;

        colapsado = true;
        MostrarRoto();
        Invoke(nameof(Desaparecer), tiempoHastaColapso);
    }

    void Desaparecer()
    {
        if (colliderPuente != null)
            colliderPuente.enabled = false;

        if (colliderHundido != null)
            colliderHundido.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}
