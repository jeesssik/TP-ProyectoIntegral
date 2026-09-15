using UnityEngine;

public class NubeDerecha: MonoBehaviour
{
    public float velocidad = 0.1f;
    public int direccion = 1;

    public float limiteIzquierdo = -12f;
    public float limiteDerecho = 12f;

    public float amplitudY = 0.05f;
    public float velocidadY = 0.5f;

    private float yInicial;

    void Start()
    {
        yInicial = transform.position.y;
    }

    void Update()
    {
        transform.position += Vector3.right * direccion * velocidad * Time.deltaTime;

        float nuevoY = yInicial + Mathf.Sin(Time.time * velocidadY) * amplitudY;

        transform.position = new Vector3(
            transform.position.x,
            nuevoY,
            transform.position.z
        );

        if (direccion == 1 && transform.position.x > limiteDerecho)
        {
            transform.position = new Vector3(
                limiteIzquierdo,
                yInicial,
                transform.position.z
            );
        }
        else if (direccion == -1 && transform.position.x < limiteIzquierdo)
        {
            transform.position = new Vector3(
                limiteDerecho,
                yInicial,
                transform.position.z
            );
        }
    }
}