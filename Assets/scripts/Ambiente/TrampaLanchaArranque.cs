using UnityEngine;

public class TrampaLanchaArranque : MonoBehaviour
{
    [Header("Punto de Referencia (Trigger Ola)")]
    public Transform puntoOla; // Ubicado a la mitad de la ola

    [Header("Velocidades")]
    public float velocidadInicio = 2f;    
    public float velocidadAcelerado = 18f; 
    public float aceleracion = 25f;        

    [Header("Inclinación del Morro")]
    public float anguloElevacionMaximo = 15f; // Grados que se levanta la trompa (ajustable)
    public float velocidadInclinacion = 4f;   // Qué tan rápido se levanta la trompa

    [Header("Referencia al Script de Flotación")]
    public LanchaFlotando scriptFlotacion; 

    private float velocidadActual = 0f;
    private float anguloActualZ = 0f;
    private bool trampaIniciada = false;
    private bool aceleracionViolenta = false;

    void Update()
    {
        // 1. Esperar al primer movimiento del jugador
        if (!trampaIniciada)
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f)
            {
                IniciarArrancada();
            }
            return;
        }

        // 2. Control de velocidad y acelerón
        if (!aceleracionViolenta)
        {
            velocidadActual = velocidadInicio;

            if (transform.position.x >= puntoOla.position.x)
            {
                aceleracionViolenta = true;
            }
        }
        else
        {
            // Acelera progresivamente hacia la velocidad máxima
            velocidadActual = Mathf.MoveTowards(velocidadActual, velocidadAcelerado, aceleracion * Time.deltaTime);

            // 3. Levantar el morro (rotación en Z) solo en la fase de aceleración violenta
            float porcentajeVelocidad = velocidadActual / velocidadAcelerado;
            float anguloObjetivo = porcentajeVelocidad * anguloElevacionMaximo;

            anguloActualZ = Mathf.Lerp(anguloActualZ, anguloObjetivo, Time.deltaTime * velocidadInclinacion);
            
            // Aplica la rotación manteniendo la posición
            transform.localRotation = Quaternion.Euler(0f, 0f, anguloActualZ);
        }

        // 4. Traslación hacia la derecha
        transform.Translate(Vector3.right * velocidadActual * Time.deltaTime, Space.World);
    }

    void IniciarArrancada()
    {
        trampaIniciada = true;

        if (scriptFlotacion != null)
        {
            scriptFlotacion.enabled = false;
        }

        Destroy(gameObject, 4f);
    }
}