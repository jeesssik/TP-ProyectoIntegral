using UnityEngine;

public class FlightMovement2D : MonoBehaviour
{
    [Header("Configuración de Vuelo")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool flyToRight = true;

    [Header("Ajustes de Cámara")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float padding = 1f; // Margen fuera de pantalla antes de reiniciar

    [Header("Variación de Altura (Eje Y)")]
    [SerializeField] private bool randomizeHeight = true;
    [SerializeField] private float minY = 1f;
    [SerializeField] private float maxY = 5f;

    private float fixedZ;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Fijar la profundidad Z original para que jamás cambie en 2D
        fixedZ = transform.position.z;
    }

    private void Update()
    {
        // 1. Mover el objeto en coordenadas del mundo
        Vector3 direction = flyToRight ? Vector3.right : Vector3.left;
        transform.position += direction * speed * Time.deltaTime;

        // 2. Comprobar si ha salido por completo del borde de la cámara
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        if (flyToRight && viewportPos.x > 1f + padding)
        {
            RespawnOnLeft();
        }
        else if (!flyToRight && viewportPos.x < 0f - padding)
        {
            RespawnOnRight();
        }
    }

    private void RespawnOnLeft()
    {
        // Aparece justo por la izquierda del borde de la cámara
        Vector3 leftEdge = mainCamera.ViewportToWorldPoint(new Vector3(0f - padding, 0.5f, mainCamera.nearClipPlane));
        float newY = randomizeHeight ? Random.Range(minY, maxY) : transform.position.y;

        transform.position = new Vector3(leftEdge.x, newY, fixedZ);
    }

    private void RespawnOnRight()
    {
        // Aparece justo por la derecha del borde de la cámara
        Vector3 rightEdge = mainCamera.ViewportToWorldPoint(new Vector3(1f + padding, 0.5f, mainCamera.nearClipPlane));
        float newY = randomizeHeight ? Random.Range(minY, maxY) : transform.position.y;

        transform.position = new Vector3(rightEdge.x, newY, fixedZ);
    }
}