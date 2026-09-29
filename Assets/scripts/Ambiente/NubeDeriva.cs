using UnityEngine;

public class NubeDeriva : MonoBehaviour
{
    public float velocidad = 0.1f;
    public int direccion = 1;
    public float limiteIzquierdo = -12f;
    public float limiteDerecho = 12f;
    public float amplitudY;
    public float velocidadY = 0.5f;

    float yInicial;

    void Start()
    {
        yInicial = transform.position.y;
    }

    void Update()
    {
        float yAlEnvolver = amplitudY != 0f ? yInicial : transform.position.y;
        Vector3 posicion = FlotacionAgua.AvanzarEnLoopX(
            transform.position,
            direccion,
            velocidad,
            limiteIzquierdo,
            limiteDerecho,
            yAlEnvolver);

        if (amplitudY != 0f)
            posicion.y = yInicial + FlotacionAgua.OffsetY(amplitudY, velocidadY);

        transform.position = posicion;
    }
}
