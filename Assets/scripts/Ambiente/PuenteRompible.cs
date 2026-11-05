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

    bool agrietado;
    bool colapsado;
    bool pisoSuelto;
    Grace jugadorSobrePuente;
    Grace causante;
    PolygonCollider2D colliderHundido;
    EdgeCollider2D bordeCurvo;
    Vector2[] curvaLocal;
    float ignorarAterrizajeHasta;
    Sprite spriteSano;

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

    void Update()
    {
        if (colapsado)
        {
            if (!pisoSuelto && !SigueSobreElPuente(causante) && !SigueSobreElPuente(jugadorSobrePuente))
                CancelarColapso();
            return;
        }

        if (jugadorSobrePuente == null)
            return;

        if (!soloCasiRompe && EstaEnLaMitad(jugadorSobrePuente) && DebeColapsarPorCorrer(jugadorSobrePuente))
            IniciarColapso();
    }

    void EvaluarJugador(Collision2D collision, bool esEntrada)
    {
        if (colapsado)
            return;

        Grace grace = Grace.En(collision.collider);
        if (grace == null || !EstaPisandoDesdeArriba(collision, grace))
            return;

        bool colapsarPorSalto =
            esEntrada &&
            Time.time >= ignorarAterrizajeHasta &&
            grace.VieneDeUnSalto &&
            DebeColapsarPorAterrizaje(collision.relativeVelocity.y);

        jugadorSobrePuente = grace;

        if (soloCasiRompe)
        {
            MostrarRoto();
            return;
        }

        if (!EstaEnLaMitad(grace))
            return;

        if (colapsarPorSalto || DebeColapsarPorCorrer(grace))
        {
            causante = grace;
            IniciarColapso();
        }
    }

    Collider2D ColliderActivo()
    {
        if (bordeCurvo != null && bordeCurvo.enabled)
            return bordeCurvo;
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
        float xMin = offset.x - size.x * 0.5f;
        float xMax = offset.x + size.x * 0.5f;

        // Una cadena, no un polígono cerrado. El pie es plano: si la bajada
        // empieza contra el poste, el talón se queda arriba y la punta se clava.
        const int muestras = 42;
        const float radio = 0.1f;
        const float extension = 0.45f;
        float radioLocal = radio / Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
        curvaLocal = new Vector2[muestras];
        for (int i = 0; i < muestras; i++)
        {
            float t = i / (muestras - 1f);
            float x = Mathf.Lerp(xMin - extension, xMax + extension, t);
            float tCurva = Mathf.InverseLerp(xMin, xMax, x);
            float h = HundimientoEn(tCurva);
            curvaLocal[i] = new Vector2(x, yTop - profundidadCurva * h - radioLocal);
        }

        if (bordeCurvo == null)
            bordeCurvo = gameObject.AddComponent<EdgeCollider2D>();

        bordeCurvo.isTrigger = false;
        bordeCurvo.edgeRadius = radio;
        bordeCurvo.sharedMaterial = new PhysicsMaterial2D("PuenteCurvo")
        {
            friction = 0f,
            bounciness = 0f
        };
        bordeCurvo.points = curvaLocal;
        bordeCurvo.enabled = true;
        if (colliderHundido != null)
            colliderHundido.enabled = false;
        colliderPuente.enabled = false;
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

        float yLocal = yTop - profundidadCurva * HundimientoEn(t);
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
        if (colapsado)
            return;

        colapsado = true;
        if (causante == null)
            causante = jugadorSobrePuente;
        bool yaHundido = agrietado;
        MostrarRoto();

        const float inicioHundido = 0.3703704f;
        const float momentoRotura = 0.7222222f;

        if (animator != null)
            animator.Play("puenteRompe", 0, yaHundido ? inicioHundido : 0f);

        float duracion = DuracionClip("puenteRompe");
        if (duracion < 0.2f)
            duracion = 1.5f;

        float inicio = yaHundido ? duracion * inicioHundido : 0f;
        float hastaRotura = momentoRotura - inicio;
        if (hastaRotura < 0.05f)
            hastaRotura = 0.05f;

        Invoke(nameof(SoltarPiso), hastaRotura);
        Invoke(nameof(Desvanecer), (duracion - inicio) + tiempoHastaColapso);
    }

    bool SigueSobreElPuente(Grace grace)
    {
        if (grace == null || colliderPuente == null)
            return false;

        Bounds puente = colliderPuente.bounds;
        float x = grace.CuerpoRigido != null
            ? grace.CuerpoRigido.position.x
            : grace.transform.position.x;
        const float margenSalida = 0.2f;
        if (x <= puente.min.x + margenSalida || x >= puente.max.x - margenSalida)
            return false;

        Collider2D cuerpo = grace.ColisionCuerpo;
        if (cuerpo == null || !cuerpo.enabled)
            return true;

        float pieY = cuerpo.bounds.min.y;
        return pieY <= puente.max.y + 0.55f && pieY >= puente.max.y - 1.8f;
    }

    void CancelarColapso()
    {
        CancelInvoke(nameof(SoltarPiso));
        CancelInvoke(nameof(Desvanecer));
        colapsado = false;
        agrietado = false;
        pisoSuelto = false;
        causante = null;

        if (animator != null)
            animator.enabled = false;

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
        Grace grace = SigueSobreElPuente(causante)
            ? causante
            : (SigueSobreElPuente(jugadorSobrePuente) ? jugadorSobrePuente : null);
        if (grace == null)
        {
            CancelarColapso();
            return;
        }

        pisoSuelto = true;
        grace.CaerDesdePuente();

        if (colliderPuente != null)
            colliderPuente.enabled = false;

        if (colliderHundido != null)
            colliderHundido.enabled = false;

        if (bordeCurvo != null)
            bordeCurvo.enabled = false;
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
