using System;
using UnityEngine;

public class OlaSalpicon : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] frames;

    [Header("Ancla en el muelle")]
    public Transform lancha;
    [Tooltip("Desde el centro de la lancha: abajo al agua, un poco hacia Grace.")]
    public Vector2 offsetAgua = new Vector2(-1.1f, -1.7f);

    [Header("Golpe sobre la plataforma")]
    public Vector2 tamanoGolpe = new Vector2(9f, 4.6f);
    public Vector2 offsetGolpe = new Vector2(-0.6f, 2.3f);

    [Header("Tiempos")]
    public float duracion = 1.25f;
    public float inicioImpacto = 0.32f;
    public float finImpacto = 0.95f;

    SpriteRenderer dibujo;
    BoxCollider2D golpe;
    Vector3 ancla;
    float tiempo = -1f;
    bool yaGolpeo;
    readonly Collider2D[] bufferGolpe = new Collider2D[12];

    void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        golpe = GetComponent<BoxCollider2D>();

        if (frames == null || frames.Length == 0)
            frames = CargarFrames();

        if (golpe != null)
        {
            golpe.isTrigger = true;
            golpe.enabled = false;
        }

        Ocultar();
    }

    void LateUpdate()
    {
        if (tiempo < 0f)
            return;

        transform.position = ancla;
        tiempo += Time.deltaTime;

        float t = Mathf.Clamp01(tiempo / duracion);
        int indice = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length));
        if (dibujo != null && frames != null && frames.Length > 0)
            dibujo.sprite = frames[indice];

        bool enImpacto = tiempo >= inicioImpacto && tiempo <= finImpacto;
        if (enImpacto && !yaGolpeo)
            IntentarTirarAGrace();

        if (tiempo < duracion)
            return;

        tiempo = -1f;
        Ocultar();
        if (golpe != null)
            golpe.enabled = false;
    }

    public void Reproducir()
    {
        if (frames == null || frames.Length == 0)
        {
            frames = CargarFrames();
            if (frames == null || frames.Length == 0)
            {
                Debug.LogError("OlaSalpicon: faltan sprites.", this);
                return;
            }
        }

        ancla = lancha != null ? lancha.position : transform.position;
        ancla.x += offsetAgua.x;
        ancla.y += offsetAgua.y;
        ancla.z = 0f;
        transform.position = ancla;

        tiempo = 0f;
        yaGolpeo = false;

        if (dibujo != null)
        {
            dibujo.enabled = true;
            dibujo.sprite = frames[0];
        }
    }

    void IntentarTirarAGrace()
    {
        Vector2 centro = (Vector2)transform.position + offsetGolpe;
        int n = Physics2D.OverlapBoxNonAlloc(centro, tamanoGolpe, 0f, bufferGolpe);
        for (int i = 0; i < n; i++)
        {
            Collider2D col = bufferGolpe[i];
            if (col == null)
                continue;

            Grace grace = col.GetComponent<Grace>();
            if (grace == null)
                grace = col.GetComponentInParent<Grace>();
            if (grace == null)
                continue;

            if (grace.EstaEnElAire)
                continue;

            yaGolpeo = true;
            grace.EmpujadaPorOla();
            return;
        }
    }

    void Ocultar()
    {
        if (dibujo == null)
            return;

        dibujo.sprite = null;
        dibujo.enabled = false;
    }

    static Sprite[] CargarFrames()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Ola/salpicon");
        if (sprites == null || sprites.Length == 0)
            return Array.Empty<Sprite>();

        Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        Vector3 origen = Application.isPlaying ? ancla : transform.position;
        Vector3 centro = origen + (Vector3)offsetGolpe;
        Gizmos.DrawWireCube(centro, tamanoGolpe);
    }
}
