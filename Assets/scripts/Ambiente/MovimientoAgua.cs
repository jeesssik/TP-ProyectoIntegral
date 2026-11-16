using System.Collections.Generic;
using UnityEngine;

public class MovimientoAgua : MonoBehaviour
{
    public float velocidadX = 0.2f;
    public float amplitudY = 0.03f;
    public float velocidadY = 1f;

    Vector3 posicionInicial;
    float paso = -1f;
    float xDerechaReposo;
    bool franjaLista;

    static readonly List<MovimientoAgua> activas = new List<MovimientoAgua>();

    void OnEnable()
    {
        activas.Add(this);
    }

    void OnDisable()
    {
        activas.Remove(this);
        franjaLista = false;
        paso = -1f;
    }

    void Awake()
    {
        posicionInicial = transform.position;
    }

    public float AlturaDeReposo => posicionInicial.y;

    public float PosicionX => transform.position.x;

    void Update()
    {
        if (!franjaLista)
            MedirFranja();

        Vector3 p = transform.position;
        p.x += velocidadX * Time.deltaTime;
        p.y = posicionInicial.y + FlotacionAgua.OffsetY(amplitudY, velocidadY);
        transform.position = p;

        // Sale por la derecha y vuelve a entrar por la izquierda.
        // El resto de la franja sigue de largo, sin volver todas juntas al inicio.
        if (paso > 0.5f && p.x > xDerechaReposo + paso && EsLaDeMasALaDerecha())
        {
            p.x = XMasALaIzquierda() - paso;
            transform.position = p;
        }
    }

    void MedirFranja()
    {
        float ancho = AnchoDeLaFranja();
        var saltos = new List<float>();
        var xs = new List<float>();
        for (int i = 0; i < activas.Count; i++)
        {
            MovimientoAgua otra = activas[i];
            if (otra == null || !MismaFranja(otra))
                continue;

            xs.Add(otra.posicionInicial.x);
        }

        xs.Sort();
        for (int i = 1; i < xs.Count; i++)
        {
            float dx = xs[i] - xs[i - 1];
            if (dx > ancho * 0.85f && dx < ancho * 1.25f)
                saltos.Add(dx);
        }

        xDerechaReposo = posicionInicial.x;
        for (int i = 0; i < xs.Count; i++)
            xDerechaReposo = Mathf.Max(xDerechaReposo, xs[i]);

        if (saltos.Count == 0)
            paso = ancho;
        else
        {
            saltos.Sort();
            paso = saltos[saltos.Count / 2];
        }

        franjaLista = true;
    }

    bool EsLaDeMasALaDerecha()
    {
        float x = transform.position.x;
        for (int i = 0; i < activas.Count; i++)
        {
            MovimientoAgua otra = activas[i];
            if (otra == null || otra == this || !MismaFranja(otra))
                continue;
            if (otra.transform.position.x > x)
                return false;
        }

        return true;
    }

    float XMasALaIzquierda()
    {
        float izquierda = transform.position.x;
        bool hayOtra = false;
        for (int i = 0; i < activas.Count; i++)
        {
            MovimientoAgua otra = activas[i];
            if (otra == null || otra == this || !MismaFranja(otra))
                continue;

            hayOtra = true;
            izquierda = Mathf.Min(izquierda, otra.transform.position.x);
        }

        return hayOtra ? izquierda : transform.position.x;
    }

    float AnchoDeLaFranja()
    {
        float ancho = AnchoSprite();
        for (int i = 0; i < activas.Count; i++)
        {
            MovimientoAgua otra = activas[i];
            if (otra == null || !MismaFranja(otra))
                continue;

            ancho = Mathf.Min(ancho, otra.AnchoSprite());
        }

        return ancho;
    }

    bool MismaFranja(MovimientoAgua otra)
    {
        if (Mathf.Abs(otra.velocidadX - velocidadX) > 0.001f)
            return false;

        return Mathf.Abs(otra.posicionInicial.y - posicionInicial.y) < 0.75f;
    }

    float AnchoSprite()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return 21.72f;

        return Mathf.Max(0.5f, sprite.bounds.size.x);
    }
}
