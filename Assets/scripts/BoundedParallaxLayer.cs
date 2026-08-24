using UnityEngine;

/// <summary>
/// Fondo que se desplaza con la cámara (típicamente 1:1) para que el arte quede fijo en pantalla
/// y siempre se vea el mismo encuadre (ej. la caverna).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(200)]
public class BoundedParallaxLayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Follow camera")]
    [Tooltip("1 = misma velocidad que la cámara (queda fijo en pantalla). Menor que 1 = parallax clásico.")]
    [SerializeField] [Range(0f, 1f)] private float cameraFollowFactor = 1f;

    [SerializeField] private bool followCameraY = true;

    [SerializeField] private bool keepOriginalZ = true;

    private float _startZ;
    private Vector3 _worldOffsetFromCamera;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        _startZ = transform.position.z;

        if (cameraTransform == null)
            return;

        _worldOffsetFromCamera = transform.position - cameraTransform.position;
        if (!followCameraY)
            _worldOffsetFromCamera.y = 0f;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 camPos = cameraTransform.position;
        Vector3 targetPos = camPos + _worldOffsetFromCamera;

        if (cameraFollowFactor < 0.999f)
        {
            Vector3 pos = transform.position;
            Vector3 delta = targetPos - pos;
            delta.x *= cameraFollowFactor;
            if (followCameraY)
                delta.y *= cameraFollowFactor;
            else
                delta.y = 0f;

            targetPos = pos + delta;
        }

        if (!followCameraY)
            targetPos.y = transform.position.y;

        if (keepOriginalZ)
            targetPos.z = _startZ;

        transform.position = targetPos;
    }
}
