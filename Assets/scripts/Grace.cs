using UnityEngine;
using UnityEngine.SceneManagement;

public class Grace : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaminar = 3f;
    public float velocidadCorrer = 6f;

    [Header("Salto")]
    public float fuerzaSalto = 7f;

    [Header("Detección de suelo")]
    public Transform groundCheck;
    public float radioSuelo = 0.15f;
    public LayerMask capaSuelo;

    [Header("Ola / muerte")]
    public Vector2 impulsoOla = new Vector2(2.2f, 5.8f);
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
    float rotacionInicialMuerte;
    Vector3 pivotePiesMuerte;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        colision = GetComponent<Collider2D>();

        estaEnSuelo = TocaSuelo() && rb.velocity.y <= 0.1f;
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

        ActualizarSuelo();
        IntentarSaltar();
        ActualizarAnimacion();
        ActualizarOrientacion();

        estabaEnSuelo = estaEnSuelo;
    }

    void FixedUpdate()
    {
        if (controlBloqueado)
            return;

        float velocidadActual = estaCorriendo ? velocidadCorrer : velocidadCaminar;
        rb.velocity = new Vector2(movimiento * velocidadActual, rb.velocity.y);
    }

    bool TocaSuelo()
    {
        return Physics2D.OverlapCircle(groundCheck.position, radioSuelo, capaSuelo);
    }

    void ActualizarSuelo()
    {
        estaEnSuelo = TocaSuelo() && rb.velocity.y <= 0.1f;

        if (estabaEnSuelo || !estaEnSuelo)
            return;

        animator.SetTrigger("Land");

        if (movimiento == 0f)
            rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    void IntentarSaltar()
    {
        if (!EntradaJuego.Salto || !estaEnSuelo)
            return;

        animator.SetTrigger("Jump");
        rb.velocity = new Vector2(rb.velocity.x, fuerzaSalto);
        estaEnSuelo = false;
    }

    void ActualizarAnimacion()
    {
        float speedParam = movimiento != 0f ? Mathf.Abs(movimiento) : 0f;
        animator.SetFloat("Speed", speedParam);
        animator.SetBool("IsGrounded", estaEnSuelo);
        animator.SetBool("IsRunning", estaCorriendo && movimiento != 0f);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);
    }

    void ActualizarOrientacion()
    {
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

        if (colision != null)
            colision.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Jump");
            animator.SetBool("IsGrounded", false);
        }

        rb.velocity = impulsoOla;
    }

    public void MorirPorContainer()
    {
        if (yaMurio)
            return;

        yaMurio = true;
        controlBloqueado = true;
        movimiento = 0f;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            rb.angularVelocity = 0f;
            rotacionInicialMuerte = rb.rotation;
        }

        if (colision != null)
        {
            colision.enabled = true;
            Vector3 puntoPies = new Vector3(colision.bounds.center.x, colision.bounds.min.y, transform.position.z);
            pivotePiesMuerte = transform.InverseTransformPoint(puntoPies);
        }

        if (animator != null)
            animator.SetTrigger("Dead");

        StartCoroutine(RotarHastaQuedarRecostada());
    }

    System.Collections.IEnumerator RotarHastaQuedarRecostada()
    {
        const float duracion = 1.65f;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracion);
            float angulo = Mathf.LerpAngle(rotacionInicialMuerte, rotacionInicialMuerte + 90f, progreso);
            Vector3 pivoteMundo = transform.TransformPoint(pivotePiesMuerte);
            float giroRelativo = angulo - rotacionInicialMuerte;
            Vector3 desplazamientoPivote = Quaternion.Euler(0f, 0f, giroRelativo) * pivotePiesMuerte;

            if (rb != null)
            {
                rb.MoveRotation(angulo);
                rb.MovePosition(pivoteMundo - desplazamientoPivote);
            }

            yield return null;
        }

        if (rb != null)
        {
            float anguloFinal = rotacionInicialMuerte + 90f;
            Vector3 pivoteMundo = transform.TransformPoint(pivotePiesMuerte);
            Vector3 desplazamientoPivote = Quaternion.Euler(0f, 0f, 90f) * pivotePiesMuerte;
            rb.MoveRotation(anguloFinal);
            rb.MovePosition(pivoteMundo - desplazamientoPivote);
        }
    }

    void MorirPorCaidaAlMar()
    {
        if (yaMurio)
            return;

        yaMurio = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
