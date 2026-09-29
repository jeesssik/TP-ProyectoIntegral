using UnityEngine;

public class MovimientoAgua : MonoBehaviour
{
    public float velocidadX = 0.2f;
    public float amplitudY = 0.03f;
    public float velocidadY = 1f;

    Vector3 posicionInicial;

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        transform.position = posicionInicial + new Vector3(
            Time.time * velocidadX,
            FlotacionAgua.OffsetY(amplitudY, velocidadY),
            0f);
    }
}
