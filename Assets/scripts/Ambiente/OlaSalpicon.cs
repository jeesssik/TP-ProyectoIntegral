using System;
using UnityEngine;

public class OlaSalpicon : MonoBehaviour
{
    [Header("Capas (animacion generada)")]
    public Sprite[] framesDetras;
    public Sprite[] framesDelante;
    public SpriteRenderer olaDetras;
    public SpriteRenderer olaDelante;

    [Header("Ancla en el agua, en la popa de la lancha")]
    public Transform lancha;
    public Vector2 offsetAgua = new Vector2(-4.1f, -1.55f);

    [Header("Golpe que sigue el frente de la ola")]
    public Vector2 tamanoGolpe = new Vector2(3.6f, 5.2f);
    public float offsetGolpeY = 2.4f;
    public float anchoSpriteUnidades = 11.43f;
    public float pivoteX = 0.9f;

    [Header("Tiempos")]
    public float duracion = 1.7f;
    public float inicioImpacto = 0.28f;
    public float finImpacto = 1.05f;

    Vector3 ancla;
    float tiempo = -1f;
    bool yaGolpeo;
    readonly Collider2D[] bufferGolpe = new Collider2D[12];

    void Awake()
    {
        if (olaDetras == null)
            olaDetras = GetComponent<SpriteRenderer>();
        if (olaDelante == null && transform.childCount > 0)
            olaDelante = transform.GetChild(0).GetComponent<SpriteRenderer>();

        if (framesDetras == null || framesDetras.Length == 0)
            framesDetras = Cargar("Ola/estela/detras");
        if (framesDelante == null || framesDelante.Length == 0)
            framesDelante = Cargar("Ola/estela/delante");

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

        bool yaInundo = tiempo >= inicioImpacto && tiempo <= finImpacto;
        if (yaInundo && !yaGolpeo)
            IntentarTirarAGrace(t);

        if (tiempo < duracion)
            return;

        tiempo = -1f;
        Ocultar();
    }

    public void Reproducir()
    {
        if (framesDetras == null || framesDetras.Length == 0)
            framesDetras = Cargar("Ola/estela/detras");
        if (framesDelante == null || framesDelante.Length == 0)
            framesDelante = Cargar("Ola/estela/delante");

        if (framesDetras.Length == 0)
        {
            Debug.LogError("OlaSalpicon: faltan sprites de la inundacion.", this);
            return;
        }

        ancla = lancha != null ? lancha.position : transform.position;
        ancla.x += offsetAgua.x;
        ancla.y += offsetAgua.y;
        ancla.z = 0f;
        transform.position = ancla;
        transform.localScale = Vector3.one;

        tiempo = 0f;
        yaGolpeo = false;
        MostrarCuadro(0f);
    }

    void MostrarCuadro(float t)
    {
        int indice = Indice(t, framesDetras.Length);

        if (olaDetras != null)
        {
            olaDetras.enabled = true;
            olaDetras.sprite = framesDetras[indice];
        }

        if (olaDelante != null && framesDelante != null && framesDelante.Length > 0)
        {
            int iDelante = Indice(t, framesDelante.Length);
            olaDelante.enabled = true;
            olaDelante.sprite = framesDelante[iDelante];
        }
    }

    void IntentarTirarAGrace(float t)
    {
        Vector2 centro = CentroGolpe(t);
        int n = Physics2D.OverlapBoxNonAlloc(centro, tamanoGolpe, 0f, bufferGolpe);
        for (int i = 0; i < n; i++)
        {
            Collider2D col = bufferGolpe[i];
            if (col == null)
                continue;

            Grace grace = col.GetComponent<Grace>();
            if (grace == null)
                grace = col.GetComponentInParent<Grace>();
            if (grace == null || grace.EstaEnElAire)
                continue;

            yaGolpeo = true;
            grace.EmpujadaPorOla();
            return;
        }
    }

    Vector2 CentroGolpe(float t)
    {
        float frente = FrenteRelativo(t);
        float xLocal = (frente - pivoteX) * anchoSpriteUnidades;
        return (Vector2)ancla + new Vector2(xLocal, offsetGolpeY);
    }

    // Tiene que coincidir con front_x(p) del generador Python.
    static float FrenteRelativo(float p)
    {
        float q = Mathf.Clamp01((p - 0.08f) / 0.72f);
        return 0.86f - 0.8f * (1f - Mathf.Pow(1f - q, 1.7f));
    }

    static int Indice(float t, int cantidad)
    {
        if (cantidad <= 1)
            return 0;
        return Mathf.Min(cantidad - 1, Mathf.FloorToInt(t * cantidad));
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
        float t = Application.isPlaying && tiempo >= 0f ? Mathf.Clamp01(tiempo / duracion) : 0.35f;
        float frente = FrenteRelativo(t);
        float xLocal = (frente - pivoteX) * anchoSpriteUnidades;
        Gizmos.color = new Color(0.15f, 0.55f, 1f, 0.35f);
        Gizmos.DrawWireCube(origen + new Vector3(xLocal, offsetGolpeY, 0f), tamanoGolpe);
    }
}
