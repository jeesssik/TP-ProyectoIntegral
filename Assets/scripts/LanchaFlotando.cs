using UnityEngine;

public class LanchaFlotando : MonoBehaviour
{
    [Header("Movimiento vertical")]
    public float amplitudY = 0.08f;
    public float velocidadY = 1.5f;

    [Header("Rotación")]
    public float amplitudRotacion = 2f;
    public float velocidadRotacion = 1.2f;

    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    void Start()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
    }

    void Update()
    {
        // Subir y bajar
        float movimientoY =
            Mathf.Sin(Time.time * velocidadY) * amplitudY;

        transform.position =
            posicionInicial + new Vector3(0, movimientoY, 0);

        // Inclinar suavemente
        float rotacionZ =
            Mathf.Sin(Time.time * velocidadRotacion)
            * amplitudRotacion;

        transform.rotation =
            rotacionInicial * Quaternion.Euler(0, 0, rotacionZ);
    }
}