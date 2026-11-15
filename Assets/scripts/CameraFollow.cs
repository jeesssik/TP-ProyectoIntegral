using UnityEngine;

[DefaultExecutionOrder(200)]
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    [SerializeField] private Transform target; // El jugador

    [Header("Ajustes de Movimiento")]
    [SerializeField] private float smoothSpeed = 5f; // Velocidad de suavizado
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // Distancia (Z debe ser negativa)

    [Header("Límites de Mapa (Opcional)")]
    [SerializeField] private bool useLimits = false;
    [SerializeField] private float minX, maxX;
    [SerializeField] private float minY, maxY;

    float zFijo;
    bool alturaFijada;
    float yFija;
    bool bordeIzquierdoFijado;
    float xBordeIzquierdo;
    // El extremo del muelle queda un poco adentro de la pantalla, con el poste y agua a la izquierda.
    const float MargenInicioPantalla = 0.12f;

    void Start()
    {
        zFijo = transform.position.z;
    }

    /// <summary>
    /// Al arrancar, el comienzo de la plataforma queda en el encuadre de inicio:
    /// el extremo visible, con un margen de agua a la izquierda.
    /// </summary>
    public void FijarBordeIzquierdo(float xMundo)
    {
        xBordeIzquierdo = xMundo;
        bordeIzquierdoFijado = true;

        Vector3 posicion = transform.position;
        posicion.x = XMinima();
        transform.position = posicion;
    }

    float XMinima()
    {
        float mitad = MitadAncho();
        float margen = mitad * 2f * MargenInicioPantalla;
        return xBordeIzquierdo + mitad - margen;
    }

    float MitadAncho()
    {
        Camera camara = GetComponent<Camera>();
        if (camara == null || !camara.orthographic)
            return 12f;
        return camara.orthographicSize * Mathf.Max(0.01f, camara.aspect);
    }

    /// <summary>Deja de subir con el jugador. El plano del nivel se queda donde está.</summary>
    public void FijarAltura()
    {
        alturaFijada = true;
        yFija = transform.position.y;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // La cámara sigue el plano del nivel. La Z queda fija para que
        // el modelo 3D no arrastre el encuadre hacia el fondo.
        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = zFijo;
        if (alturaFijada)
            desiredPosition.y = yFija;

        // 2. Interpolar suavemente entre la posición actual y la deseada
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. Aplicar límites de mapa si están activos
        if (useLimits)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        if (bordeIzquierdoFijado)
            smoothedPosition.x = Mathf.Max(smoothedPosition.x, XMinima());

        // 4. Asignar la nueva posición
        transform.position = smoothedPosition;
    }
}