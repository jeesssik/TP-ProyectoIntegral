using UnityEngine;

public class PuenteRompible : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public BoxCollider2D colliderPuente;

    [Header("Collider durante la rotura")]
    public Vector2 offsetNormal;
    public Vector2 sizeNormal;

    public Vector2 offsetRoto;
    public Vector2 sizeRoto;

    [Header("Tiempos")]
    public float tiempoHastaDesnivel = 0.35f;
    public float tiempoHastaCaida = 0.8f;

    private bool activado = false;

    void Start()
    {
        if (colliderPuente != null)
        {
            offsetNormal = colliderPuente.offset;
            sizeNormal = colliderPuente.size;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (activado) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            activado = true;
            animator.SetTrigger("Romper");

            Invoke(nameof(BajarCollider), tiempoHastaDesnivel);
            Invoke(nameof(QuitarCollider), tiempoHastaCaida);
        }
    }

    void BajarCollider()
    {
        colliderPuente.offset = offsetRoto;
        colliderPuente.size = sizeRoto;
    }

    void QuitarCollider()
    {
        colliderPuente.enabled = false;
    }
}