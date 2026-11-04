using UnityEngine;

public class LanchaFlotando : MonoBehaviour
{
    [Header("Movimiento vertical")]
    public float amplitudY = 0.08f;
    public float velocidadY = 1.5f;

    [Header("Rotación")]
    public float amplitudRotacion = 2f;
    public float velocidadRotacion = 1.2f;

    [HideInInspector]
    public float extraY;

    Vector3 posicionInicial;
    Quaternion rotacionInicial;
    Rigidbody2D cuerpo;

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;

        if (cuerpo == null)
            return;

        cuerpo.bodyType = RigidbodyType2D.Kinematic;
        cuerpo.gravityScale = 0f;
        cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
        cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Collider2D apoyo = GetComponent<Collider2D>();
        if (apoyo != null)
        {
            apoyo.sharedMaterial = new PhysicsMaterial2D("Bidon")
            {
                friction = 0.9f,
                bounciness = 0f
            };
        }
    }

    void Update()
    {
        if (cuerpo != null)
            return;

        transform.position = posicionInicial + new Vector3(0f, FlotacionAgua.OffsetY(amplitudY, velocidadY) + extraY, 0f);
        transform.rotation = FlotacionAgua.Rotacion(rotacionInicial, amplitudRotacion, velocidadRotacion);
    }

    void FixedUpdate()
    {
        if (cuerpo == null)
            return;

        Vector3 pos = posicionInicial + new Vector3(0f, FlotacionAgua.OffsetY(amplitudY, velocidadY) + extraY, 0f);
        cuerpo.MovePosition(pos);
        cuerpo.MoveRotation(rotacionInicial.eulerAngles.z + FlotacionAgua.OffsetY(amplitudRotacion, velocidadRotacion));
    }
}
