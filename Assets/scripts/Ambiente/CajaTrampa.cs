using System.Collections;
using UnityEngine;

public class CajaTrampa : MonoBehaviour
{
    public Sprite[] reposo;
    public Sprite[] impulso;
    public float fpsReposo = 7f;
    public float fpsImpulso = 18f;
    public int cuadroDeLanzamiento = 2;

    bool disparada;
    BoxCollider2D caja;
    SpriteRenderer vista;
    int cuadro;
    float acum;

    void Awake()
    {
        caja = GetComponent<BoxCollider2D>();
        vista = GetComponent<SpriteRenderer>();
        Mostrar(reposo, 0);
    }

    void Update()
    {
        if (disparada || reposo == null || reposo.Length == 0 || vista == null)
            return;

        acum += Time.deltaTime * fpsReposo;
        if (acum < 1f)
            return;

        acum -= 1f;
        cuadro = (cuadro + 1) % reposo.Length;
        Mostrar(reposo, cuadro);
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
            StartCoroutine(Disparar(grace));
            return;
        }
    }

    IEnumerator Disparar(Grace grace)
    {
        if (impulso == null || impulso.Length == 0)
        {
            grace.LanzarAlCielo();
            yield break;
        }

        float paso = 1f / Mathf.Max(1f, fpsImpulso);
        bool lanzo = false;
        for (int i = 0; i < impulso.Length; i++)
        {
            Mostrar(impulso, i);
            if (!lanzo && i >= cuadroDeLanzamiento)
            {
                lanzo = true;
                grace.LanzarAlCielo();
            }

            yield return new WaitForSeconds(paso);
        }

        if (!lanzo)
            grace.LanzarAlCielo();
    }

    void Mostrar(Sprite[] secuencia, int indice)
    {
        if (vista == null || secuencia == null || indice < 0 || indice >= secuencia.Length)
            return;
        if (secuencia[indice] != null)
            vista.sprite = secuencia[indice];
    }
}
