using UnityEngine;

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

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private float movimiento;
    private bool estaEnSuelo;
    private bool estabaEnSuelo;
    private bool estaCorriendo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        estaEnSuelo = Physics2D.OverlapCircle(
            groundCheck.position,
            radioSuelo,
            capaSuelo
        );
        estabaEnSuelo = estaEnSuelo;
    }

    void Update()
    {
        movimiento = Input.GetAxisRaw("Horizontal");

        estaCorriendo =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        // DETECTAR SUELO
        bool tocaSuelo = Physics2D.OverlapCircle(
            groundCheck.position,
            radioSuelo,
            capaSuelo
        );

        estaEnSuelo = tocaSuelo && rb.velocity.y <= 0.1f;

        // DETECTAR ATERRIZAJE EXACTO
        if (!estabaEnSuelo && estaEnSuelo)
        {
            animator.SetTrigger("Land");

            // Si al aterrizar NO hay entrada del jugador, frenar en seco la inercia
            if (movimiento == 0)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        // SALTO
        if (Input.GetKeyDown(KeyCode.Space) && estaEnSuelo)
        {
            animator.SetTrigger("Jump");

            rb.velocity = new Vector2(
                rb.velocity.x,
                fuerzaSalto
            );

            estaEnSuelo = false;
        }

        // CONTROL DE ANIMADOR (Asegura 0 absoluto si no hay Input)
        float speedParam = (movimiento != 0) ? Mathf.Abs(movimiento) : 0f;
        animator.SetFloat("Speed", speedParam);

        animator.SetBool("IsGrounded", estaEnSuelo);
        animator.SetBool("IsRunning", estaCorriendo && movimiento != 0);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);

        // GIRAR PERSONAJE
        if (movimiento > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (movimiento < 0)
        {
            spriteRenderer.flipX = true;
        }

        estabaEnSuelo = estaEnSuelo;
    }

    void FixedUpdate()
    {
        float velocidadActual = estaCorriendo ? velocidadCorrer : velocidadCaminar;

        rb.velocity = new Vector2(
            movimiento * velocidadActual,
            rb.velocity.y
        );
    }
}