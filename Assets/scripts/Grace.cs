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
    public float gravedadEscala = 3f;
    public float multiplicadorCaida = 1.6f;
    [Range(0f, 1f)]
    public float controlAire = 0.28f;
    public float tiempoIgnorarSueloTrasSalto = 0.18f;

    [Header("Detección de suelo")]
    public Transform groundCheck;
    public Vector2 tamanoSuelo = new Vector2(0.7f, 0.22f);
    public LayerMask capaSuelo;

    [Header("Ola / muerte")]
    public Vector2 impulsoOla = new Vector2(9.5f, 2.2f);
    public float yMuerteMar = -7f;

    Rigidbody2D rb;
    Animator animator;
    Collider2D colision;
    Quaternion rotacionInicial;
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

    public bool EstaCorriendo => estaCorriendo;
    public bool EstaEnElAire => !estaEnSuelo;
    public bool VieneDeUnSalto => Time.time - marcaSalto < 1.35f;
    public Collider2D ColisionCuerpo => colision;
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
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;
        rotacionInicial = transform.localRotation;
        ConfigurarFisica();
    }

    void Start()
    {
        if (rb != null)
            rb.gravityScale = gravedadEscala;

        estaEnSuelo = TocaSuelo();
        estabaEnSuelo = estaEnSuelo;
        ActualizarAnimacion();
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
        estabaEnSuelo = estaEnSuelo;
    }

    void FixedUpdate()
    {
        if (enCinematica || controlBloqueado || rb == null || yaMurio)
            return;

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
        float velocidadVertical = rb.velocity.y;
        if (velocidadVertical < 0f)
            velocidadVertical += Physics2D.gravity.y * (multiplicadorCaida - 1f) * gravedadEscala * Time.fixedDeltaTime;

        rb.velocity = new Vector2(velocidadAire, velocidadVertical);
    }

    void ConfigurarFisica()
    {
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravedadEscala;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;

        colision = GetComponent<Collider2D>();
        if (colision == null)
        {
            CapsuleCollider2D capsule = gameObject.AddComponent<CapsuleCollider2D>();
            Vector3 escala = transform.lossyScale;
            float escalaX = Mathf.Max(Mathf.Abs(escala.x), 0.0001f);
            float escalaY = Mathf.Max(Mathf.Abs(escala.y), 0.0001f);
            capsule.size = new Vector2(0.75f / escalaX, 2.4f / escalaY);
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.offset = new Vector2(0f, 1.2f / escalaY);
            colision = capsule;
        }
        colision.enabled = true;
        colision.isTrigger = false;
    }

    bool PuedeSaltar()
    {
        return estaEnSuelo || Time.time - ultimoSueloTime < 0.08f;
    }

    void EjecutarSalto()
    {
        estaEnSuelo = false;
        estabaEnSuelo = false;
        ignorarSueloHasta = Time.time + tiempoIgnorarSueloTrasSalto;
        rb.velocity = new Vector2(rb.velocity.x, fuerzaSalto);
        marcaSalto = Time.time;
        if (animator != null)
        {
            animator.ResetTrigger("Land");
            animator.SetTrigger("Jump");
        }
    }

    bool TocaSuelo()
    {
        if (Time.time < ignorarSueloHasta || (rb != null && rb.velocity.y > 1f))
            return false;

        Bounds bounds = colision != null ? colision.bounds : new Bounds(transform.position, Vector3.one);
        // El GroundCheck original pertenece al personaje 2D y tiene offsets
        // basados en el sprite. Para el modelo 3D usamos la base del collider.
        Vector2 origen = new Vector2(bounds.center.x, bounds.min.y - tamanoSuelo.y * 0.15f);
        return Physics2D.OverlapBox(origen, tamanoSuelo, 0f, capaSuelo) != null;
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

        animator.SetFloat("Speed", Mathf.Abs(movimiento));
        animator.SetBool("IsGrounded", estaEnSuelo);
        animator.SetBool("IsRunning", estaCorriendo && movimiento != 0f);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);

    }

    void ActualizarOrientacion()
    {
        if (Mathf.Abs(movimiento) < 0.01f)
            return;

        transform.localRotation = rotacionInicial * Quaternion.Euler(0f, movimiento < 0f ? 180f : 0f, 0f);
    }

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
        }

        transform.SetParent(asiento, true);
        transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        transform.position = asiento.TransformPoint(offsetPiesLocal);
    }

    public void EmpujadaPorOla()
    {
        if (controlBloqueado || enCinematica || rb == null)
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
        Bounds bounds = colision != null ? colision.bounds : new Bounds(transform.position, Vector3.one);
        Vector3 origen = new Vector3(bounds.center.x, bounds.min.y - tamanoSuelo.y * 0.15f, bounds.center.z);
        Gizmos.DrawWireCube(origen, tamanoSuelo);
    }
}
