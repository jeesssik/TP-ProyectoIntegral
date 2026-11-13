using UnityEngine;
using UnityEngine.SceneManagement;

public class Grace : MonoBehaviour
{
    public const float DuracionAnimacionMuerte = 1.65f;
    const string EstadoCaidaOla = "Caida";

    [Header("Movimiento")]
    public float velocidadCaminar = 3f;
    public float velocidadCorrer = 7f;

    [Header("Salto / gravedad")]
    public float fuerzaSalto = 13.5f;
    public float impulsoLateralSalto = 7.5f;
    public float velocidadSaltoCorriendo = 8f;
    public float gravedadEscala = 3f;
    public float multiplicadorCaida = 1.6f;
    [Range(0f, 1f)]
    public float controlAire = 0.55f;
    public float tiempoIgnorarSueloTrasSalto = 0.18f;

    [Header("Detección de suelo")]
    public Transform groundCheck;
    public Vector2 tamanoSuelo = new Vector2(0.7f, 0.22f);
    public LayerMask capaSuelo;

    [Header("Ola / muerte")]
    public Vector2 impulsoOla = new Vector2(9.5f, 0f);
    public float yMuerteMar = -7f;
    [Tooltip("Ajuste vertical de la animacion de muerte para compensar margen transparente del sprite.")]
    public float ajusteAlturaMuerte = 0f;

    Rigidbody2D rb;
    Animator animator;
    Collider2D colision;
    SpriteRenderer rendererVisual;
    Quaternion rotacionInicial;
    float movimiento;
    bool estaEnSuelo;
    bool estabaEnSuelo;
    bool estaCorriendo;
    bool controlBloqueado;
    bool enCinematica;
    bool yaMurio;
    bool saltoPedido;
    bool lanzadaPorOla;
    bool lanzadaAlCielo;
    bool caidaAlMar;
    bool recargaCaidaPendiente;
    float proximoDesenganche;
    readonly ContactPoint2D[] contactosSuelo = new ContactPoint2D[8];
    const float ToleranciaDesnivel = 0.04f;
    float ignorarSueloHasta;
    float ultimoSueloTime = -10f;
    float marcaSalto = -100f;
    float saltoPedidoEn = -10f;
    float alturaPisoMuerte;
    bool ajustarMuerteAlPiso;
    float distanciaPies = -1.2f;
    const float planoVisualZ = -1f;
    const int ordenSobreEscenario = 30;

    Collider2D colliderCuerpo;
    Transform cuerpoFisico;
    SkinnedMeshRenderer[] mallas;

    public bool EstaCorriendo => estaCorriendo;
    public bool ListaParaLaGrua
    {
        get
        {
            if (!estaEnSuelo || enCinematica || yaMurio)
                return false;
            if (animator == null || animator.IsInTransition(0))
                return animator == null;

            AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(0);
            return !estado.IsName("JumpOver") && !estado.IsName("landJump");
        }
    }
    public bool EstaEnElAire => !estaEnSuelo;
    public bool EstaMoviendose => estaEnSuelo && !controlBloqueado && !enCinematica &&
        !yaMurio && Mathf.Abs(movimiento) > 0.01f;
    public bool VieneDeUnSalto => Time.time - marcaSalto < 1.35f;
    public bool SaltoReciente => Time.time - marcaSalto < 1.2f;
    public Collider2D ColisionCuerpo => colision;
    public Rigidbody2D CuerpoRigido => rb;

    public static Grace En(Component otro)
    {
        if (otro == null)
            return null;

        Grace propia = otro.GetComponent<Grace>();
        if (propia != null)
            return propia;

        Grace hija = otro.GetComponentInChildren<Grace>();
        if (hija != null)
            return hija;

        return otro.GetComponentInParent<Grace>();
    }
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
        animator = BuscarAnimador();
        rendererVisual = GetComponentInChildren<SpriteRenderer>();
        foreach (Animator animador in GetComponentsInChildren<Animator>(true))
            animador.applyRootMotion = false;
        rotacionInicial = RotacionDePerfil(transform.localRotation);
        ConfigurarFisica();
        transform.localRotation = rotacionInicial;
        AlinearVisual();
        PonerDelanteDelEscenario();
    }

    void Start()
    {
        if (rb != null)
            rb.gravityScale = gravedadEscala;

        estaEnSuelo = TocaSuelo();
        estabaEnSuelo = estaEnSuelo;
        ActualizarAnimacion();
    }

    void OnAnimatorMove()
    {
    }

    void LateUpdate()
    {
        if (!enCinematica || lanzadaAlCielo)
        {
            AlinearVisual();
            if (lanzadaPorOla || caidaAlMar || lanzadaAlCielo)
                AnclarPiesVisualesDuranteCaida();
        }

        if (!ajustarMuerteAlPiso || rendererVisual == null)
            return;

        float limiteInferior = ObtenerLimiteInferiorVisible();
        transform.position += Vector3.up *
            (alturaPisoMuerte + ajusteAlturaMuerte - limiteInferior);
    }

    float ObtenerLimiteInferiorVisible()
    {
        Sprite sprite = rendererVisual.sprite;
        if (sprite == null)
            return rendererVisual.bounds.min.y;

        Vector2[] vertices = sprite.vertices;
        if (vertices == null || vertices.Length == 0)
            return rendererVisual.bounds.min.y;

        float limiteInferior = float.PositiveInfinity;
        for (int i = 0; i < vertices.Length; i++)
        {
            float y = rendererVisual.transform.TransformPoint(vertices[i]).y;
            limiteInferior = Mathf.Min(limiteInferior, y);
        }

        return limiteInferior;
    }

    void Update()
    {
        if (lanzadaAlCielo)
        {
            MantenerAnimacionCaida();
            return;
        }

        if (enCinematica)
            return;

        // El origen del modelo queda en los pies. yMuerteMar en -5 está
        // a la altura del muelle, así que el primer puente, al hundirse, la mata.
        float lineaMar = Mathf.Min(yMuerteMar, -7.5f);
        float yCuerpo = cuerpoFisico != null ? cuerpoFisico.position.y : transform.position.y;
        if (lanzadaPorOla || caidaAlMar)
        {
            estaEnSuelo = false;
            MantenerAnimacionCaida();
        }
        if (!estaEnSuelo && yCuerpo < lineaMar)
        {
            if (caidaAlMar)
            {
                if (!recargaCaidaPendiente)
                {
                    recargaCaidaPendiente = true;
                    Invoke(nameof(MorirPorCaidaAlMar), 0.3f);
                }
                return;
            }

            MorirPorCaidaAlMar();
            return;
        }

        if (controlBloqueado)
            return;

        movimiento = EntradaJuego.Horizontal;
        estaCorriendo = EntradaJuego.Corre;

        if (EntradaJuego.Salto)
        {
            saltoPedido = true;
            saltoPedidoEn = Time.time;
        }

        ActualizarSuelo();
        ActualizarAnimacion();
        ActualizarOrientacion();
        estabaEnSuelo = estaEnSuelo;
    }

    void FixedUpdate()
    {
        if (lanzadaPorOla && rb != null && !yaMurio)
        {
            // Sin impulso hacia arriba: cae desde la altura de la plataforma.
            float vertical = Mathf.Min(0f, rb.velocity.y);
            rb.velocity = new Vector2(impulsoOla.x, vertical);
            return;
        }

        if (enCinematica || controlBloqueado || rb == null || yaMurio)
            return;

        rb.gravityScale = gravedadEscala;

        if (saltoPedido)
        {
            if (PuedeSaltar())
            {
                saltoPedido = false;
                EjecutarSalto();
            }
            else if (Time.time - saltoPedidoEn > 0.12f)
                saltoPedido = false;
        }

        if (EntradaJuego.ConsumirFrenoPorCambioDeZona())
        {
            movimiento = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        float velocidadObjetivo = VelocidadHorizontal();
        if (estaEnSuelo)
        {
            TolerarDesnivel(velocidadObjetivo);
            SubirEscalonSiHay(velocidadObjetivo);
            BajarConLaCurva(velocidadObjetivo);
            rb.velocity = VelocidadSobreLaCurva(velocidadObjetivo);
            return;
        }

        float velocidadAire = Mathf.Lerp(rb.velocity.x, velocidadObjetivo, controlAire);
        float velocidadVertical = rb.velocity.y;
        if (velocidadVertical < 0f)
            velocidadVertical += Physics2D.gravity.y * (multiplicadorCaida - 1f) * gravedadEscala * Time.fixedDeltaTime;

        rb.velocity = new Vector2(velocidadAire, velocidadVertical);
    }

    void TolerarDesnivel(float velocidadObjetivo)
    {
        if (Mathf.Abs(velocidadObjetivo) < 0.1f || Time.time < proximoDesenganche || colision == null)
            return;

        float sentido = Mathf.Sign(velocidadObjetivo);
        float pieY = colision.bounds.min.y;
        bool filo = HayFiloDelante(sentido, pieY);
        bool trabada = Mathf.Abs(rb.velocity.x) < 0.25f;
        if (!filo && !trabada)
            return;

        float frente = sentido > 0f ? colision.bounds.max.x : colision.bounds.min.x;
        Vector2 origen = new Vector2(frente + sentido * 0.05f, pieY + ToleranciaDesnivel + 0.12f);
        RaycastHit2D piso = Physics2D.Raycast(origen, Vector2.down, ToleranciaDesnivel + 0.2f, capaSuelo);
        if (piso.collider == null)
            return;

        float desnivel = piso.point.y - pieY;
        if (desnivel > ToleranciaDesnivel || desnivel < -0.008f)
            return;
        if (!filo && desnivel < 0.0004f)
            return;

        Vector2 rodilla = new Vector2(rb.position.x, pieY + 0.4f);
        RaycastHit2D pared = Physics2D.Raycast(rodilla, Vector2.right * sentido, 0.35f, capaSuelo);
        if (pared.collider != null && pared.normal.y < 0.45f)
            return;

        rb.position += new Vector2(sentido * 0.035f, Mathf.Max(0f, desnivel) + 0.006f);
        rb.velocity = new Vector2(velocidadObjetivo, 0f);
        proximoDesenganche = Time.time + 0.06f;
    }

    bool HayFiloDelante(float sentido, float pieY)
    {
        int cantidad = rb.GetContacts(contactosSuelo);
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 normal = contactosSuelo[i].normal;
            if (normal.x * sentido > -0.2f || normal.y > 0.55f)
                continue;
            if (contactosSuelo[i].point.y > pieY + ToleranciaDesnivel)
                continue;

            Collider2D otro = contactosSuelo[i].collider;
            if (otro == null || ((1 << otro.gameObject.layer) & capaSuelo.value) == 0)
                continue;
            return true;
        }

        return false;
    }

    void SubirEscalonSiHay(float velocidadObjetivo)
    {
        if (Mathf.Abs(velocidadObjetivo) < 0.1f || Time.time < proximoDesenganche)
            return;

        float sentido = Mathf.Sign(velocidadObjetivo);
        Vector2 pies = rb.position;
        Vector2 origen = pies + Vector2.up * 0.008f;
        RaycastHit2D cara = Physics2D.Raycast(origen, Vector2.right * sentido, 0.4f, capaSuelo);
        if (cara.collider == null || cara.normal.y > 0.85f)
            return;

        RaycastHit2D alto = Physics2D.Raycast(origen + Vector2.up * 0.16f, Vector2.right * sentido, 0.4f, capaSuelo);
        if (alto.collider != null)
            return;

        Vector2 encima = cara.point + new Vector2(sentido * 0.06f, 0.25f);
        RaycastHit2D pisoAlto = Physics2D.Raycast(encima, Vector2.down, 0.4f, capaSuelo);
        if (pisoAlto.collider == null)
            return;

        float desnivel = pisoAlto.point.y - pies.y;
        if (desnivel < 0.001f || desnivel > 0.14f)
            return;

        rb.position = new Vector2(cara.point.x + sentido * 0.14f, pisoAlto.point.y + 0.02f);
        proximoDesenganche = Time.time + 0.12f;
    }

    void BajarConLaCurva(float velocidadObjetivo)
    {
        if (Mathf.Abs(velocidadObjetivo) < 0.1f || Time.time < proximoDesenganche)
            return;

        float sentido = Mathf.Sign(velocidadObjetivo);
        Vector2 pies = rb.position;
        Vector2 origen = pies + Vector2.right * sentido * 0.22f + Vector2.up * 0.4f;
        RaycastHit2D suelo = Physics2D.Raycast(origen, Vector2.down, 0.85f, capaSuelo);
        if (suelo.collider == null)
            return;

        float desnivel = suelo.point.y - pies.y;
        if (desnivel > -0.012f || desnivel < -0.35f)
            return;

        Vector2 pecho = pies + Vector2.up * 0.5f;
        RaycastHit2D pared = Physics2D.Raycast(pecho, Vector2.right * sentido, 0.32f, capaSuelo);
        if (pared.collider != null && pared.normal.y < 0.45f)
            return;

        rb.position += Vector2.up * (desnivel + 0.015f);
    }

    Vector2 VelocidadSobreLaCurva(float velocidadObjetivo)
    {
        if (Mathf.Abs(velocidadObjetivo) < 0.01f)
            return new Vector2(0f, rb.velocity.y);

        Vector2 origen = rb.position + Vector2.up * 0.16f;
        RaycastHit2D suelo = Physics2D.Raycast(origen, Vector2.down, 0.5f, capaSuelo);
        if (suelo.collider == null || suelo.normal.y > 0.96f)
            return new Vector2(velocidadObjetivo, rb.velocity.y);

        Vector2 tangente = new Vector2(suelo.normal.y, -suelo.normal.x);
        if (tangente.x * velocidadObjetivo < 0f)
            tangente = -tangente;

        if (suelo.normal.y < 0.97f && Mathf.Abs(rb.velocity.x) < 0.35f && Time.time >= proximoDesenganche)
        {
            rb.position += tangente.normalized * 0.08f;
            proximoDesenganche = Time.time + 0.06f;
        }

        return tangente.normalized * Mathf.Abs(velocidadObjetivo);
    }

    float VelocidadHorizontal()
    {
        if (Mathf.Abs(movimiento) < 0.01f)
            return 0f;

        float enSuelo = estaCorriendo ? velocidadCorrer : velocidadCaminar;
        if (estaEnSuelo)
            return movimiento * enSuelo;

        float enAire = estaCorriendo ? velocidadSaltoCorriendo : impulsoLateralSalto;
        return movimiento * Mathf.Max(enSuelo, enAire);
    }

    void ConfigurarFisica()
    {
        cuerpoFisico = transform.parent;
        if (cuerpoFisico != null)
        {
            rb = cuerpoFisico.GetComponent<Rigidbody2D>();
            colliderCuerpo = cuerpoFisico.GetComponent<Collider2D>();
        }

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (colliderCuerpo == null)
            colliderCuerpo = GetComponent<Collider2D>();
        if (cuerpoFisico == null)
            cuerpoFisico = transform;

        colision = colliderCuerpo;
        if (rb != null)
            rb.gravityScale = gravedadEscala;

        mallas = GetComponentsInChildren<SkinnedMeshRenderer>(true);
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
        float lateral = rb.velocity.x;
        if (Mathf.Abs(movimiento) > 0.01f)
        {
            float enAire = estaCorriendo ? velocidadSaltoCorriendo : impulsoLateralSalto;
            lateral = movimiento * Mathf.Max(Mathf.Abs(rb.velocity.x), enAire);
        }
        rb.velocity = new Vector2(lateral, fuerzaSalto);
        marcaSalto = Time.time;
        if (animator != null)
        {
            animator.ResetTrigger("Land");
            animator.SetTrigger("Jump");
        }

        AvisarPuenteBajoLosPies();
    }

    void AvisarPuenteBajoLosPies()
    {
        Vector2 pies = cuerpoFisico != null
            ? (Vector2)cuerpoFisico.position
            : (colision != null
                ? new Vector2(colision.bounds.center.x, colision.bounds.min.y)
                : (Vector2)transform.position);
        Vector2 origen = new Vector2(pies.x, pies.y + 0.02f);
        Collider2D[] suelos = Physics2D.OverlapBoxAll(origen, new Vector2(0.36f, 0.2f), 0f, capaSuelo);
        for (int i = 0; i < suelos.Length; i++)
        {
            PuenteRompible puente = suelos[i].GetComponent<PuenteRompible>();
            if (puente != null)
                puente.NotificarSalto(this);
        }
    }

    bool TocaSuelo()
    {
        // 8 deja pasar el empuje del container (sube a unos 5) y corta el salto (11.5).
        if (Time.time < ignorarSueloHasta || (rb != null && rb.velocity.y > 8f))
            return false;

        Vector2 pies = cuerpoFisico != null
            ? (Vector2)cuerpoFisico.position
            : (colision != null
                ? new Vector2(colision.bounds.center.x, colision.bounds.min.y)
                : (Vector2)transform.position);
        Vector2 origen = new Vector2(pies.x, pies.y + 0.02f);
        return Physics2D.OverlapBox(origen, new Vector2(0.36f, 0.2f), 0f, capaSuelo) != null;
    }

    void ActualizarSuelo()
    {
        estaEnSuelo = TocaSuelo();
        if (estaEnSuelo)
            ultimoSueloTime = Time.time;

        if (!estabaEnSuelo && estaEnSuelo && animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.SetTrigger("Land");
        }

        if (estaEnSuelo)
            AsegurarSalidaDelSalto();

        if (estabaEnSuelo || !estaEnSuelo)
            return;

        if (movimiento == 0f && rb != null)
            rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    void AsegurarSalidaDelSalto()
    {
        if (animator == null || animator.IsInTransition(0))
            return;
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("JumpOver"))
            return;

        animator.ResetTrigger("Jump");
        animator.SetTrigger("Land");
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

    void AlinearVisual()
    {
        if (cuerpoFisico == null)
            return;

        if (cuerpoFisico.parent == null)
        {
            cuerpoFisico.rotation = Quaternion.identity;
            cuerpoFisico.localScale = Vector3.one;
        }

        Vector3 deseado = cuerpoFisico.position;
        deseado.z = planoVisualZ;
        transform.localPosition = cuerpoFisico.InverseTransformPoint(deseado);
    }

    void PonerDelanteDelEscenario()
    {
        Shader plano = Shader.Find("Grace/Plano");
        foreach (Renderer render in GetComponentsInChildren<Renderer>(true))
        {
            render.sortingOrder = ordenSobreEscenario;
            if (plano == null || render is SpriteRenderer)
                continue;

            Material[] originales = render.sharedMaterials;
            var planos = new Material[originales.Length];
            for (int i = 0; i < originales.Length; i++)
            {
                Material origen = originales[i];
                var nuevo = new Material(plano);
                if (origen != null && origen.HasProperty("_MainTex"))
                    nuevo.mainTexture = origen.mainTexture;
                if (origen != null && origen.HasProperty("_Color"))
                    nuevo.color = origen.color;
                planos[i] = nuevo;
            }

            render.materials = planos;
        }
    }

    static Quaternion RotacionDePerfil(Quaternion rotacion)
    {
        float yaw = rotacion.eulerAngles.y;
        float lado = Mathf.DeltaAngle(0f, yaw) >= 0f ? 90f : -90f;
        return Quaternion.Euler(0f, lado, 0f);
    }

    void ActualizarOrientacion()
    {
        if (Mathf.Abs(movimiento) < 0.01f)
            return;

        transform.localRotation = rotacionInicial * Quaternion.Euler(0f, movimiento < 0f ? 180f : 0f, 0f);
    }

    public void EntrarAlEdificioFinal()
    {
        if (yaMurio || enCinematica)
            return;

        enCinematica = true;
        controlBloqueado = true;
        movimiento = 0f;
        estaCorriendo = false;
        saltoPedido = false;

        UsarColision(false);
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
        }

        // Queda detrás de la fachada: el vano negro la tapa y se lee como que entró.
        foreach (Renderer render in GetComponentsInChildren<Renderer>(true))
            render.sortingOrder = 2;
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

        UsarColision(false);
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

        Transform raizLancha = cuerpoFisico != null ? cuerpoFisico : transform;
        raizLancha.SetParent(asiento, true);
        raizLancha.position = asiento.TransformPoint(offsetPiesLocal);
        raizLancha.rotation = Quaternion.identity;
        transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        Vector3 enElPlano = transform.position;
        enElPlano.z = planoVisualZ;
        transform.position = enElPlano;

        foreach (Renderer render in GetComponentsInChildren<Renderer>(true))
            render.sortingOrder = ordenSorting;
    }

    public void AcomodarEnAncla(Vector3 local)
    {
        Transform raiz = cuerpoFisico != null ? cuerpoFisico : transform;
        raiz.localPosition = local;
        raiz.localRotation = Quaternion.identity;
    }

    void UsarColision(bool activa)
    {
        if (colision != null)
            colision.enabled = activa;
    }

    public void EmpujadaPorOla()
    {
        if (controlBloqueado || enCinematica || rb == null)
            return;

        controlBloqueado = true;
        estaEnSuelo = false;
        estabaEnSuelo = false;
        lanzadaPorOla = true;
        movimiento = 0f;
        estaCorriendo = false;
        saltoPedido = false;
        ReproducirCaidaPorOla();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = gravedadEscala;
        rb.velocity = new Vector2(impulsoOla.x, 0f);
        // Suelta el piso en el acto: la caída sale de la plataforma, no sigue caminando encima.
        UsarColision(false);
        Invoke(nameof(MorirPorCaidaAlMar), 0.95f);
    }

    public void AguantarSobrePuente()
    {
        if (yaMurio || caidaAlMar || lanzadaPorOla || enCinematica)
            return;

        saltoPedido = false;
        ignorarSueloHasta = 0f;
        if (rb != null && rb.velocity.y > 0f)
            rb.velocity = new Vector2(rb.velocity.x, 0f);

        if (animator == null)
            return;

        animator.ResetTrigger("Jump");
        if (TieneParametro("IsFalling"))
            animator.SetBool("IsFalling", false);
    }

    public void CaerDesdePuente()
    {
        if (yaMurio || caidaAlMar || lanzadaPorOla || enCinematica)
            return;

        caidaAlMar = true;
        controlBloqueado = true;
        estaEnSuelo = false;
        estabaEnSuelo = false;
        movimiento = 0f;
        estaCorriendo = false;
        saltoPedido = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = gravedadEscala;
            if (rb.velocity.y > 0f)
                rb.velocity = new Vector2(rb.velocity.x, 0f);
        }

        ReproducirCaidaPorOla();
        if (animator != null)
            animator.Update(0f);
        UsarColision(false);
    }

    Animator BuscarAnimador()
    {
        Animator[] animadores = GetComponentsInChildren<Animator>(true);
        int hashCaida = Animator.StringToHash(EstadoCaidaOla);
        Animator elegido = null;

        foreach (Animator candidato in animadores)
        {
            if (candidato.runtimeAnimatorController == null)
            {
                candidato.enabled = false;
                continue;
            }

            if (elegido == null ||
                (candidato.HasState(0, hashCaida) && !elegido.HasState(0, hashCaida)))
                elegido = candidato;
        }

        if (elegido == null)
            return animadores.Length > 0 ? animadores[0] : null;

        // Un segundo Animator sobre el mismo rig pisa la pose y la deja en idle.
        foreach (Animator candidato in animadores)
        {
            if (candidato != elegido)
                candidato.enabled = false;
        }

        return elegido;
    }

    void ReproducirCaidaPorOla()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", 0f);
        animator.SetBool("IsGrounded", false);
        animator.SetBool("IsRunning", false);
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Land");
        if (TieneParametro("IsFalling"))
            animator.SetBool("IsFalling", true);

        if (animator.HasState(0, Animator.StringToHash(EstadoCaidaOla)))
            animator.Play(EstadoCaidaOla, 0, 0f);
    }

    void MantenerAnimacionCaida()
    {
        if (animator == null || (yaMurio && !lanzadaAlCielo))
            return;

        if (TieneParametro("IsFalling"))
            animator.SetBool("IsFalling", true);

        if (!animator.HasState(0, Animator.StringToHash(EstadoCaidaOla)))
            return;

        if (animator.IsInTransition(0) &&
            animator.GetNextAnimatorStateInfo(0).IsName(EstadoCaidaOla))
            return;

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(EstadoCaidaOla))
            animator.Play(EstadoCaidaOla, 0, 0f);
    }

    bool TieneParametro(string nombre)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.name == nombre)
                return true;
        }

        return false;
    }

    public void LanzarAlCielo()
    {
        if (yaMurio)
            return;

        yaMurio = true;
        lanzadaAlCielo = true;
        enCinematica = true;
        controlBloqueado = true;
        movimiento = 0f;
        saltoPedido = false;
        estaEnSuelo = false;
        estabaEnSuelo = false;

        UsarColision(false);
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            // Golpe seco: sale disparada y corta al salir del encuadre.
            rb.gravityScale = 2.2f;
            rb.velocity = new Vector2(7f, 28f);
        }

        ReproducirCaidaPorOla();
        if (animator != null)
        {
            animator.speed = 1.6f;
            animator.Update(0f);
        }

        CameraFollow camara = Camera.main != null
            ? Camera.main.GetComponent<CameraFollow>()
            : null;
        if (camara != null)
            camara.FijarAltura();

        Invoke(nameof(ReiniciarTrasLanzamiento), 0.5f);
    }

    void ReiniciarTrasLanzamiento()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MorirPorContainer()
    {
        if (yaMurio)
            return;

        yaMurio = true;
        controlBloqueado = true;
        movimiento = 0f;
        saltoPedido = false;
        if (AlinearCuerpoConElPiso(out alturaPisoMuerte))
            ajustarMuerteAlPiso = true;
        UsarColision(false);
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

    bool AlinearCuerpoConElPiso(out float alturaPiso)
    {
        alturaPiso = 0f;
        if (colision == null || capaSuelo.value == 0)
            return false;

        Bounds bounds = colision.bounds;
        RaycastHit2D impacto = Physics2D.Raycast(
            bounds.center,
            Vector2.down,
            Mathf.Infinity,
            capaSuelo
        );

        if (impacto.collider == null)
            return false;

        alturaPiso = impacto.point.y;
        return true;
    }

    void MorirPorCaidaAlMar()
    {
        if (yaMurio)
            return;

        yaMurio = true;
        lanzadaPorOla = false;
        CancelInvoke(nameof(MorirPorCaidaAlMar));
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void AnclarPiesVisualesDuranteCaida()
    {
        if (cuerpoFisico == null || mallas == null || mallas.Length == 0)
            return;

        float limiteInferior = float.PositiveInfinity;
        for (int i = 0; i < mallas.Length; i++)
        {
            if (mallas[i] != null && mallas[i].enabled)
                limiteInferior = Mathf.Min(limiteInferior, mallas[i].bounds.min.y);
        }

        if (float.IsPositiveInfinity(limiteInferior))
            return;

        // El clip puede desplazar el rig verticalmente según la pose anterior.
        // Los pies visuales deben seguir el cuerpo físico mientras este abandona
        // la plataforma, nunca subir por encima de él.
        transform.position += Vector3.up * (cuerpoFisico.position.y - limiteInferior);
    }

    /// <summary>La deja quieta para que la grúa de cambio de zona la lleve.</summary>
    public void PrepararTrasladoGrua()
    {
        if (yaMurio)
            return;

        enCinematica = true;
        controlBloqueado = true;
        movimiento = 0f;
        estaCorriendo = false;
        saltoPedido = false;
        estaEnSuelo = true;

        if (colision != null && colision.enabled)
            distanciaPies = colision.bounds.min.y - transform.position.y;

        UsarColision(false);

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
        }
    }

    /// <summary>Distancia en Y desde el transform hasta los pies. Es negativa.</summary>
    public float DistanciaPies()
    {
        return distanciaPies;
    }

    public void EngancharEnGrua(Transform anclaje)
    {
        PrepararTrasladoGrua();
        Transform raizGrua = cuerpoFisico != null ? cuerpoFisico : transform;
        raizGrua.SetParent(anclaje, true);
        raizGrua.localRotation = Quaternion.identity;
        transform.localRotation = rotacionInicial;
    }

    public void SoltarDeGrua(Vector3 piesMundo)
    {
        Transform raizGrua = cuerpoFisico != null ? cuerpoFisico : transform;
        raizGrua.SetParent(null, true);
        raizGrua.position = new Vector3(piesMundo.x, piesMundo.y - distanciaPies, 0f);
        raizGrua.rotation = Quaternion.identity;
        transform.localRotation = rotacionInicial;

        if (rendererVisual != null)
            rendererVisual.sortingOrder = 9;

        UsarColision(true);

        if (rb != null)
        {
            rb.simulated = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravedadEscala;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        enCinematica = false;
        controlBloqueado = false;
        estaEnSuelo = true;
        ultimoSueloTime = Time.time;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Bounds bounds = colision != null ? colision.bounds : new Bounds(transform.position, Vector3.one);
        Vector3 origen = new Vector3(bounds.center.x, bounds.min.y - tamanoSuelo.y * 0.15f, bounds.center.z);
        Gizmos.DrawWireCube(origen, tamanoSuelo);
    }
}
