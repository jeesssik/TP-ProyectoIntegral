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
    public Vector2 impulsoOla = new Vector2(7.5f, 1.6f);
    public float yMuerteMar = -7f;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Animator animator;
    Collider2D colision;

    float movimiento;
    bool estaEnSuelo;
    bool estabaEnSuelo;
    bool estaCorriendo;
    bool controlBloqueado;
    bool yaMurio;
    bool saltoPedido;
    float ignorarSueloHasta;
    float ultimoSueloTime = -10f;
    float rotacionInicialMuerte;
    float sentidoCaidaMuerte = 1f;
    Vector3 pivotePiesMundo;
    Vector3 offsetDesdePies;

    public bool EstaCorriendo => estaCorriendo;
    public bool EstaEnElAire => !estaEnSuelo;

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
        colision = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.gravityScale = gravedadEscala;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (colision is BoxCollider2D caja && caja.edgeRadius < 0.05f)
            caja.edgeRadius = 0.12f;
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
        if (controlBloqueado || rb == null)
            return;

        // Por si algo del inspector o la física lo pisó.
        rb.gravityScale = gravedadEscala;

        if (saltoPedido)
        {
            saltoPedido = false;
            if (PuedeSaltar())
                EjecutarSalto();
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
    }

    bool TocaSuelo()
    {
        if (Time.time < ignorarSueloHasta)
            return false;

        Vector2 origen;
        if (colision != null)
            origen = new Vector2(colision.bounds.center.x, colision.bounds.min.y - tamanoSuelo.y * 0.5f);
        else if (groundCheck != null)
            origen = groundCheck.position;
        else
            return false;

        return Physics2D.OverlapBox(origen, tamanoSuelo, 0f, capaSuelo);
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
        if (spriteRenderer == null || !estaEnSuelo)
            return;

        if (movimiento > 0f)
            spriteRenderer.flipX = false;
        else if (movimiento < 0f)
            spriteRenderer.flipX = true;
    }

    public void EmpujadaPorOla()
    {
        if (controlBloqueado)
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

        sentidoCaidaMuerte = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;

        if (colision != null)
        {
            colision.enabled = true;
            pivotePiesMundo = new Vector3(colision.bounds.center.x, colision.bounds.min.y, transform.position.z);
        }
        else
        {
            pivotePiesMundo = transform.position;
        }

        offsetDesdePies = transform.position - pivotePiesMundo;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.None;
            rotacionInicialMuerte = rb.rotation;
        }

        if (animator != null)
            animator.SetTrigger("Dead");

        StartCoroutine(RotarHastaQuedarRecostada());
    }

    System.Collections.IEnumerator RotarHastaQuedarRecostada()
    {
        float tiempo = 0f;
        float anguloFinal = rotacionInicialMuerte + 90f * sentidoCaidaMuerte;

        while (tiempo < DuracionAnimacionMuerte)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / DuracionAnimacionMuerte);
            float angulo = Mathf.LerpAngle(rotacionInicialMuerte, anguloFinal, progreso);
            float giroRelativo = angulo - rotacionInicialMuerte;
            Vector3 nuevaPos = pivotePiesMundo + Quaternion.Euler(0f, 0f, giroRelativo) * offsetDesdePies;

            if (rb != null)
            {
                rb.MoveRotation(angulo);
                rb.MovePosition(nuevaPos);
            }

            yield return null;
        }

        if (rb != null)
        {
            rb.MoveRotation(anguloFinal);
            rb.MovePosition(pivotePiesMundo + Quaternion.Euler(0f, 0f, 90f * sentidoCaidaMuerte) * offsetDesdePies);
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
        Collider2D col = colision != null ? colision : GetComponent<Collider2D>();
        Vector3 origen;
        if (col != null)
            origen = new Vector3(col.bounds.center.x, col.bounds.min.y - tamanoSuelo.y * 0.5f, 0f);
        else if (groundCheck != null)
            origen = groundCheck.position;
        else
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origen, tamanoSuelo);
    }
}
