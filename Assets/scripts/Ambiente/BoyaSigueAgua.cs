using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cada boya avanza a la velocidad de la franja de agua que tiene debajo y se mece encima.
/// </summary>
public class BoyaSigueAgua : MonoBehaviour
{
    const float amplitudFlote = 0.06f;
    const float amplitudInclinacion = 2.8f;

    MovimientoAgua agua;
    Vector3 posicionInicial;
    Quaternion rotacionInicial;
    float fase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        EngancharBoyasDeLaEscena();
    }

    static void EngancharBoyasDeLaEscena()
    {
        SpriteRenderer[] sprites = FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer sprite = sprites[i];
            if (sprite == null || !EsBoya(sprite))
                continue;
            if (sprite.GetComponent<BoyaSigueAgua>() != null)
                continue;

            sprite.gameObject.AddComponent<BoyaSigueAgua>();
        }
    }

    static bool EsBoya(SpriteRenderer sprite)
    {
        if (sprite.gameObject.name.StartsWith("boya", StringComparison.OrdinalIgnoreCase))
            return true;

        return sprite.sprite != null
            && sprite.sprite.name.StartsWith("boya", StringComparison.OrdinalIgnoreCase);
    }

    void Start()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
        fase = posicionInicial.x * 0.37f;
        agua = FranjaDebajo();
    }

    void LateUpdate()
    {
        if (agua == null)
            agua = FranjaDebajo();
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

    MovimientoAgua FranjaDebajo()
    {
        MovimientoAgua[] franjas = FindObjectsOfType<MovimientoAgua>();
        MovimientoAgua mejor = null;
        float menorDistancia = float.MaxValue;
        float x = posicionInicial.x;
        float y = posicionInicial.y;

        for (int i = 0; i < franjas.Length; i++)
        {
            MovimientoAgua franja = franjas[i];
            if (franja == null || !CubreEnX(franja, x))
                continue;

            float distancia = Mathf.Abs(franja.AlturaDeReposo - y);
            if (distancia >= menorDistancia)
                continue;

            menorDistancia = distancia;
            mejor = franja;
        }

        if (mejor != null)
            return mejor;

        for (int i = 0; i < franjas.Length; i++)
        {
            MovimientoAgua franja = franjas[i];
            if (franja == null)
                continue;

            float distancia = Mathf.Abs(franja.AlturaDeReposo - y);
            if (distancia >= menorDistancia)
                continue;

            menorDistancia = distancia;
            mejor = franja;
        }

        return mejor;
    }

    static bool CubreEnX(MovimientoAgua franja, float x)
    {
        SpriteRenderer sprite = franja.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return false;

        Bounds zona = sprite.bounds;
        return x >= zona.min.x - 1.5f && x <= zona.max.x + 1.5f;
    }
}
