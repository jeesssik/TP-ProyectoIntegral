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
    public float umbralImpactoSalto = -2f;
    public float tiempoHastaColapso = 0.2f;

    [Header("Collider curvo")]
    [Tooltip("Cuánto baja el centro de las tablas respecto al puente sano.")]
    public float profundidadCurva = 0.58f;
    public float radioBorde = 0.08f;

    private bool agrietado;
    private bool colapsado;
    private Grace jugadorSobrePuente;
    private EdgeCollider2D colliderCurvo;

    void Awake()
    {
        if (colliderPuente == null)
            colliderPuente = GetComponent<BoxCollider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // El animator pisa el sprite sano de puenteTablas; lo apagamos hasta que haga falta.
        if (animator != null)
            animator.enabled = false;

        colliderCurvo = GetComponent<EdgeCollider2D>();
        if (colliderCurvo == null)
            colliderCurvo = gameObject.AddComponent<EdgeCollider2D>();

        colliderCurvo.enabled = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluarJugador(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        EvaluarJugador(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Grace>() == jugadorSobrePuente)
            jugadorSobrePuente = null;
    }

    void Update()
    {
        if (colapsado || jugadorSobrePuente == null)
            return;

        // Correr encima lo rompe; saltar NO (solo el aterrizaje, en EvaluarJugador).
        if (!soloCasiRompe && DebeColapsarPorCorrer(jugadorSobrePuente))
            IniciarColapso();
    }

    void EvaluarJugador(Collision2D collision)
    {
        if (colapsado)
            return;

        Grace grace = collision.gameObject.GetComponent<Grace>();
        if (grace == null || !EstaPisandoDesdeArriba(collision))
            return;

        jugadorSobrePuente = grace;
        MostrarRoto();

        // relativeVelocity.y < 0 = Grace aterrizando sobre el puente.
        float impactoVertical = collision.relativeVelocity.y;
        if (!soloCasiRompe && DebeColapsarPorAterrizaje(impactoVertical))
            IniciarColapso();
        else if (!soloCasiRompe && DebeColapsarPorCorrer(grace))
            IniciarColapso();
    }

    Collider2D ColliderActivo()
    {
        if (colliderCurvo != null && colliderCurvo.enabled)
            return colliderCurvo;

        return colliderPuente;
    }

    bool EstaPisandoDesdeArriba(Collision2D collision)
    {
        Collider2D activo = ColliderActivo();
        if (activo == null)
            return true;

        float caraSuperior = activo.bounds.max.y;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).point.y >= caraSuperior - 0.35f)
                return true;
        }

        return false;
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

        // Solo cuando cae encima (impacto hacia abajo), no al impulsarse a saltar.
        return impactoVertical < umbralImpactoSalto;
    }

    void MostrarRoto()
    {
        if (agrietado)
            return;

        agrietado = true;

        // Caminando: cruje. Correr o aterrizar encima: colapsa (salvo soloCasiRompe).
        if (animator != null)
        {
            animator.enabled = true;
            animator.Play("casiRompe", 0, 0f);
        }

        if (!soloCasiRompe && spriteRenderer != null && spriteRoto != null)
            spriteRenderer.sprite = SpriteAlineadoAlSano(spriteRenderer.sprite, spriteRoto);

        AplicarColliderCurvo();
    }

    void AplicarColliderCurvo()
    {
        if (colliderPuente == null || colliderCurvo == null)
            return;

        Vector2 offset = colliderPuente.offset;
        Vector2 size = colliderPuente.size;
        float yTop = offset.y + size.y * 0.5f - radioBorde;
        float xMin = offset.x - size.x * 0.5f + 0.12f;
        float xMax = offset.x + size.x * 0.5f - 0.12f;

        // Perfil de puenteTablas_2: los postes quedan altos y el centro se hunde un poco a la derecha.
        float[] hundimiento =
        {
            0.00f, 0.06f, 0.22f, 0.52f, 0.88f, 1.00f, 0.72f, 0.32f, 0.08f, 0.00f
        };

        var puntos = new Vector2[hundimiento.Length];
        for (int i = 0; i < hundimiento.Length; i++)
        {
            float t = i / (hundimiento.Length - 1f);
            puntos[i] = new Vector2(
                Mathf.Lerp(xMin, xMax, t),
                yTop - profundidadCurva * hundimiento[i]);
        }

        colliderCurvo.points = puntos;
        colliderCurvo.edgeRadius = radioBorde;
        colliderPuente.enabled = false;
        colliderCurvo.enabled = true;
    }

    static Sprite SpriteAlineadoAlSano(Sprite sano, Sprite roto)
    {
        float pivotY = roto.pivot.y / roto.rect.height;

        // Los cortes del spritesheet no tienen la misma altura; el pivote central
        // sube el puente roto. Usamos la misma Y de textura que el sprite sano.
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
        MostrarRoto();
        Invoke(nameof(Desaparecer), tiempoHastaColapso);
    }

    void Desaparecer()
    {
        if (colliderPuente != null)
            colliderPuente.enabled = false;

        if (colliderCurvo != null)
            colliderCurvo.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}
