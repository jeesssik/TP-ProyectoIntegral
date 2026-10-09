using System.Collections;
using UnityEngine;

/// <summary>
/// Lancha al final del nivel: Grace se sube (idle, detrás del casco), arranca y abre "Nivel completado".
/// </summary>
public class LanchaFinNivel : MonoBehaviour
{
    [Header("Referencias")]
    public LanchaFlotando flotacion;
    public Transform asiento;
    [Tooltip("Punto donde van los pies de Grace, en espacio local de la lancha (cerca del borde inferior del casco).")]
    public Vector3 offsetAsiento = new Vector3(-1.5f, -2.1f, 0f);

    [Header("Capa")]
    public int ordenSortingLancha = 5;
    public int ordenSortingGrace = 4;

    [Header("Viaje")]
    public float delayArranque = 0.45f;
    public float velocidadInicio = 2.5f;
    public float velocidadMaxima = 14f;
    public float aceleracion = 8f;
    public float anguloElevacionMaximo = 10f;
    public float velocidadInclinacion = 3f;
    public float xParaGanar = 145f;

    bool abordada;
    bool viajando;
    bool gano;
    float velocidad;
    float anguloZ;
    Grace grace;
    SpriteRenderer spriteLancha;

    void Awake()
    {
        if (flotacion == null)
            flotacion = GetComponent<LanchaFlotando>();
        if (asiento == null)
            asiento = transform;

        spriteLancha = GetComponent<SpriteRenderer>();
        if (spriteLancha != null)
            spriteLancha.sortingOrder = ordenSortingLancha;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (abordada)
            return;

        Grace g = other.GetComponent<Grace>();
        if (g == null)
            g = other.GetComponentInParent<Grace>();
        if (g == null)
            return;

        Abordar(g);
    }

    void Abordar(Grace g)
    {
        abordada = true;
        grace = g;
        g.SubirseALancha(asiento, offsetAsiento, ordenSortingGrace);
        StartCoroutine(ViajeDeVictoria());
    }

    IEnumerator ViajeDeVictoria()
    {
        yield return new WaitForSeconds(delayArranque);

        if (flotacion != null)
            flotacion.enabled = false;

        viajando = true;
        velocidad = velocidadInicio;

        while (!gano)
        {
            velocidad = Mathf.MoveTowards(velocidad, velocidadMaxima, aceleracion * Time.deltaTime);
            transform.Translate(Vector3.right * velocidad * Time.deltaTime, Space.World);

            float t = Mathf.Clamp01(velocidad / velocidadMaxima);
            float anguloObjetivo = t * anguloElevacionMaximo;
            anguloZ = Mathf.Lerp(anguloZ, anguloObjetivo, Time.deltaTime * velocidadInclinacion);
            transform.rotation = Quaternion.Euler(0f, 0f, anguloZ);

            if (transform.position.x >= xParaGanar)
            {
                gano = true;
                viajando = false;
                break;
            }

            yield return null;
        }

        PantallaNivelCompletado.Mostrar();
    }
}
