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
    PlayerAudio audio;
    Animator animator;
    Collider2D colision;
    SpriteRenderer rendererVisual;
    Quaternion rotacionInicial;
    float movimiento;
    bool estaEnSuelo;
    bool estabaEnSuelo;
    bool saltoEnElAire;
    bool soltoElSueloEnElSalto;
    bool estaCorriendo;
    bool controlBloqueado;
    bool enCinematica;
    bool yaMurio;
    bool saltoPedido;
    bool lanzadaPorOla;
    bool lanzadaAlCielo;
    bool caidaAlMar;
    bool muerteAlAterrizar;
    bool caidaVacioInicio;
    bool saltoVacioInicio;
    bool estirandoSalto;
    bool entrandoEnCaida;
    float relojEstiron;
    bool bordeInicioListo;
    float bordeIzquierdoInicio;
    float alturaInicio;
    float avanceCaida;
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
            return estaEnSuelo && !enCinematica && !yaMurio;
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
        audio = GetComponent<PlayerAudio>();
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

        if (GruaZona.ZonaAlcanzada && rb != null)
        {
            float bajoLosPies = 0f;
            if (colision is CapsuleCollider2D capsula)
                bajoLosPies = capsula.offset.y - capsula.size.y * 0.5f;
            Vector2 piso = GruaZona.Reaparicion;
            rb.position = new Vector2(piso.x, piso.y - bajoLosPies);
            rb.velocity = Vector2.zero;
            Physics2D.SyncTransforms();
            CameraFollow camara = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (camara != null)
                camara.ColocarEn(rb.position);
        }

        estaEnSuelo = TocaSuelo();
        estabaEnSuelo = estaEnSuelo;
        MedirPlataformaInicio();
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
            if (lanzadaPorOla || caidaAlMar || lanzadaAlCielo || (caidaVacioInicio && !entrandoEnCaida))
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

        if (muerteAlAterrizar)
        {
            float yCaida = cuerpoFisico != null ? cuerpoFisico.position.y : transform.position.y;
            estaEnSuelo = false;
            if (yCaida < Mathf.Min(yMuerteMar, -7.5f))
            {
                MorirPorCaidaAlMar();
                return;
            }

            if (PisoBajoLosPies())
                CompletarMuerteEnElPiso();
            return;
        }

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
        ActualizarCaidaAlVacioInicial();
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

        if (caidaAlMar && rb != null && !yaMurio)
        {
            float vertical = Mathf.Min(0f, rb.velocity.y);
            rb.velocity = new Vector2(avanceCaida, vertical);
            return;
        }

        if (muerteAlAterrizar && rb != null && !yaMurio)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(0f, rb.velocity.y));
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
            FrenarIzquierdaEnElInicio();
            return;
        }

        float velocidadAire = Mathf.Lerp(rb.velocity.x, velocidadObjetivo, controlAire);
        float velocidadVertical = rb.velocity.y;
        if (velocidadVertical < 0f)
            velocidadVertical += Physics2D.gravity.y * (multiplicadorCaida - 1f) * gravedadEscala * Time.fixedDeltaTime;

        SoltarParedVertical(ref velocidadAire, ref velocidadVertical);
        rb.velocity = new Vector2(velocidadAire, velocidadVertical);
        FrenarIzquierdaEnElInicio();
    }

    void SoltarParedVertical(ref float velocidadX, ref float velocidadY)
    {
        if (rb == null)
            return;

        int cantidad = rb.GetContacts(contactosSuelo);
        float separacion = 0f;
        bool pared = false;
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 normal = contactosSuelo[i].normal;
            // Piso y techo no. Solo la cara vertical, que con la fricción deja vy en cero.
            if (normal.y > 0.4f || Mathf.Abs(normal.x) < 0.65f)
                continue;

            pared = true;
            if (velocidadX * normal.x < 0f)
                velocidadX = 0f;
            separacion += normal.x;
        }

        if (!pared)
            return;

        if (velocidadY > -2f)
            velocidadY = -2f;

        if (separacion != 0f)
            rb.position += new Vector2(Mathf.Sign(separacion) * 0.02f, 0f);
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
        return estaEnSuelo && !saltoEnElAire;
    }

    void EjecutarSalto()
    {
        saltoEnElAire = true;
        soltoElSueloEnElSalto = false;
        estaEnSuelo = false;
        estabaEnSuelo = false;
        ultimoSueloTime = -10f;
        ignorarSueloHasta = Time.time + tiempoIgnorarSueloTrasSalto;
        float lateral = rb.velocity.x;
        if (Mathf.Abs(movimiento) > 0.01f)
        {
            float enAire = estaCorriendo ? velocidadSaltoCorriendo : impulsoLateralSalto;
            lateral = movimiento * Mathf.Max(Mathf.Abs(rb.velocity.x), enAire);
        }
        if (bordeInicioListo && rb.position.x <= bordeIzquierdoInicio + 0.4f)
            lateral = Mathf.Max(0f, lateral);
        rb.velocity = new Vector2(lateral, fuerzaSalto);
        marcaSalto = Time.time;
        if (audio != null)
            audio.PlayJump();
        if (animator != null)
        {
            animator.ResetTrigger("Land");
            animator.SetTrigger("Jump");
        }

        if (VaHaciaElVacioInicial())
        {
            saltoVacioInicio = true;
            estirandoSalto = false;
            relojEstiron = 0f;
        }

        AvisarPuenteBajoLosPies();
    }

    bool VaHaciaElVacioInicial()
    {
        if (!bordeInicioListo || rb == null)
            return false;

        bool haciaIzquierda = movimiento < -0.01f || rb.velocity.x < -0.15f;
        bool enLaPlataforma = rb.position.x < bordeIzquierdoInicio + 4f
            && rb.position.y < alturaInicio + 1.2f
            && rb.position.y > alturaInicio - 0.6f;
        return haciaIzquierda && enLaPlataforma;
    }

    void AvisarPuenteBajoLosPies()
    {
        Vector2 pies = colision != null
            ? new Vector2(colision.bounds.center.x, colision.bounds.min.y)
            : cuerpoFisico != null
                ? (Vector2)cuerpoFisico.position
                : (Vector2)transform.position;
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
        // Caja chica, hacia abajo, y solo si la cara es un piso.
        // El roce lateral con la plataforma de al lado no cuenta como suelo.
        RaycastHit2D golpe = Physics2D.BoxCast(
            new Vector2(pies.x, pies.y + 0.2f),
            new Vector2(0.16f, 0.02f),
            0f,
            Vector2.down,
            0.32f,
            capaSuelo);
        if (golpe.collider == null || golpe.normal.y < 0.45f)
            return false;

        PuenteRompible puente = PuenteRompible.ColapsoActivo;
        if (puente != null && puente.EnRoceDeSalida(this) && golpe.collider.GetComponent<PuenteRompible>() != puente)
            return false;

        return true;
    }

    void IntentarCaerDelPuenteRoto()
    {
        if (caidaAlMar || lanzadaPorOla || enCinematica || yaMurio)
            return;

        PuenteRompible puente = PuenteRompible.ColapsoActivo;
        if (puente != null && puente.EnRoceDeSalida(this))
            CaerDesdePuente();
    }

    void ActualizarSuelo()
    {
        bool toca = TocaSuelo();
        if (saltoEnElAire)
        {
            if (!toca)
                soltoElSueloEnElSalto = true;

            if (toca && soltoElSueloEnElSalto)
                saltoEnElAire = false;
            else
                toca = false;
        }

        estaEnSuelo = toca;
        if (!estaEnSuelo)
            IntentarCaerDelPuenteRoto();
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

    void MedirPlataformaInicio()
    {
        if (bordeInicioListo || rb == null)
            return;

        Vector2 origen = rb.position + Vector2.up * 0.3f;
        RaycastHit2D suelo = Physics2D.Raycast(origen, Vector2.down, 3f, capaSuelo);
        if (suelo.collider == null)
            return;

        alturaInicio = suelo.point.y;
        float x = suelo.point.x;
        bool hayBordeIzquierdo = false;
        for (int i = 0; i < 120; i++)
        {
            Vector2 prueba = new Vector2(x - 0.35f, alturaInicio + 0.45f);
            RaycastHit2D paso = Physics2D.Raycast(prueba, Vector2.down, 1.2f, capaSuelo);
            if (paso.collider == null || paso.point.y < alturaInicio - 0.35f)
            {
                hayBordeIzquierdo = true;
                break;
            }
            x = paso.point.x;
        }

        bordeIzquierdoInicio = x;
        bordeInicioListo = true;

        if (!hayBordeIzquierdo)
            return;

        CameraFollow camara = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (camara != null)
            camara.FijarBordeIzquierdo(bordeIzquierdoInicio);
    }

    void FrenarIzquierdaEnElInicio()
    {
        if (!bordeInicioListo || rb == null)
            return;

        float minimo = bordeIzquierdoInicio + 0.3f;
        if (rb.position.x > minimo)
            return;

        rb.position = new Vector2(minimo, rb.position.y);
        if (rb.velocity.x < 0f)
            rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    void ActualizarCaidaAlVacioInicial()
    {
        if (caidaAlMar || lanzadaPorOla || lanzadaAlCielo || yaMurio || enCinematica)
            return;

        if (!bordeInicioListo)
            MedirPlataformaInicio();

        if (saltoVacioInicio && !caidaVacioInicio)
        {
            ActualizarTransicionSaltoVacio();
            return;
        }

        if (caidaVacioInicio)
        {
            if (entrandoEnCaida)
            {
                if (animator != null
                    && !animator.IsInTransition(0)
                    && animator.GetCurrentAnimatorStateInfo(0).IsName(EstadoCaidaOla))
                {
                    entrandoEnCaida = false;
                    if (TieneParametro("IsFalling"))
                        animator.SetBool("IsFalling", true);
                }
                return;
            }

            MantenerAnimacionCaida();
            return;
        }

        if (!bordeInicioListo || estaEnSuelo || rb == null)
            return;
        if (rb.position.x > bordeIzquierdoInicio - 0.08f)
            return;
        if (rb.position.y > alturaInicio - 0.28f)
            return;
        if (rb.velocity.y > -0.15f)
            return;

        caidaVacioInicio = true;
        ReproducirCaidaPorOla();
    }

    const float CorteEstironSalto = 0.62f;
    const float DuracionEstironSalto = 0.2f;

    void ActualizarTransicionSaltoVacio()
    {
        if (animator == null || rb == null)
            return;

        if (estaEnSuelo && soltoElSueloEnElSalto)
        {
            saltoVacioInicio = false;
            estirandoSalto = false;
            animator.speed = 1f;
            return;
        }

        int salto = Animator.StringToHash("JumpOver");
        if (!animator.HasState(0, salto))
            return;

        float normalizado;
        if (!estirandoSalto && rb.velocity.y > 0.35f)
        {
            float subida = 1f - Mathf.Clamp01(rb.velocity.y / Mathf.Max(0.01f, fuerzaSalto));
            normalizado = subida * CorteEstironSalto;
        }
        else
        {
            if (!estirandoSalto)
            {
                estirandoSalto = true;
                relojEstiron = 0f;
            }

            relojEstiron += Time.deltaTime;
            float estiron = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(relojEstiron / DuracionEstironSalto));
            normalizado = Mathf.Lerp(CorteEstironSalto, 1f, estiron);
            if (estiron >= 1f && YaCaeAlVacioInicial())
            {
                EntrarCaidaDesdeEstiron();
                return;
            }
        }

        animator.speed = 0f;
        animator.Play(salto, 0, normalizado);
    }

    bool YaCaeAlVacioInicial()
    {
        return rb.position.x < bordeIzquierdoInicio - 0.05f
            && rb.position.y < alturaInicio - 0.12f
            && rb.velocity.y < -0.2f;
    }

    void EntrarCaidaDesdeEstiron()
    {
        saltoVacioInicio = false;
        caidaVacioInicio = true;
        entrandoEnCaida = true;
        animator.speed = 1f;
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Land");
        animator.SetBool("IsGrounded", false);
        animator.SetBool("IsRunning", false);
        animator.SetFloat("Speed", 0f);
        animator.CrossFadeInFixedTime(EstadoCaidaOla, 0.2f, 0, 0f);
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
        // En el editor Shader.Find ve cualquier shader del proyecto. En la build
        // solo entran los que están referenciados, así que el material vive en Resources.
        Material basePlano = Resources.Load<Material>("GracePlano");
        Shader plano = basePlano != null ? basePlano.shader : Shader.Find("Grace/Plano");
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
        if (caidaVacioInicio)
            return;
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
        SonarGolpe();
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

        float avance = rb != null ? rb.velocity.x : 0f;
        if (estaCorriendo)
        {
            float signo = Mathf.Abs(avance) > 0.2f
                ? Mathf.Sign(avance)
                : (Mathf.Abs(movimiento) > 0.01f ? Mathf.Sign(movimiento) : 1f);
            avance = signo * Mathf.Max(Mathf.Abs(avance), velocidadCorrer);
        }
        avanceCaida = avance;

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
            rb.velocity = new Vector2(avanceCaida, Mathf.Min(0f, rb.velocity.y));
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

        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Land");
        animator.SetBool("IsGrounded", false);
        if (TieneParametro("IsFalling"))
            animator.SetBool("IsFalling", true);

        int caida = Animator.StringToHash(EstadoCaidaOla);
        if (animator.HasState(0, caida))
        {
            animator.Play(caida, 0, 0f);
            animator.Update(0f);
        }

        animator.SetFloat("Speed", 0f);
        animator.SetBool("IsRunning", false);
    }

    void MantenerAnimacionCaida()
    {
        if (animator == null || (yaMurio && !lanzadaAlCielo))
            return;

        animator.SetBool("IsGrounded", false);
        if (TieneParametro("IsFalling"))
            animator.SetBool("IsFalling", true);

        int caida = Animator.StringToHash(EstadoCaidaOla);
        if (!animator.HasState(0, caida))
            return;

        bool yaEnCaida = animator.GetCurrentAnimatorStateInfo(0).IsName(EstadoCaidaOla)
            || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(EstadoCaidaOla));
        if (!yaEnCaida)
        {
            animator.Play(caida, 0, 0f);
            animator.Update(0f);
        }

        animator.SetFloat("Speed", 0f);
        animator.SetBool("IsRunning", false);
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
        SonarGolpe();
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
        MorirPorContainer(false, false, 1f);
    }

    /// <summary>Misma muerte de container, con otra velocidad de animación. No afecta al resto de muertes.</summary>
    public void MorirAplastadaPorContainer(float velocidadAnimacion)
    {
        // El clip se queda parada hasta ~0.5 s; esta muerte entra directo en la caída.
        MorirPorContainer(false, false, Mathf.Max(0.2f, velocidadAnimacion), 0.5f);
    }

    public void MorirEnElPiso()
    {
        if (yaMurio || muerteAlAterrizar)
            return;

        SonarGolpe();
        if (PisoBajoLosPies())
        {
            MorirPorContainer(true, true, 1f);
            Invoke(nameof(ReiniciarTrasLanzamiento), DuracionAnimacionMuerte);
            return;
        }

        muerteAlAterrizar = true;
        controlBloqueado = true;
        movimiento = 0f;
        saltoPedido = false;
        estaEnSuelo = false;
        ignorarSueloHasta = 0f;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravedadEscala;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(0f, rb.velocity.y));
        }

        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.ResetTrigger("Land");
        }
    }

    bool PisoBajoLosPies()
    {
        if (rb == null)
            return estaEnSuelo;

        RaycastHit2D suelo = Physics2D.Raycast(rb.position + Vector2.up * 0.08f, Vector2.down, 0.28f, capaSuelo);
        return suelo.collider != null && suelo.normal.y > 0.45f;
    }

    void CompletarMuerteEnElPiso()
    {
        if (yaMurio)
            return;

        muerteAlAterrizar = false;
        AsentarEnElPiso();
        MorirPorContainer(true, true, 1f);
        Invoke(nameof(ReiniciarTrasLanzamiento), DuracionAnimacionMuerte);
    }

    void AsentarEnElPiso()
    {
        if (rb == null)
            return;

        RaycastHit2D suelo = Physics2D.Raycast(rb.position + Vector2.up * 0.2f, Vector2.down, 0.7f, capaSuelo);
        if (suelo.collider == null || suelo.normal.y < 0.45f)
            return;

        float dy = suelo.point.y - rb.position.y;
        if (dy < -0.02f || dy > 0.35f)
            return;

        rb.velocity = Vector2.zero;
        rb.position = new Vector2(rb.position.x, suelo.point.y);
    }

    void MorirPorContainer(bool yaEnElPiso, bool transicionSuave, float velocidadAnimacion, float inicioClip = 0f)
    {
        if (yaMurio)
            return;

        yaMurio = true;
        controlBloqueado = true;
        movimiento = 0f;
        saltoPedido = false;
        saltoEnElAire = false;
        estaEnSuelo = true;
        if (!yaEnElPiso)
            SonarGolpe();
        if (!yaEnElPiso && AlinearCuerpoConElPiso(out alturaPisoMuerte))
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
        if (animator == null)
            return;

        animator.speed = velocidadAnimacion;
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Land");
        animator.ResetTrigger("Dead");
        animator.SetFloat("Speed", 0f);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsGrounded", true);
        if (TieneParametro("IsFalling"))
            animator.SetBool("IsFalling", false);

        int estadoMuerte = Animator.StringToHash("dead_back");
        if (inicioClip > 0f && animator.HasState(0, estadoMuerte))
        {
            animator.Play(estadoMuerte, 0, Mathf.Clamp01(inicioClip / 2.5666668f));
            animator.Update(0f);
        }
        else if (transicionSuave && animator.HasState(0, estadoMuerte))
            animator.CrossFadeInFixedTime("dead_back", 0.3f, 0, 0f);
        else
            animator.SetTrigger("Dead");
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

    public void RecibirGolpe()
    {
        if (yaMurio)
            return;

        SonarGolpe();
    }

    void SonarGolpe()
    {
        if (audio == null)
            audio = GetComponent<PlayerAudio>();
        if (audio != null)
            audio.PlayHurt();
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

    bool MiraALaIzquierda()
    {
        float delta = Mathf.DeltaAngle(rotacionInicial.eulerAngles.y, transform.localEulerAngles.y);
        return Mathf.Abs(delta) > 90f;
    }

    void AnclarPiesVisualesDuranteCaida()
    {
        if (cuerpoFisico == null)
            return;

        // El clip se zambulle hacia adelante. De perfil a la izquierda ese
        // avance es horizontal y la saca del cuadro. La cadera queda en el cuerpo.
        if ((caidaVacioInicio || MiraALaIzquierda()) && animator != null)
        {
            Transform cadera = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (cadera != null)
            {
                Vector3 destino = cuerpoFisico.position + Vector3.up * 0.9f;
                transform.position += new Vector3(
                    destino.x - cadera.position.x,
                    destino.y - cadera.position.y,
                    0f);
                return;
            }
        }

        if (mallas == null || mallas.Length == 0)
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
        saltoEnElAire = false;
        soltoElSueloEnElSalto = false;

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
            animator.SetBool("IsFalling", false);
            animator.SetFloat("VerticalSpeed", 0f);
            animator.ResetTrigger("Jump");
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("JumpOver"))
                animator.SetTrigger("Land");
            else
                animator.ResetTrigger("Land");
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
        saltoEnElAire = false;
        soltoElSueloEnElSalto = false;
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
