using System.Collections;
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
    public float tiempoHastaColapso = 0.45f;
    [Tooltip("Acelera el clip puenteRompe para que Grace caiga mientras todavía corre encima.")]
    public float velocidadRotura = 2.2f;
    [Tooltip("Ancho de la franja central, en fracción del puente, donde correr o saltar lo rompe. Los bordes de afuera no disparan la rotura.")]
    [Range(0.2f, 0.5f)]
    public float fraccionZonaCentral = 0.34f;

    [Header("Collider curvo")]
    [Tooltip("Cuánto baja el centro de las tablas respecto al puente sano.")]
    public float profundidadCurva = 0.58f;
    public float espesorTablas = 0.45f;

    static readonly float[] Hundimiento =
    {
        0.00f, 0.06f, 0.22f, 0.52f, 0.88f, 1.00f, 0.72f, 0.32f, 0.08f, 0.00f
    };

    const float SegundoDeQuiebre = 0.7222222f;

    bool agrietado;
    bool colapsado;
    bool pisoSuelto;
    Grace jugadorSobrePuente;
    Grace causante;
    PolygonCollider2D colliderHundido;
    EdgeCollider2D bordeCurvo;
    Vector2[] curvaLocal;
    float ignorarAterrizajeHasta;
    float ultimoContacto = -10f;
    float profundidadUsada;
    float inicioColapso;
    float duracionClip = 1.45f;
    Sprite spriteSano;

    float VentanaContacto => Mathf.Max(0.1f, Time.fixedDeltaTime * 3f);

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

        if (spriteRenderer != null)
            spriteSano = spriteRenderer.sprite;

        EdgeCollider2D colliderCurvo = GetComponent<EdgeCollider2D>();
        if (colliderCurvo != null)
            colliderCurvo.enabled = false;
    }

    public void NotificarSalto(Grace grace)
    {
        if (soloCasiRompe || pisoSuelto || colapsado || grace == null)
            return;
        if (!ContactoFresco() || !EstaEnLaMitad(grace))
            return;

        causante = grace;
        jugadorSobrePuente = grace;
        ultimoContacto = Time.time;
        IniciarColapso();
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
        if (colapsado)
            return;

        if (Grace.En(collision.collider) == jugadorSobrePuente)
            jugadorSobrePuente = null;
    }

    void FixedUpdate()
    {
        if (!colapsado || pisoSuelto || soloCasiRompe)
            return;

        float progreso = ProgresoAnimacion();
        if (progreso < MarcaDeCaida())
        {
            AcompanarHundimiento(progreso);
            MantenerApoyada(causante != null ? causante : jugadorSobrePuente);
            return;
        }

        SoltarPiso();
    }

    float MarcaDeCaida()
    {
        if (duracionClip <= 0.05f)
            return 0.48f;

        return Mathf.Clamp(SegundoDeQuiebre / duracionClip, 0.35f, 0.62f);
    }

    void MantenerApoyada(Grace grace)
    {
        if (grace == null || colliderPuente == null)
            return;

        Rigidbody2D rb = grace.CuerpoRigido;
        Collider2D cuerpo = grace.ColisionCuerpo;
        if (rb == null || cuerpo == null || !cuerpo.enabled)
            return;

        Bounds puente = colliderPuente.bounds;
        if (rb.position.x < puente.min.x + 0.15f || rb.position.x > puente.max.x - 0.15f)
            return;

        float superficie = AlturaMundoEnX(rb.position.x);
        float dy = superficie - cuerpo.bounds.min.y;
        if (dy > -0.04f || dy < -2.2f)
            return;

        rb.position += new Vector2(0f, dy);
        if (rb.velocity.y < 0f)
            rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    float ProgresoAnimacion()
    {
        if (animator != null && animator.enabled)
        {
            AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(0);
            if (estado.IsName("puenteRompe"))
                return Mathf.Clamp01(estado.normalizedTime);
        }

        float duracionReal = duracionClip / Mathf.Max(1f, velocidadRotura);
        if (duracionReal <= 0.05f)
            return 1f;

        return Mathf.Clamp01((Time.time - inicioColapso) / duracionReal);
    }

    void AcompanarHundimiento(float progreso)
    {
        float nQuiebre = MarcaDeCaida();
        float sag = progreso <= nQuiebre
            ? Mathf.SmoothStep(0f, 1f, progreso / nQuiebre)
            : 1f;
        float profundidad = profundidadCurva * sag;
        if (profundidad < 0.02f)
            return;

        bool curvaNueva = bordeCurvo == null || !bordeCurvo.enabled
            || Mathf.Abs(profundidad - profundidadUsada) >= 0.012f;
        profundidadUsada = profundidad;
        agrietado = true;
        if (curvaNueva)
            AplicarColliderHundido(profundidadUsada, 0.08f);

        Grace grace = causante != null ? causante : jugadorSobrePuente;
        if (EstaSobreElTramo(grace))
            RecostarJugador(grace);
    }

    void EvaluarJugador(Collision2D collision, bool esEntrada)
    {
        if (pisoSuelto)
            return;

        Grace grace = Grace.En(collision.collider);
        if (grace == null)
            return;

        bool pies = EstaPisandoDesdeArriba(collision);
        bool despegue = EsDespegueSobreElPuente(collision, grace);
        if (!pies && !despegue)
            return;

        if (pies)
        {
            ultimoContacto = Time.time;
            jugadorSobrePuente = grace;
        }

        if (colapsado)
            return;

        if (soloCasiRompe)
        {
            if (pies)
                MostrarRoto();
            return;
        }

        if (!EstaEnLaMitad(grace))
            return;

        if (DebeColapsarPorCorrer(grace) || EsSaltoQueRompe(collision, grace, esEntrada, despegue))
        {
            causante = grace;
            IniciarColapso();
        }
    }

    bool ContactoFresco()
    {
        return Time.time - ultimoContacto <= VentanaContacto;
    }

    bool EsDespegueSobreElPuente(Collision2D collision, Grace grace)
    {
        Rigidbody2D rb = collision.rigidbody;
        return grace.SaltoReciente &&
            rb != null &&
            rb.velocity.y > 2.5f &&
            ContactoFresco();
    }

    bool EsSaltoQueRompe(Collision2D collision, Grace grace, bool esEntrada, bool despegue)
    {
        if (soloCasiRompe || !grace.SaltoReciente || Time.time < ignorarAterrizajeHasta)
            return false;

        if (despegue)
            return true;

        // El aterrizaje llega con el cuerpo todavía en el aire. Caminar
        // desde el muelle no cuenta: ahí ya estaba en el suelo.
        if (esEntrada && grace.EstaEnElAire)
            return true;

        if (!esEntrada)
            return false;

        float impacto = Mathf.Abs(collision.relativeVelocity.y);
        float caida = collision.rigidbody != null
            ? Mathf.Max(0f, -collision.rigidbody.velocity.y)
            : 0f;
        float umbral = Mathf.Abs(umbralImpactoSalto);
        if (umbral < 3.5f)
            umbral = 3.5f;

        return impacto >= umbral || caida >= umbral;
    }

    Collider2D ColliderActivo()
    {
        if (bordeCurvo != null && bordeCurvo.enabled)
            return bordeCurvo;
        if (colliderHundido != null && colliderHundido.enabled)
            return colliderHundido;
        return colliderPuente;
    }

    bool EstaPisandoDesdeArriba(Collision2D collision)
    {
        Collider2D activo = ColliderActivo();
        if (activo == null || !activo.enabled)
            return false;

        Rigidbody2D rb = collision.rigidbody;
        if (rb != null && rb.velocity.y > 2.2f)
            return false;

        Bounds cubierta = activo.bounds;
        const float insetBorde = 0.2f;
        float xMin = cubierta.min.x + insetBorde;
        float xMax = cubierta.max.x - insetBorde;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contacto = collision.GetContact(i);
            // El signo de la normal depende de quién recibe el choque.
            // En el puente, pararse arriba puede llegar como +Y o como -Y.
            if (Mathf.Abs(contacto.normal.y) < 0.6f)
                continue;
            if (contacto.point.x < xMin || contacto.point.x > xMax)
                continue;

            float ySuperficie = AlturaMundoEnX(contacto.point.x);
            if (contacto.point.y < ySuperficie - 0.28f)
                continue;
            if (contacto.point.y > ySuperficie + 0.35f)
                continue;
            return true;
        }

        return false;
    }

    bool EstaEnLaMitad(Grace grace)
    {
        if (grace == null || colliderPuente == null)
            return false;

        Bounds cubierta = colliderPuente.bounds;
        float x = grace.CuerpoRigido != null
            ? grace.CuerpoRigido.position.x
            : grace.transform.position.x;
        float semi = cubierta.size.x * fraccionZonaCentral * 0.5f;
        return x >= cubierta.center.x - semi && x <= cubierta.center.x + semi;
    }

    bool DebeColapsarPorCorrer(Grace grace)
    {
        if (soloCasiRompe || grace == null)
            return false;

        Rigidbody2D rb = grace.CuerpoRigido;
        return grace.EstaCorriendo &&
            EntradaJuego.HayMovimientoHorizontal &&
            rb != null &&
            Mathf.Abs(rb.velocity.x) > 1.5f;
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

        profundidadUsada = profundidadCurva;
        AplicarColliderHundido(profundidadUsada, 0.45f);
        RecostarJugador(jugadorSobrePuente);
        ignorarAterrizajeHasta = Time.time + 0.4f;
    }

    void AplicarColliderHundido(float profundidad, float extension)
    {
        if (colliderPuente == null)
            return;

        Vector2 offset = colliderPuente.offset;
        Vector2 size = colliderPuente.size;
        float yTop = offset.y + size.y * 0.5f;
        float xMin = offset.x - size.x * 0.5f;
        float xMax = offset.x + size.x * 0.5f;

        // Una cadena, no un polígono cerrado. El pie es plano: si la bajada
        // empieza contra el poste, el talón se queda arriba y la punta se clava.
        const int muestras = 42;
        const float radio = 0.1f;
        float radioLocal = radio / Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
        if (curvaLocal == null || curvaLocal.Length != muestras)
            curvaLocal = new Vector2[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = i / (muestras - 1f);
            float x = Mathf.Lerp(xMin - extension, xMax + extension, t);
            float tCurva = Mathf.InverseLerp(xMin, xMax, x);
            float h = HundimientoEn(tCurva);
            curvaLocal[i] = new Vector2(x, yTop - profundidad * h - radioLocal);
        }

        if (bordeCurvo == null)
        {
            bordeCurvo = gameObject.AddComponent<EdgeCollider2D>();
            bordeCurvo.sharedMaterial = new PhysicsMaterial2D("PuenteCurvo")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        bordeCurvo.isTrigger = false;
        bordeCurvo.edgeRadius = radio;
        bordeCurvo.points = curvaLocal;
        bordeCurvo.enabled = true;
        if (colliderHundido != null)
            colliderHundido.enabled = false;
        colliderPuente.enabled = false;
    }

    bool EstaSobreElTramo(Grace grace)
    {
        if (grace == null || colliderPuente == null)
            return false;

        Bounds puente = colliderPuente.bounds;
        float x = grace.CuerpoRigido != null
            ? grace.CuerpoRigido.position.x
            : grace.transform.position.x;
        if (x < puente.min.x + 0.12f || x > puente.max.x - 0.12f)
            return false;

        Collider2D cuerpo = grace.ColisionCuerpo;
        if (cuerpo == null || !cuerpo.enabled)
            return ContactoFresco();

        float pieY = cuerpo.bounds.min.y;
        float superficie = AlturaMundoEnX(x);
        return pieY <= superficie + 0.7f && pieY >= superficie - 0.9f;
    }

    void RecostarJugador(Grace grace)
    {
        if (grace == null)
            return;

        Rigidbody2D rb = grace.CuerpoRigido;
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
        float xMin = offset.x - size.x * 0.5f;
        float xMax = offset.x + size.x * 0.5f;

        float xLocal = transform.InverseTransformPoint(new Vector3(xMundo, 0f, 0f)).x;
        float t = Mathf.InverseLerp(xMin, xMax, xLocal);
        t = Mathf.Clamp01(t);

        float yLocal = yTop - profundidadUsada * HundimientoEn(t);
        if (!agrietado)
            yLocal = yTop;

        return transform.TransformPoint(new Vector3(xLocal, yLocal, 0f)).y;
    }

    float HundimientoEn(float t)
    {
        t = Mathf.Clamp01(t);
        float f = t * (Hundimiento.Length - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, Hundimiento.Length - 2);
        float u = f - i;
        float h = Mathf.Lerp(Hundimiento[i], Hundimiento[i + 1], u);
        float hombro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.16f))
            * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - t) / 0.16f));
        return h * hombro;
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
        if (colapsado || soloCasiRompe)
            return;

        duracionClip = DuracionClip("puenteRompe");
        if (duracionClip < 0.2f)
            duracionClip = 1.45f;

        float velocidad = Mathf.Max(1f, velocidadRotura);
        inicioColapso = Time.time;
        colapsado = true;
        if (causante == null)
            causante = jugadorSobrePuente;
        if (causante != null)
            causante.AguantarSobrePuente();

        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = velocidad;
            animator.Play("puenteRompe", 0, 0f);
            animator.Update(0f);
        }

        Invoke(nameof(Desvanecer), duracionClip / velocidad + tiempoHastaColapso);
    }

    void CancelarColapso()
    {
        CancelInvoke(nameof(SoltarPiso));
        CancelInvoke(nameof(Desvanecer));
        colapsado = false;
        agrietado = false;
        pisoSuelto = false;
        profundidadUsada = 0f;
        causante = null;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.enabled = false;
        }

        if (spriteRenderer != null)
        {
            if (spriteSano != null)
                spriteRenderer.sprite = spriteSano;
            spriteRenderer.enabled = true;
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        if (bordeCurvo != null)
            bordeCurvo.enabled = false;
        if (colliderHundido != null)
            colliderHundido.enabled = false;
        if (colliderPuente != null)
            colliderPuente.enabled = true;
    }

    float DuracionClip(string nombre)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0f;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == nombre)
                return clips[i].length;
        }

        return 0f;
    }

    void SoltarPiso()
    {
        if (pisoSuelto)
            return;

        Grace grace = causante != null ? causante : jugadorSobrePuente;
        bool sigueEncima = EstaSobreElTramo(grace);
        pisoSuelto = true;

        if (colliderPuente != null)
            colliderPuente.enabled = false;

        if (colliderHundido != null)
            colliderHundido.enabled = false;

        if (bordeCurvo != null)
            bordeCurvo.enabled = false;

        if (sigueEncima)
            grace.CaerDesdePuente();
    }

    void Desvanecer()
    {
        StartCoroutine(FundirSprite());
    }

    IEnumerator FundirSprite()
    {
        if (spriteRenderer == null)
            yield break;

        Color color = spriteRenderer.color;
        float duracion = 0.45f;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, t / duracion);
            spriteRenderer.color = color;
            yield return null;
        }

        spriteRenderer.enabled = false;
    }
}
