using UnityEngine;

public class FlightMovement : MonoBehaviour
{
    [Header("Configuración de Vuelo")]
    [SerializeField] float speed = 3f;
    [SerializeField] bool flyToRight = true;

    [Header("Ajustes de Cámara")]
    [SerializeField] Camera mainCamera;
    [SerializeField] float padding = 1f;

    [Header("Variación de Altura (Eje Y)")]
    [SerializeField] bool randomizeHeight = true;
    [SerializeField] float minY = 1f;
    [SerializeField] float maxY = 5f;

    float fixedZ;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        fixedZ = transform.position.z;
    }

    void Update()
    {
        Vector3 direction = flyToRight ? Vector3.right : Vector3.left;
        transform.position += direction * speed * Time.deltaTime;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        if (flyToRight && viewportPos.x > 1f + padding)
            RespawnOnLeft();
        else if (!flyToRight && viewportPos.x < 0f - padding)
            RespawnOnRight();
    }

    void RespawnOnLeft()
    {
        RespawnEnBorde(0f - padding);
    }

    void RespawnOnRight()
    {
        RespawnEnBorde(1f + padding);
    }

    void RespawnEnBorde(float viewportX)
    {
        Vector3 borde = mainCamera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, mainCamera.nearClipPlane));
        float newY = randomizeHeight ? Random.Range(minY, maxY) : transform.position.y;
        transform.position = new Vector3(borde.x, newY, fixedZ);
    }
}
