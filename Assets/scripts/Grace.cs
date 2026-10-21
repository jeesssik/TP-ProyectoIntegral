using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Grace : MonoBehaviour
{
    public const float DuracionAnimacionMuerte = 1.65f;

    [Header("Movimiento")]
    public float velocidadCaminar = 3f;
    public float velocidadCorrer = 7f;

    [Header("Salto / gravedad")]
    public float fuerzaSalto = 11.5f;
    [Tooltip("Escala de gravedad del Rigidbody2D. 1 = lunar para este tamaño; 3 cae como persona.")]
    public float gravedadEscala = 3f;
    [Tooltip("Extra de gravedad solo al caer (más de 1 = cae más rápido de lo que sube).")]
    public float multiplicadorCaida = 1.6f;
    [Range(0f, 1f)]
    public float controlAire = 0.28f;
    [Tooltip("Tras saltar, ignorar suelo un ratito para que el Overlap no cancele el salto.")]
    public float tiempoIgnorarSueloTrasSalto = 0.18f;

    [Header("Detección de suelo")]
    public Transform groundCheck;
    public Vector2 tamanoSuelo = new Vector2(0.7f, 0.22f);
    public LayerMask capaSuelo;

    [Header("Ola / muerte")]
    public Vector2 impulsoOla = new Vector2(9.5f, 2.2f);
    public float yMuerteMar = -7f;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Animator animator;
    Collider2D colision;
    PolygonCollider2D colliderCuerpo;
    readonly Dictionary<int, Vector2[]> cacheFormasSprite = new Dictionary<int, Vector2[]>();
    readonly List<Vector2> bufferForma = new List<Vector2>(64);
    readonly List<Vector2> bufferAplicar = new List<Vector2>(64);
    Sprite spriteColliderActual;
    bool colliderEspejado;
    bool colliderAnclado;
    float pieLocalY = float.NaN;
    Vector2[] formaReposo;

    float movimiento;
    bool estaEnSuelo;
    bool estabaEnSuelo;
    bool estaCorriendo;
    bool controlBloqueado;
    bool enCinematica;
    bool yaMurio;
    bool saltoPedido;
    float ignorarSueloHasta;
    float ultimoSueloTime = -10f;
    float marcaSalto = -100f;
    float rotacionInicialMuerte;
    float sentidoCaidaMuerte = 1f;
    Vector3 pivotePiesMundo;
    Vector3 offsetDesdePies;

    public bool EstaCorriendo => estaCorriendo;
    public bool EstaEnElAire => !estaEnSuelo;
    public bool VieneDeUnSalto => Time.time - marcaSalto < 1.35f;
    public Collider2D ColisionCuerpo => colision;

    /// <summary>
    /// Salto / aire real: no confía solo en el overlap de suelo (a veces falla).
    /// </summary>
    public bool EstaEsquivandoOla =>
        !estaEnSuelo ||
        Time.time < ignorarSueloHasta ||
        (rb != null && rb.velocity.y > 0.5f);

    public bool EstaFueraDelAlcanceDeLaOla(float xCuandoEmpezoLaOla, float distanciaEsquivaAtras)
    {
        if (controlBloqueado)
            return false;

        if (transform.position.x < xCuandoEmpezoLaOla - distanciaEsquivaAtras)
            return true;

        bool vaAtras =
            (rb != null && rb.velocity.x < -0.35f) ||
            EntradaJuego.Horizontal < -0.1f;

        return !estaEnSuelo && vaAtras;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        ConfigurarColliderCuerpo();

        if (rb != null)
        {
            rb.gravityScale = gravedadEscala;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Start()
    {
        if (rb != null)
            rb.gravityScale = gravedadEscala;

        estaEnSuelo = TocaSuelo();
        estabaEnSuelo = estaEnSuelo;
    }

    void Update()
    {
        if (enCinematica)
            return;

        if (transform.position.y < yMuerteMar)
        {
            MorirPorCaidaAlMar();
            return;
        }

        if (controlBloqueado)
            return;

        movimiento = EntradaJuego.Horizontal;
        estaCorriendo = EntradaJuego.Corre;

        if (EntradaJuego.Salto)
            saltoPedido = true;

        ActualizarSuelo();
        ActualizarAnimacion();
        ActualizarOrientacion();
        SincronizarColliderConSprite();

        estabaEnSuelo = estaEnSuelo;
    }

    void LateUpdate()
    {
        if (enCinematica || yaMurio)
            return;

        SincronizarColliderConSprite();
    }

    void FixedUpdate()
    {
        if (!enCinematica && !yaMurio)
            SincronizarColliderConSprite();

        if (enCinematica || controlBloqueado || rb == null)
            return;

        // Por si algo del inspector o la física lo pisó.
        rb.gravityScale = gravedadEscala;

        if (saltoPedido)
        {
            saltoPedido = false;
            if (PuedeSaltar())
                EjecutarSalto();
        }

        if (EntradaJuego.ConsumirFrenoPorCambioDeZona())
        {
            movimiento = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        float velocidadObjetivo = movimiento * (estaCorriendo ? velocidadCorrer : velocidadCaminar);

        if (estaEnSuelo)
        {
            rb.velocity = new Vector2(velocidadObjetivo, rb.velocity.y);
            return;
        }

        float velocidadAire = Mathf.Lerp(rb.velocity.x, velocidadObjetivo, controlAire);
        float vy = rb.velocity.y;

        // Caer más rápido que subir (gravedad asimétrica).
        if (vy < 0f)
            vy += Physics2D.gravity.y * (multiplicadorCaida - 1f) * gravedadEscala * Time.fixedDeltaTime;

        rb.velocity = new Vector2(velocidadAire, vy);
    }

    bool PuedeSaltar()
    {
        // Coyote corto: permite saltar un frame después de dejar el borde.
        return estaEnSuelo || Time.time - ultimoSueloTime < 0.08f;
    }

    void EjecutarSalto()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Land");
            animator.SetTrigger("Jump");
            animator.SetBool("IsGrounded", false);
        }

        estaEnSuelo = false;
        estabaEnSuelo = false;
        ignorarSueloHasta = Time.time + tiempoIgnorarSueloTrasSalto;

        // Separar un toque del piso para que el solver de contactos no mate la velocidad vertical.
        Vector2 pos = rb.position;
        pos.y += 0.1f;
        rb.position = pos;
        rb.velocity = new Vector2(rb.velocity.x, fuerzaSalto);
        marcaSalto = Time.time;
        spriteColliderActual = null;
        SincronizarColliderConSprite();
    }

    bool TocaSuelo()
    {
        if (Time.time < ignorarSueloHasta)
            return false;

        // Subiendo: no contar suelo. El polígono de un frame de salto no puede
        // marcar Idle/Land con los pies visuales en el aire.
        if (rb != null && rb.velocity.y > 1f)
            return false;

        return Physics2D.OverlapBox(OrigenSuelo(), tamanoSuelo, 0f, capaSuelo);
    }

    Vector2 OrigenSuelo()
    {
        float yLocal = !float.IsNaN(pieLocalY) ? pieLocalY : -3.72f;
        float xLocal = 0.66f;
        if (spriteRenderer != null && spriteRenderer.flipX)
            xLocal = -xLocal;

        if (groundCheck != null)
        {
            Vector3 local = groundCheck.localPosition;
            local.x = xLocal;
            local.y = yLocal - tamanoSuelo.y * 0.15f;
            groundCheck.localPosition = local;
            return groundCheck.position;
        }

        return transform.TransformPoint(new Vector3(xLocal, yLocal - tamanoSuelo.y * 0.15f, 0f));
    }

    void ActualizarSuelo()
    {
        estaEnSuelo = TocaSuelo();
        if (estaEnSuelo)
            ultimoSueloTime = Time.time;

        if (estabaEnSuelo || !estaEnSuelo)
            return;

        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.SetTrigger("Land");
        }

        if (movimiento == 0f && rb != null)
            rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    void ActualizarAnimacion()
    {
        if (animator == null || rb == null)
            return;

        float speedParam = movimiento != 0f ? Mathf.Abs(movimiento) : 0f;
        animator.SetFloat("Speed", speedParam);
        animator.SetBool("IsGrounded", estaEnSuelo);
        animator.SetBool("IsRunning", estaCorriendo && movimiento != 0f);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);
    }

    void ActualizarOrientacion()
    {
        if (spriteRenderer == null)
            return;

        if (movimiento > 0f)
            spriteRenderer.flipX = false;
        else if (movimiento < 0f)
            spriteRenderer.flipX = true;
    }

    void ConfigurarColliderCuerpo()
    {
        BoxCollider2D caja = GetComponent<BoxCollider2D>();
        if (caja != null)
            caja.enabled = false;

        colliderCuerpo = GetComponent<PolygonCollider2D>();
        if (colliderCuerpo == null)
            colliderCuerpo = gameObject.AddComponent<PolygonCollider2D>();

        colliderCuerpo.autoTiling = false;
        colliderCuerpo.isTrigger = false;
        colliderCuerpo.enabled = true;
        colliderCuerpo.offset = Vector2.zero;
        colision = colliderCuerpo;
        SincronizarColliderConSprite();
    }

    void SincronizarColliderConSprite()
    {
        if (colliderCuerpo == null || !colliderCuerpo.enabled)
            return;
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Sprite sprite = spriteRenderer.sprite;
        bool flip = spriteRenderer.flipX;
        bool anclar = estaEnSuelo && !float.IsNaN(pieLocalY);
        if (sprite == spriteColliderActual && flip == colliderEspejado && anclar == colliderAnclado)
            return;

        Vector2[] forma = ObtenerFormaSprite(sprite);
        if (forma == null || forma.Length < 3)
            return;

        bufferAplicar.Clear();
        for (int i = 0; i < forma.Length; i++)
        {
            Vector2 p = forma[i];
            if (flip)
                p.x = -p.x;
            bufferAplicar.Add(p);
        }

        if (flip)
            bufferAplicar.Reverse();

        // En suelo los frames de walk/run cambian el min.y y eso dispara Jump/Land.
        // Anclar pies mantiene el contacto; en el aire usamos la silueta real (piernas recogidas).
        if (anclar)
        {
            float minY = bufferAplicar[0].y;
            for (int i = 1; i < bufferAplicar.Count; i++)
            {
                if (bufferAplicar[i].y < minY)
                    minY = bufferAplicar[i].y;
            }

            float dy = pieLocalY - minY;
            if (Mathf.Abs(dy) > 0.001f)
            {
                for (int i = 0; i < bufferAplicar.Count; i++)
                {
                    Vector2 p = bufferAplicar[i];
                    p.y += dy;
                    bufferAplicar[i] = p;
                }
            }
        }

        colliderCuerpo.pathCount = 1;
        colliderCuerpo.SetPath(0, bufferAplicar);
        spriteColliderActual = sprite;
        colliderEspejado = flip;
        colliderAnclado = anclar;
    }

    Vector2[] ObtenerFormaSprite(Sprite sprite)
    {
        int id = sprite.GetInstanceID();
        if (cacheFormasSprite.TryGetValue(id, out Vector2[] cache))
            return cache;

        int n = sprite.GetPhysicsShapeCount();
        if (n > 0)
        {
            int mejor = 0;
            float mejorArea = -1f;
            for (int i = 0; i < n; i++)
            {
                bufferForma.Clear();
                sprite.GetPhysicsShape(i, bufferForma);
                float area = AreaAbsoluta(bufferForma);
                if (area > mejorArea)
                {
                    mejorArea = area;
                    mejor = i;
                }
            }

            bufferForma.Clear();
            sprite.GetPhysicsShape(mejor, bufferForma);
            SimplificarForma(bufferForma, 0.04f);
            cache = bufferForma.ToArray();
        }

        if (cache == null || !FormaUsable(cache))
            cache = formaReposo;

        if (cache == null)
            cache = FormaCuerpoPorDefecto();

        cacheFormasSprite[id] = cache;
        RecordarPieSiHaceFalta(cache);
        if (formaReposo == null && FormaUsable(cache))
            formaReposo = cache;
        return cache;
    }

    static bool FormaUsable(Vector2[] forma)
    {
        if (forma == null || forma.Length < 3)
            return false;

        float minX = forma[0].x, maxX = forma[0].x, minY = forma[0].y, maxY = forma[0].y;
        for (int i = 1; i < forma.Length; i++)
        {
            minX = Mathf.Min(minX, forma[i].x);
            maxX = Mathf.Max(maxX, forma[i].x);
            minY = Mathf.Min(minY, forma[i].y);
            maxY = Mathf.Max(maxY, forma[i].y);
        }

        // El canvas de Grace es 19.2×10.8; si la physics shape sale de eso, es basura.
        return maxX - minX <= 2.6f && maxY - minY <= 6.2f;
    }

    static Vector2[] FormaCuerpoPorDefecto()
    {
        return new[]
        {
            new Vector2(0.28f, -3.64f),
            new Vector2(1.08f, -3.64f),
            new Vector2(1.12f, -0.45f),
            new Vector2(1.05f, 0.55f),
            new Vector2(0.72f, 1.08f),
            new Vector2(0.42f, 0.55f),
            new Vector2(0.34f, -0.45f)
        };
    }

    void RecordarPieSiHaceFalta(Vector2[] forma)
    {
        if (!float.IsNaN(pieLocalY) || forma == null || forma.Length == 0)
            return;

        float minY = forma[0].y;
        for (int i = 1; i < forma.Length; i++)
        {
            if (forma[i].y < minY)
                minY = forma[i].y;
        }

        pieLocalY = minY;
    }

    static float AreaAbsoluta(List<Vector2> pts)
    {
        float a = 0f;
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 p = pts[i];
            Vector2 q = pts[(i + 1) % n];
            a += p.x * q.y - q.x * p.y;
        }

        return Mathf.Abs(a) * 0.5f;
    }

    static void SimplificarForma(List<Vector2> pts, float minDist)
    {
        if (pts.Count < 5)
            return;

        float minSqr = minDist * minDist;
        int w = 1;
        for (int i = 1; i < pts.Count - 1; i++)
        {
            if ((pts[i] - pts[w - 1]).sqrMagnitude >= minSqr)
            {
                pts[w] = pts[i];
                w++;
            }
        }

        pts[w] = pts[pts.Count - 1];
        w++;
        if (w < 3)
            return;

        pts.RemoveRange(w, pts.Count - w);
    }

    /// <param name="offsetPiesLocal">Donde van los pies, en espacio local del asiento.</param>
    /// <param name="ordenSorting">Orden de dibujo (menor que la lancha = detrás del casco).</param>
    public void SubirseALancha(Transform asiento, Vector3 offsetPiesLocal, int ordenSorting = 4)
    {
        if (yaMurio || enCinematica)
            return;

        enCinematica = true;
        controlBloqueado = true;
        movimiento = 0f;
        estaCorriendo = false;
        saltoPedido = false;

        if (colision != null)
            colision.enabled = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.simulated = false;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsGrounded", true);
            animator.SetBool("IsRunning", false);
            animator.SetFloat("VerticalSpeed", 0f);
            animator.ResetTrigger("Jump");
            animator.ResetTrigger("Land");
            animator.Play("graceIdle", 0, 0f);
        }

        Vector3 piesMundo = asiento.TransformPoint(offsetPiesLocal);
        Vector3 offsetPiesDesdeCentro = EstimarOffsetPiesDesdeCentro();

        transform.SetParent(asiento, true);
        // En escena Grace usa ~180° en Y; identity la deja mirando al revés de la lancha.
        transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        transform.position = piesMundo - offsetPiesDesdeCentro;

        if (spriteRenderer != null)
        {
            // Misma convención que caminar a la derecha (la lancha avanza +X).
            spriteRenderer.flipX = false;
            spriteRenderer.sortingOrder = ordenSorting;
        }
    }

    /// <summary>
    /// Offset mundo desde el transform hasta los pies visibles (contenido opaco del sprite).
    /// Los frames de Grace tienen mucho padding transparente.
    /// </summary>
    Vector3 EstimarOffsetPiesDesdeCentro()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return Vector3.zero;

        Sprite s = spriteRenderer.sprite;
        // Contenido medido en idle/0001: pies ~175px desde abajo del rect 1080, pivot al centro.
        const float pieDesdeAbajoPx = 175f;
        float altura = s.rect.height;
        float pivotY = s.pivot.y;
        float pieDesdePivotPx = pieDesdeAbajoPx - pivotY;
        float unidades = pieDesdePivotPx / s.pixelsPerUnit;
        float escalaY = Mathf.Abs(transform.lossyScale.y);
        return new Vector3(0f, unidades * escalaY, 0f);
    }

    public void EmpujadaPorOla()
    {
        if (controlBloqueado || enCinematica)
            return;

        controlBloqueado = true;
        movimiento = 0f;
        saltoPedido = false;

        if (colision != null)
            colision.enabled = false;

        if (animator != null)
            animator.SetBool("IsGrounded", false);

        rb.gravityScale = gravedadEscala;
        rb.velocity = impulsoOla;
    }

    public void MorirPorContainer()
    {
        if (yaMurio)
            return;

        yaMurio = true;
        controlBloqueado = true;
        movimiento = 0f;
        saltoPedido = false;

        // La animación Dead ya desploma el cuerpo en el sprite.
        // No rotar el transform: eso la enterraba y dejaba los pies colgando.
        if (colision != null)
            colision.enabled = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsGrounded", true);
            animator.SetTrigger("Dead");
        }
    }

    void MorirPorCaidaAlMar()
    {
        if (yaMurio)
            return;

        yaMurio = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(OrigenSuelo(), tamanoSuelo);
    }
}
