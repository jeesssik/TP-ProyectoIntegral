using UnityEngine;

public class TrampaLanchaArranque : MonoBehaviour
{
    [Header("Punto de Referencia (Trigger Ola)")]
    public Transform puntoOla;

    [Header("Velocidades")]
    public float velocidadInicio = 2f;
    public float velocidadAcelerado = 18f;
    public float aceleracion = 25f;
    public float tiempoHastaAceleron = 0.4f;

    [Header("Inclinación del Morro")]
    public float anguloElevacionMaximo = 15f;
    public float velocidadInclinacion = 4f;

    [Header("Referencias")]
    public LanchaFlotando scriptFlotacion;
    public OlaSalpicon salpicon;

    float velocidadActual;
    float anguloActualZ;
    float tiempoArranque;
    bool trampaIniciada;
    bool aceleracionViolenta;

    void Update()
    {
        if (!trampaIniciada)
        {
            if (EntradaJuego.HayMovimientoHorizontal)
                IniciarArrancada();
            return;
        }

        if (!aceleracionViolenta)
        {
            velocidadActual = velocidadInicio;

            bool porDistancia = puntoOla != null && transform.position.x >= puntoOla.position.x;
            bool porTiempo = Time.time >= tiempoArranque + tiempoHastaAceleron;
            if (porDistancia || porTiempo)
                DispararAceleron();
        }
        else
        {
            velocidadActual = Mathf.MoveTowards(velocidadActual, velocidadAcelerado, aceleracion * Time.deltaTime);

            float porcentajeVelocidad = velocidadActual / velocidadAcelerado;
            float anguloObjetivo = porcentajeVelocidad * anguloElevacionMaximo;
            anguloActualZ = Mathf.Lerp(anguloActualZ, anguloObjetivo, Time.deltaTime * velocidadInclinacion);
            transform.localRotation = Quaternion.Euler(0f, 0f, anguloActualZ);
        }

        transform.Translate(Vector3.right * velocidadActual * Time.deltaTime, Space.World);
    }

    void IniciarArrancada()
    {
        trampaIniciada = true;
        tiempoArranque = Time.time;

        if (scriptFlotacion != null)
            scriptFlotacion.enabled = false;

        if (salpicon != null)
            salpicon.Reproducir();

        Destroy(gameObject, 4f);
    }

    void DispararAceleron()
    {
        aceleracionViolenta = true;
    }
}
