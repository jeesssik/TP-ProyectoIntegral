using UnityEngine;

public class LanchaFlotando : MonoBehaviour
{
    [Header("Movimiento vertical")]
    public float amplitudY = 0.08f;
    public float velocidadY = 1.5f;

    [Header("Rotación")]
    public float amplitudRotacion = 2f;
    public float velocidadRotacion = 1.2f;

    Vector3 posicionInicial;
    Quaternion rotacionInicial;

    void Start()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
    }

    void Update()
    {
        transform.position = posicionInicial + new Vector3(0f, FlotacionAgua.OffsetY(amplitudY, velocidadY), 0f);
        transform.rotation = FlotacionAgua.Rotacion(rotacionInicial, amplitudRotacion, velocidadRotacion);
    }
}
