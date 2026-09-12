using UnityEngine;

public class MovimientoAgua : MonoBehaviour
{
    public float velocidadX = 0.2f;
    public float amplitudY = 0.03f;
    public float velocidadY = 1f;

    private Vector3 posicionInicial;

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        float x = Time.time * velocidadX;
        float y = Mathf.Sin(Time.time * velocidadY) * amplitudY;

        transform.position = posicionInicial + new Vector3(x, y, 0);
    }
}