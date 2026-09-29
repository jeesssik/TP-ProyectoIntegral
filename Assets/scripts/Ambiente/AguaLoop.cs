using UnityEngine;

public class AguaLoop : MonoBehaviour
{
    public Transform aguaA;
    public Transform aguaB;
    public float velocidad = 0.5f;

    float ancho;
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        ancho = aguaA.GetComponent<SpriteRenderer>().bounds.size.x;
        aguaB.position = aguaA.position + Vector3.right * ancho;
    }

    void Update()
    {
        Vector3 movimiento = Vector3.left * velocidad * Time.deltaTime;
        aguaA.position += movimiento;
        aguaB.position += movimiento;

        float bordeIzquierdo = cam.ViewportToWorldPoint(Vector3.zero).x;
        ReciclarSiSalio(aguaA, aguaB, bordeIzquierdo);
        ReciclarSiSalio(aguaB, aguaA, bordeIzquierdo);
    }

    void ReciclarSiSalio(Transform actual, Transform otro, float bordeIzquierdo)
    {
        if (actual.position.x + ancho / 2f >= bordeIzquierdo)
            return;

        actual.position = new Vector3(
            otro.position.x + ancho,
            actual.position.y,
            actual.position.z);
    }
}
