using UnityEngine;

public class BidonHundible : MonoBehaviour
{
    public float tiempoDeAguante = 1.2f;
    public float aviso = 0.4f;
    public float velocidadHundimiento = 4.5f;
    public float profundidadParaSoltar = 1.6f;

    LanchaFlotando flotacion;
    Rigidbody2D cuerpo;
    Collider2D apoyo;
    float acumulado;
    bool hundiendo;
    Vector2 posicionAlHundir;
    float bajada;

    void Awake()
    {
        flotacion = GetComponent<LanchaFlotando>();
        cuerpo = GetComponent<Rigidbody2D>();
        apoyo = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        if (hundiendo)
        {
            Hundir();
            return;
        }

        if (!HayJugadorEncima())
        {
            acumulado = 0f;
            if (flotacion != null)
                flotacion.extraY = 0f;
            return;
        }

        acumulado += Time.fixedDeltaTime;
        float restante = tiempoDeAguante - acumulado;
        if (flotacion != null && restante < aviso)
        {
            float apuro = 1f - Mathf.Clamp01(restante / Mathf.Max(0.05f, aviso));
            flotacion.extraY = -0.22f * apuro + Mathf.Sin(Time.time * 32f) * 0.035f * apuro;
        }

        if (acumulado < tiempoDeAguante)
            return;

        hundiendo = true;
        posicionAlHundir = cuerpo != null ? cuerpo.position : (Vector2)transform.position;
        if (flotacion != null)
            flotacion.enabled = false;
    }

    void Hundir()
    {
        bajada += velocidadHundimiento * Time.fixedDeltaTime;
        Vector2 destino = posicionAlHundir + Vector2.down * bajada;
        if (cuerpo != null)
            cuerpo.MovePosition(destino);
        else
            transform.position = destino;

        if (bajada < profundidadParaSoltar || apoyo == null)
            return;

        apoyo.enabled = false;
        enabled = false;
    }

    bool HayJugadorEncima()
    {
        if (apoyo == null || !apoyo.enabled)
            return false;

        Bounds zona = apoyo.bounds;
        Vector2 centro = new Vector2(zona.center.x, zona.max.y + 0.15f);
        Vector2 tamano = new Vector2(Mathf.Max(0.4f, zona.size.x * 0.8f), 0.5f);
        Collider2D[] toques = Physics2D.OverlapBoxAll(centro, tamano, 0f);
        for (int i = 0; i < toques.Length; i++)
        {
            if (toques[i] == null || toques[i] == apoyo)
                continue;
            if (Grace.En(toques[i]) != null)
                return true;
        }

        return false;
    }
}
