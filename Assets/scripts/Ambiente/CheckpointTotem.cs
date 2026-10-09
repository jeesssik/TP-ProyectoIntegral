using System;
using UnityEngine;

/// <summary>
/// Tótem: al acercarse/pasarlo, cierra la zona invertida, pasa a verde y arma la cascada.
/// Sin collider sólido.
/// </summary>
public class CheckpointTotem : MonoBehaviour
{
    const string RecursosTerminal = "Terminales industriales_ rojo y verde";

    [Header("Visual")]
    public SpriteRenderer totem;
    public Sprite spriteApagado;   // rojo
    public Sprite spriteActivado;  // verde

    [Header("Detección")]
    [Tooltip("Activa cuando Grace llega a esta distancia a la izquierda del borde izquierdo del tótem.")]
    public float anticipoActivacion = 0.8f;

    [Header("Cascada")]
    public PlataformaQueCae[] plataformas;

    bool yaTocado;
    Grace grace;

    void Awake()
    {
        if (totem == null)
            totem = GetComponent<SpriteRenderer>();

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        CargarSpritesSiFaltan();

        if (totem != null && spriteApagado != null)
        {
            totem.color = Color.white;
            totem.sprite = spriteApagado;
        }
    }

    void Start()
    {
        grace = FindObjectOfType<Grace>();
    }

    void Update()
    {
        if (!yaTocado)
            IntentarActivar();
    }

    void IntentarActivar()
    {
        if (grace == null)
        {
            grace = FindObjectOfType<Grace>();
            if (grace == null)
                return;
        }

        if (!GraceLlegoAlTotem())
            return;

        yaTocado = true;
        EntradaJuego.InvertidoHorizontal = false;

        if (LaserInversionControles.Instancia != null)
            LaserInversionControles.Instancia.CerrarZona();

        if (totem != null && spriteActivado != null)
        {
            totem.color = Color.white;
            totem.sprite = spriteActivado;
        }

        ArmarCascada();
    }

    bool GraceLlegoAlTotem()
    {
        float xActivacion = transform.position.x - anticipoActivacion;
        float yMin = transform.position.y - 2f;
        float yMax = transform.position.y + 12f;

        if (totem != null && totem.sprite != null)
        {
            Bounds b = totem.bounds;
            // Antes del borde izquierdo: controles normales y pasa libre.
            xActivacion = b.min.x - anticipoActivacion;
            yMin = b.min.y - 2f;
            yMax = b.max.y + 2f;
        }

        Vector3 p = grace.transform.position;
        return p.x >= xActivacion && p.y >= yMin && p.y <= yMax;
    }

    /// <summary>Borde izquierdo del tótem: ahí termina la zona invertida del láser.</summary>
    public float XFinZonaInvertida()
    {
        if (totem != null && totem.sprite != null)
            return totem.bounds.min.x;
        return transform.position.x;
    }

    void ArmarCascada()
    {
        if (plataformas == null)
            return;

        for (int i = 0; i < plataformas.Length; i++)
        {
            if (plataformas[i] != null)
                plataformas[i].Armar();
        }
    }

    void CargarSpritesSiFaltan()
    {
        if (spriteApagado != null && spriteActivado != null)
            return;

        Sprite[] todos = Resources.LoadAll<Sprite>(RecursosTerminal);
        if (todos == null || todos.Length == 0)
            return;

        if (spriteApagado == null)
            spriteApagado = Buscar(todos, "_0");
        if (spriteActivado == null)
            spriteActivado = Buscar(todos, "_4");
    }

    static Sprite Buscar(Sprite[] sprites, string sufijo)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.EndsWith(sufijo, StringComparison.Ordinal))
                return sprites[i];
        }
        return null;
    }
}
