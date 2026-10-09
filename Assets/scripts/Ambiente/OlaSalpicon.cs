using System;
using UnityEngine;

public class OlaSalpicon : MonoBehaviour
{
    [Header("Capas")]
    public Sprite[] framesDetras;
    public Sprite[] framesDelante;
    public SpriteRenderer olaDetras;
    public SpriteRenderer olaDelante;

    [Header("Ancla en el agua, en la popa de la lancha")]
    public Transform lancha;
    public Vector2 offsetAgua = new Vector2(-4.1f, -1.55f);

    [Header("Zona inundada")]
    [Tooltip("Cuánto se extiende la inundación a la izquierda del ancla (unidades mundo).")]
    public float alcanceIzquierda = 11f;
    [Tooltip("Cuánto se extiende a la derecha del ancla.")]
    public float alcanceDerecha = 1.5f;
    public float altoGolpe = 6f;
    public float offsetGolpeY = 3f;

    [Header("Tiempos")]
    public float duracion = 1.7f;
    public float inicioImpacto = 0.18f;
    public float finImpacto = 1.25f;

    Vector3 ancla;
    float tiempo = -1f;
    bool yaGolpeo;
    bool yaEsquivo;
    Grace graceCache;

    void Awake()
    {
        if (olaDetras == null)
            olaDetras = GetComponent<SpriteRenderer>();
        if (olaDelante == null && transform.childCount > 0)
            olaDelante = transform.GetChild(0).GetComponent<SpriteRenderer>();

        CargarSiFalta();
        Ocultar();
    }

    void LateUpdate()
    {
        if (tiempo < 0f)
            return;

        transform.position = ancla;
        tiempo += Time.deltaTime;

        float t = Mathf.Clamp01(tiempo / duracion);
        MostrarCuadro(t);

        // Si saltó en cualquier momento mientras corre la ola, ya esquivó
        // (así no la pisa al aterrizar todavía inundado).
        if (!yaEsquivo && !yaGolpeo)
            RegistrarEsquivaSiCorresponde();

        if (!yaGolpeo && !yaEsquivo && tiempo >= inicioImpacto && tiempo <= finImpacto)
            IntentarTirarAGrace();

        if (tiempo < duracion)
            return;

        tiempo = -1f;
        Ocultar();
    }

    public void Reproducir()
    {
        CargarSiFalta();

        if (framesDetras == null || framesDetras.Length == 0)
        {
            Debug.LogError("OlaSalpicon: no hay sprites. Revisá Resources/Ola/estela/detras", this);
            return;
        }

        gameObject.SetActive(true);

        if (lancha == null)
        {
            var trampa = FindObjectOfType<TrampaLanchaArranque>();
            if (trampa != null)
                lancha = trampa.transform;
        }

        ancla = lancha != null ? lancha.position : transform.position;
        ancla.x += offsetAgua.x;
        ancla.y += offsetAgua.y;
        ancla.z = 0f;

        transform.SetParent(null, true);
        transform.position = ancla;
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;

        tiempo = 0f;
        yaGolpeo = false;
        yaEsquivo = false;
        graceCache = FindObjectOfType<Grace>();
        MostrarCuadro(0f);
    }

    void CargarSiFalta()
    {
        if (framesDetras == null || framesDetras.Length == 0)
        {
            framesDetras = Cargar("Ola/estela/detras");
            if (framesDetras.Length == 0)
                framesDetras = Cargar("Ola/detras");
        }

        if (framesDelante == null || framesDelante.Length == 0)
        {
            framesDelante = Cargar("Ola/estela/delante");
            if (framesDelante.Length == 0)
                framesDelante = Cargar("Ola/delante");
        }
    }

    void MostrarCuadro(float t)
    {
        // Frame 0 suele ser vacío: arrancamos desde el 1.
        int indice = IndiceVisible(t, framesDetras.Length);
        Sprite cuadro = framesDetras[indice];

        if (olaDetras != null)
        {
            olaDetras.enabled = true;
            olaDetras.sortingOrder = 7;
            olaDetras.color = Color.white;
            olaDetras.sprite = cuadro;
        }

        if (olaDelante != null)
        {
            olaDelante.enabled = true;
            olaDelante.sortingOrder = 12;
            olaDelante.color = Color.white;
            if (framesDelante != null && framesDelante.Length > 0)
                olaDelante.sprite = framesDelante[IndiceVisible(t, framesDelante.Length)];
            else
                olaDelante.sprite = cuadro;
        }
    }

    void RegistrarEsquivaSiCorresponde()
    {
        if (graceCache == null)
            graceCache = FindObjectOfType<Grace>();
        if (graceCache == null)
            return;

        if (graceCache.EstaEsquivandoOla)
            yaEsquivo = true;
    }

    void IntentarTirarAGrace()
    {
        if (graceCache == null)
            graceCache = FindObjectOfType<Grace>();
        if (graceCache == null || yaEsquivo)
            return;

        if (graceCache.EstaEsquivandoOla)
        {
            yaEsquivo = true;
            return;
        }

        if (!GraceEnZonaInundada(graceCache))
            return;

        yaGolpeo = true;
        graceCache.EmpujadaPorOla();
    }

    bool GraceEnZonaInundada(Grace grace)
    {
        Vector3 p = grace.transform.position;
        float xMin = ancla.x - alcanceIzquierda;
        float xMax = ancla.x + alcanceDerecha;
        float yMin = ancla.y - 0.5f;
        float yMax = ancla.y + altoGolpe;

        Collider2D col = grace.GetComponent<Collider2D>();
        if (col != null && col.enabled)
        {
            Bounds b = col.bounds;
            bool solapaX = b.max.x >= xMin && b.min.x <= xMax;
            bool solapaY = b.max.y >= yMin && b.min.y <= yMax;
            if (solapaX && solapaY)
                return true;
        }

        return p.x >= xMin && p.x <= xMax && p.y >= yMin && p.y <= yMax;
    }

    // Salta el frame 0 (alpha 0) y reparte el resto en la duración.
    static int IndiceVisible(float t, int cantidad)
    {
        if (cantidad <= 1)
            return 0;
        if (cantidad == 2)
            return 1;

        int usable = cantidad - 1;
        int i = 1 + Mathf.FloorToInt(Mathf.Clamp01(t) * usable);
        return Mathf.Min(cantidad - 1, i);
    }

    void Ocultar()
    {
        if (olaDetras != null)
        {
            olaDetras.sprite = null;
            olaDetras.enabled = false;
        }

        if (olaDelante != null)
        {
            olaDelante.sprite = null;
            olaDelante.enabled = false;
        }
    }

    static Sprite[] Cargar(string ruta)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(ruta);
        if (sprites == null || sprites.Length == 0)
            return Array.Empty<Sprite>();

        Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origen = Application.isPlaying && tiempo >= 0f ? ancla : transform.position;
        Vector3 centro = new Vector3(
            origen.x + (alcanceDerecha - alcanceIzquierda) * 0.5f,
            origen.y + offsetGolpeY,
            0f);
        Vector3 tam = new Vector3(alcanceIzquierda + alcanceDerecha, altoGolpe, 0.1f);
        Gizmos.color = new Color(0.15f, 0.55f, 1f, 0.35f);
        Gizmos.DrawWireCube(centro, tam);
    }
}
