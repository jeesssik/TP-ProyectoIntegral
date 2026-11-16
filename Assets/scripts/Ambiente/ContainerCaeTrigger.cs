using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ContainerCaeTrigger : MonoBehaviour
{
    public Animator containerAnimator;
    public Transform containerTransform;
    [Tooltip("Sprite que queda en el piso después de caer. El de la grúa solo se ve mientras cuelga.")]
    public Sprite spriteEnElPiso;
    public string estadoNormal = "ContainerMeciendose";
    public string estadoCaida = "ContainerCae";
    public float tiempoHastaImpacto = 0.7f;
    public float velocidadCaidaContainer = 22f;
    [Tooltip("Si al impactar Grace está más lejos que esto, sobrevive.")]
    public float anchoImpacto = 1.6f;
    [Tooltip("Y mundial por debajo del cual el container sale del frame y se apaga.")]
    public float yFueraDePantalla = -14f;
    public LayerMask capaPiso;

    bool activado;
    bool yaMato;
    bool asentado;
    bool escalando;
    Grace graceImpactada;
    Grace graceVigia;
    BoxCollider2D pisoCaja;
    float xZonaImpacto;
    Vector3 posicionTirantesInicial;
    Quaternion rotacionTirantesInicial;
    Vector3 escalaTirantesInicial;

    void Awake()
    {
        if (capaPiso.value == 0)
            capaPiso = 1 << 6; // misma capa de suelo que Grace

        if (containerTransform == null)
            containerTransform = transform;

        pisoCaja = GetComponent<BoxCollider2D>();
        if (pisoCaja != null && containerAnimator == null)
            pisoCaja.enabled = false;

        if (containerAnimator != null)
        {
            posicionTirantesInicial = containerAnimator.transform.position;
            rotacionTirantesInicial = containerAnimator.transform.rotation;
            // El clip está pensado a escala 1; sin esto el Animator pisa el scale de escena.
            escalaTirantesInicial = containerAnimator.transform.localScale;
            containerAnimator.enabled = true;
            containerAnimator.Play(estadoNormal, 0, 0f);
        }

        // El sprite suelto se esconde solo si hay un animator mostrando el que se mece.
        // Si el script está en el propio container, ese es el que tiene que verse colgado.
        if (containerAnimator != null && containerTransform != null && containerTransform != containerAnimator.transform)
        {
            Animator animExtra = containerTransform.GetComponent<Animator>();
            if (animExtra != null)
                animExtra.enabled = false;

            containerTransform.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (asentado)
        {
            IntentarEscalar();
            return;
        }

        if (activado || containerAnimator != null)
            return;

        Grace debajo = GraceBajoElContainer();
        if (debajo == null)
            return;

        activado = true;
        graceImpactada = debajo;
        xZonaImpacto = transform.position.x;
        StartCoroutine(CaerYMatar());
    }

    Grace GraceBajoElContainer()
    {
        if (graceVigia == null)
            graceVigia = FindObjectOfType<Grace>();
        Grace grace = graceVigia;
        if (grace == null)
            return null;

        Collider2D cuerpo = grace.ColisionCuerpo;
        float x = cuerpo != null ? cuerpo.bounds.center.x : grace.transform.position.x;
        float pie = cuerpo != null ? cuerpo.bounds.min.y : grace.transform.position.y;
        Bounds zona = ZonaDelContainer();
        if (x < zona.min.x || x > zona.max.x)
            return null;
        if (pie > zona.max.y)
            return null;
        if (zona.min.y - pie > 18f)
            return null;
        return grace;
    }

    Bounds ZonaDelContainer()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            return sr.bounds;
        if (containerTransform != null)
        {
            SpriteRenderer otro = containerTransform.GetComponent<SpriteRenderer>();
            if (otro != null && otro.sprite != null)
                return otro.bounds;
        }
        return new Bounds(transform.position, new Vector3(4f, 4f, 1f));
    }

    void LateUpdate()
    {
        if (containerAnimator == null || !containerAnimator.enabled)
            return;

        // El clip asume scale 1; reaplicamos la escala de escena siempre que anime.
        Transform t = containerAnimator.transform;
        t.localScale = escalaTirantesInicial;

        if (!activado)
            return;

        t.position = posicionTirantesInicial;
        t.rotation = rotacionTirantesInicial;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Grace grace = Grace.En(other);
        if (activado || grace == null || containerAnimator == null)
            return;

        activado = true;
        graceImpactada = grace;
        xZonaImpacto = transform.position.x;

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.enabled = false;

        containerAnimator.enabled = true;
        containerAnimator.Play(estadoCaida, 0, 0f);
        Invoke(nameof(Impactar), tiempoHastaImpacto);
    }

    void Impactar()
    {
        // El spritesheet termina a media altura: lo escondemos y bajamos el suelto al piso.
        if (containerAnimator != null)
        {
            SpriteRenderer srAnim = containerAnimator.GetComponent<SpriteRenderer>();
            if (srAnim != null)
                srAnim.enabled = false;

            containerAnimator.enabled = false;
        }

        if (containerTransform != null)
            StartCoroutine(CaerHastaElPiso());

        bool mortal = graceImpactada != null &&
            Mathf.Abs(graceImpactada.transform.position.x - xZonaImpacto) <= anchoImpacto;

        if (!mortal)
            return;

        graceImpactada.MorirPorContainer();
        Invoke(nameof(ReiniciarEscena), Grace.DuracionAnimacionMuerte);
    }

    IEnumerator CaerHastaElPiso()
    {
        containerTransform.SetParent(null, true);
        containerTransform.gameObject.SetActive(true);

        Vector3 inicio = new Vector3(
            xZonaImpacto,
            posicionTirantesInicial.y - 1.5f,
            0f
        );
        // Sigue de largo: atraviesa el muelle y sale por abajo del frame.
        Vector3 destino = new Vector3(xZonaImpacto, yFueraDePantalla, 0f);

        containerTransform.SetPositionAndRotation(inicio, Quaternion.Euler(0f, 0f, -18f));

        while ((containerTransform.position - destino).sqrMagnitude > 0.0001f)
        {
            containerTransform.position = Vector3.MoveTowards(
                containerTransform.position,
                destino,
                velocidadCaidaContainer * Time.deltaTime
            );
            yield return null;
        }

        containerTransform.position = destino;
        containerTransform.gameObject.SetActive(false);
    }

    IEnumerator CaerYMatar()
    {
        Transform caja = containerTransform != null ? containerTransform : transform;
        caja.SetParent(null, true);
        caja.gameObject.SetActive(true);

        SpriteRenderer sr = caja.GetComponent<SpriteRenderer>();
        float suelo = AlturaDelPiso(caja.position);
        float hastaElPiso = sr != null && sr.sprite != null ? caja.position.y - sr.bounds.min.y : 0f;
        Vector3 destino = new Vector3(xZonaImpacto, suelo + hastaElPiso, caja.position.z);

        while ((caja.position - destino).sqrMagnitude > 0.01f)
        {
            caja.position = Vector3.MoveTowards(
                caja.position,
                destino,
                velocidadCaidaContainer * Time.deltaTime);
            if (!yaMato && sr != null && graceImpactada != null)
            {
                Collider2D cuerpo = graceImpactada.ColisionCuerpo;
                float tope = cuerpo != null ? cuerpo.bounds.max.y : graceImpactada.transform.position.y + 1.6f;
                if (sr.bounds.min.y <= tope)
                    MatarSiSigueDebajo();
            }
            yield return null;
        }

        caja.position = destino;
        if (!yaMato)
            MatarSiSigueDebajo();
        if (yaMato)
            yield break;

        PonerSueltoEnElPiso(caja, suelo);
        if (pisoCaja != null)
            pisoCaja.enabled = true;
        asentado = true;
    }

    void PonerSueltoEnElPiso(Transform caja, float suelo)
    {
        SpriteRenderer sr = caja.GetComponent<SpriteRenderer>();
        if (sr == null || spriteEnElPiso == null)
            return;

        // container Cae_0 mide 406 px de recuadro, pero el dibujo opaco son 380.
        // containerSuelto: el cajón (sin cadenas ni margen) va de x 76..1216.
        Sprite colgado = sr.sprite;
        float anchoRecuadro = colgado.rect.width / colgado.pixelsPerUnit * Mathf.Abs(caja.lossyScale.x);
        const float opacoSobreRecuadro = 380f / 406f;
        float anchoVisible = anchoRecuadro * opacoSobreRecuadro;

        sr.sprite = spriteEnElPiso;

        const float lienzo = 12.54f;
        const float cuerpoIzq = 0.76f;
        const float cuerpoDer = 12.16f;
        const float cuerpoAbajo = 2.38f;
        const float cuerpoArriba = 8.04f;
        float mitad = lienzo * 0.5f;
        float localIzq = cuerpoIzq - mitad;
        float localDer = cuerpoDer - mitad;
        float localAbajo = cuerpoAbajo - mitad;
        float localArriba = cuerpoArriba - mitad;

        float anchoCuerpo = cuerpoDer - cuerpoIzq;
        float escala = anchoVisible / Mathf.Max(0.01f, anchoCuerpo);
        caja.localScale = new Vector3(escala, escala, 1f);
        caja.position = new Vector3(xZonaImpacto, suelo - localAbajo * escala, caja.position.z);

        if (pisoCaja == null)
            return;

        pisoCaja.offset = new Vector2((localIzq + localDer) * 0.5f, (localAbajo + localArriba) * 0.5f);
        pisoCaja.size = new Vector2(localDer - localIzq, localArriba - localAbajo);
    }

    void IntentarEscalar()
    {
        if (escalando || pisoCaja == null || !pisoCaja.enabled)
            return;

        if (graceVigia == null)
            graceVigia = FindObjectOfType<Grace>();
        Grace grace = graceVigia;
        if (grace == null || grace.EstaEnElAire)
            return;

        Rigidbody2D cuerpoRb = grace.CuerpoRigido;
        Collider2D cuerpo = grace.ColisionCuerpo;
        if (cuerpoRb == null || cuerpo == null)
            return;

        Bounds caja = pisoCaja.bounds;
        if (cuerpo.bounds.min.y > caja.max.y - 0.25f)
            return;

        bool aLaIzquierda = cuerpo.bounds.max.x < caja.center.x;
        bool aLaDerecha = cuerpo.bounds.min.x > caja.center.x;
        if (!aLaIzquierda && !aLaDerecha)
            return;

        float hueco = aLaIzquierda
            ? caja.min.x - cuerpo.bounds.max.x
            : cuerpo.bounds.min.x - caja.max.x;
        if (hueco > 0.45f)
            return;

        if (!EntradaJuego.Salto)
            return;

        StartCoroutine(Escalar(grace, aLaIzquierda));
    }

    IEnumerator Escalar(Grace grace, bool desdeLaIzquierda)
    {
        escalando = true;
        Rigidbody2D cuerpoRb = grace.CuerpoRigido;
        Collider2D cuerpo = grace.ColisionCuerpo;
        if (cuerpoRb == null || cuerpo == null || pisoCaja == null)
        {
            escalando = false;
            yield break;
        }

        float pies = cuerpoRb.position.y - cuerpo.bounds.min.y;
        Bounds caja = pisoCaja.bounds;
        Vector2 inicio = cuerpoRb.position;
        float xSobre = desdeLaIzquierda ? caja.min.x + 0.55f : caja.max.x - 0.55f;
        float ySobre = caja.max.y + pies + 0.04f;
        Vector2 arriba = new Vector2(inicio.x, ySobre);
        Vector2 encima = new Vector2(xSobre, ySobre);

        float t = 0f;
        const float duracion = 0.45f;
        while (t < duracion)
        {
            t += Time.fixedDeltaTime;
            float k = Mathf.Clamp01(t / duracion);
            Vector2 pos = k < 0.6f
                ? Vector2.Lerp(inicio, arriba, k / 0.6f)
                : Vector2.Lerp(arriba, encima, (k - 0.6f) / 0.4f);
            cuerpoRb.velocity = Vector2.zero;
            cuerpoRb.position = pos;
            yield return new WaitForFixedUpdate();
        }

        cuerpoRb.velocity = Vector2.zero;
        cuerpoRb.position = encima;
        escalando = false;
    }

    float AlturaDelPiso(Vector3 desde)
    {
        Vector2 origen = new Vector2(desde.x, desde.y);
        RaycastHit2D[] golpes = Physics2D.RaycastAll(origen, Vector2.down, 40f, capaPiso);
        float suelo = float.NegativeInfinity;
        for (int i = 0; i < golpes.Length; i++)
        {
            if (golpes[i].collider == null || golpes[i].normal.y < 0.5f)
                continue;
            if (golpes[i].point.y > desde.y - 1.5f)
                continue;
            if (golpes[i].point.y > suelo)
                suelo = golpes[i].point.y;
        }

        if (suelo > float.NegativeInfinity)
            return suelo;
        return yFueraDePantalla;
    }

    void MatarSiSigueDebajo()
    {
        if (yaMato || graceImpactada == null)
            return;

        float ancho = anchoImpacto;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            ancho = Mathf.Max(ancho, sr.bounds.extents.x);

        Collider2D cuerpo = graceImpactada.ColisionCuerpo;
        float x = cuerpo != null ? cuerpo.bounds.center.x : graceImpactada.transform.position.x;
        if (Mathf.Abs(x - xZonaImpacto) > ancho)
            return;

        yaMato = true;
        graceImpactada.MorirPorContainer();
        Invoke(nameof(ReiniciarEscena), Grace.DuracionAnimacionMuerte);
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
