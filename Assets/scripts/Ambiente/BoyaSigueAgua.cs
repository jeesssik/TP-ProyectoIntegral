using UnityEngine;

/// <summary>
/// Avanza a la misma velocidad que la franja de agua más cercana y se mece encima.
/// </summary>
public class BoyaSigueAgua : MonoBehaviour
{
    const float amplitudFlote = 0.06f;
    const float amplitudInclinacion = 2.8f;

    MovimientoAgua agua;
    Vector3 posicionInicial;
    Quaternion rotacionInicial;
    float fase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EngancharBoyasDeLaEscena()
    {
        SpriteRenderer[] sprites = FindObjectsOfType<SpriteRenderer>();
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer sprite = sprites[i];
            if (sprite == null || sprite.sprite == null)
                continue;
            if (!sprite.sprite.name.StartsWith("boya"))
                continue;
            if (sprite.GetComponent<BoyaSigueAgua>() != null)
                continue;

            sprite.gameObject.AddComponent<BoyaSigueAgua>();
        }
    }

    void Start()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
        fase = posicionInicial.x * 0.37f;
        agua = FranjaMasCercana();
    }

    void LateUpdate()
    {
        if (agua == null)
            agua = FranjaMasCercana();
        if (agua == null)
            return;

        posicionInicial.x += agua.velocidadX * Time.deltaTime;

        float oleaje = FlotacionAgua.OffsetY(agua.amplitudY, agua.velocidadY);
        float flote = Mathf.Sin(Time.time * agua.velocidadY * 0.85f + fase) * amplitudFlote;
        float inclinacion = Mathf.Sin(Time.time * agua.velocidadY * 1.15f + fase + 1.2f) * amplitudInclinacion;

        Vector3 p = posicionInicial;
        p.y += oleaje + flote;
        transform.position = p;
        transform.rotation = rotacionInicial * Quaternion.Euler(0f, 0f, inclinacion);
    }

    MovimientoAgua FranjaMasCercana()
    {
        MovimientoAgua[] franjas = FindObjectsOfType<MovimientoAgua>();
        MovimientoAgua mejor = null;
        float menorDistancia = float.MaxValue;
        float altura = posicionInicial.y;

        for (int i = 0; i < franjas.Length; i++)
        {
            MovimientoAgua franja = franjas[i];
            if (franja == null)
                continue;

            float distancia = Mathf.Abs(franja.AlturaDeReposo - altura);
            if (distancia >= menorDistancia)
                continue;

            menorDistancia = distancia;
            mejor = franja;
        }

        return mejor;
    }
}
