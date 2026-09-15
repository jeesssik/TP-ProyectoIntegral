using UnityEngine;

public class NubeIzquierda: MonoBehaviour
{
    public float velocidad = 0.1f;
    public int direccion = 1; // 1 = derecha, -1 = izquierda

    public float limiteIzquierdo = -12f;
    public float limiteDerecho = 12f;

    void Update()
    {
        transform.position += Vector3.right * direccion * velocidad * Time.deltaTime;

        if (direccion == 1 && transform.position.x > limiteDerecho)
        {
            transform.position = new Vector3(
                limiteIzquierdo,
                transform.position.y,
                transform.position.z
            );
        }
        else if (direccion == -1 && transform.position.x < limiteIzquierdo)
        {
            transform.position = new Vector3(
                limiteDerecho,
                transform.position.y,
                transform.position.z
            );
        }
    }
}