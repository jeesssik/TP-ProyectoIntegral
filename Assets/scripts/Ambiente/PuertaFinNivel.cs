using System.Collections;
using UnityEngine;

/// <summary>
/// Puerta del edificio final. Grace entra al vano negro y el nivel queda superado.
/// </summary>
public class PuertaFinNivel : MonoBehaviour
{
    [Header("Vano")]
    [Tooltip("Caja del vano, en unidades locales del sprite (antes de la escala).")]
    public Vector2 offsetEntrada = new Vector2(0f, -2.82f);
    public Vector2 tamanoEntrada = new Vector2(0.46f, 0.55f);
    public float esperaTrasEntrar = 0.4f;

    bool ganada;

    void Awake()
    {
        BoxCollider2D caja = GetComponent<BoxCollider2D>();
        if (caja == null)
            caja = gameObject.AddComponent<BoxCollider2D>();

        caja.isTrigger = true;
        caja.offset = offsetEntrada;
        caja.size = tamanoEntrada;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ganada)
            return;

        Grace grace = Grace.En(other);
        if (grace == null)
            return;

        ganada = true;
        grace.EntrarAlEdificioFinal();
        StartCoroutine(CerrarNivel());
    }

    IEnumerator CerrarNivel()
    {
        yield return new WaitForSeconds(esperaTrasEntrar);
        PantallaNivelCompletado.Mostrar();
    }
}
