using System.Collections.Generic;
using UnityEngine;

public class MovimientoAgua : MonoBehaviour
{
    public float velocidadX = 0.2f;
    public float amplitudY = 0.03f;
    public float velocidadY = 1f;

    Vector3 posicionInicial;
    float periodo = -1f;

    static readonly List<MovimientoAgua> activas = new List<MovimientoAgua>();

    void OnEnable()
    {
        activas.Add(this);
    }

    void OnDisable()
    {
        activas.Remove(this);
        periodo = -1f;
    }

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        float avance = Time.time * velocidadX;
        if (periodo < 0f)
            periodo = CalcularPeriodo();

        // La misma imagen se repite al lado de la otra. Al recorrer un ancho,
        // cada pieza ocupa el lugar de la anterior y el dibujo no se corta.
        if (periodo > 0.5f)
            avance = Mathf.Repeat(avance, periodo);

        transform.position = posicionInicial + new Vector3(
            avance,
            FlotacionAgua.OffsetY(amplitudY, velocidadY),
            0f);
    }

    float CalcularPeriodo()
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

        if (saltos.Count == 0)
            return ancho;

        saltos.Sort();
        return saltos[saltos.Count / 2];
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
