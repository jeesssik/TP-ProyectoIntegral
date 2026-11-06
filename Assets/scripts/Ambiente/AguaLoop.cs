using UnityEngine;

public class AguaLoop : MonoBehaviour
{
    [SerializeField] Transform aguaA;
    [SerializeField] Transform aguaB;
    [SerializeField] float velocidad = 0.5f;

    float ancho;
    Camera cam;
    bool listo;

    void Start()
    {
        if (aguaA == null || aguaB == null)
        {
            enabled = false;
            return;
        }

        cam = Camera.main;
        SpriteRenderer spriteAgua = aguaA.GetComponent<SpriteRenderer>();
        if (spriteAgua == null || cam == null)
        {
            enabled = false;
            return;
        }

        ancho = spriteAgua.bounds.size.x;
        aguaB.rotation = aguaA.rotation;
        aguaB.position = aguaA.position + Vector3.right * ancho;
        listo = true;
    }

    void Update()
    {
        if (!listo || aguaA == null || aguaB == null || cam == null)
            return;

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
