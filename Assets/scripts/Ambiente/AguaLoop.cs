using UnityEngine;

public class AguaLoop : MonoBehaviour
{
    public Transform aguaA;
    public Transform aguaB;

    public float velocidad = 0.5f;

    private float ancho;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;

        ancho = aguaA.GetComponent<SpriteRenderer>().bounds.size.x;

        aguaB.position =
            aguaA.position + Vector3.right * ancho;
    }

    void Update()
    {
        Vector3 movimiento =
            Vector3.left * velocidad * Time.deltaTime;

        aguaA.position += movimiento;
        aguaB.position += movimiento;

        float bordeIzquierdo =
            cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;

        if (aguaA.position.x + ancho / 2f < bordeIzquierdo)
        {
            aguaA.position = new Vector3(
                aguaB.position.x + ancho,
                aguaA.position.y,
                aguaA.position.z
            );
        }

        if (aguaB.position.x + ancho / 2f < bordeIzquierdo)
        {
            aguaB.position = new Vector3(
                aguaA.position.x + ancho,
                aguaB.position.y,
                aguaB.position.z
            );
        }
    }
}