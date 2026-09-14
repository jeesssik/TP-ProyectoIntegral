using UnityEngine;

public class Grace : MonoBehaviour
{
    public float velocidad = 5f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        float movimiento = Input.GetAxisRaw("Horizontal");

        rb.velocity = new Vector2(
            movimiento * velocidad,
            rb.velocity.y
        );

        if (movimiento > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (movimiento < 0)
        {
            spriteRenderer.flipX = true;
        }
    }
}