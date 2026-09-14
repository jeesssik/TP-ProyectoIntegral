using UnityEngine;

public class LanchaPatrulla : MonoBehaviour
{
    [Header("Patrulla horizontal")]
    public float limiteIzquierdo = -5f;
    public float limiteDerecho = 5f;
    public float velocidadHorizontal = 1.5f;

    [Header("Movimiento con el agua")]
    public float amplitudVertical = 0.08f;
    public float velocidadVertical = 1.8f;

    [Header("Inclinación")]
    public float amplitudRotacion = 2f;
    public float velocidadRotacion = 1.3f;

    private int direccion = 1;

    private float yInicial;
    private Quaternion rotacionInicial;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        yInicial = transform.position.y;
        rotacionInicial = transform.rotation;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        MoverHorizontalmente();
        Flotar();
    }

    void MoverHorizontalmente()
    {
        transform.position +=
            Vector3.right *
            direccion *
            velocidadHorizontal *
            Time.deltaTime;

        if (transform.position.x >= limiteDerecho)
        {
            direccion = -1;
            GirarLancha();
        }
        else if (transform.position.x <= limiteIzquierdo)
        {
            direccion = 1;
            GirarLancha();
        }
    }

    void Flotar()
    {
        float movimientoY =
            Mathf.Sin(Time.time * velocidadVertical)
            * amplitudVertical;

        Vector3 posicion = transform.position;

        posicion.y =
            yInicial + movimientoY;

        transform.position = posicion;

        float rotacionZ =
            Mathf.Sin(
                Time.time * velocidadRotacion
            ) * amplitudRotacion;

        transform.rotation =
            rotacionInicial *
            Quaternion.Euler(0, 0, rotacionZ);
    }

    void GirarLancha()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX =
                direccion == -1;
        }
    }
}