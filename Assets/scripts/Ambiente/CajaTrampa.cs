using UnityEngine;

public class CajaTrampa : MonoBehaviour
{
    bool disparada;
    BoxCollider2D caja;

    void Awake()
    {
        caja = GetComponent<BoxCollider2D>();
    }

    void FixedUpdate()
    {
        if (disparada || caja == null)
            return;

        Bounds tapa = caja.bounds;
        Vector2 centro = new Vector2(tapa.center.x, tapa.max.y + 0.12f);
        Vector2 tamano = new Vector2(tapa.size.x * 0.72f, 0.55f);
        Collider2D[] toques = Physics2D.OverlapBoxAll(centro, tamano, 0f);
        for (int i = 0; i < toques.Length; i++)
        {
            if (toques[i] == null || toques[i].gameObject == gameObject)
                continue;

            Grace grace = Grace.En(toques[i]);
            if (grace == null)
                continue;

            disparada = true;
            grace.LanzarAlCielo();
            return;
        }
    }
}
