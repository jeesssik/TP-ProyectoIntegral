using System.Collections;
using UnityEngine;

public class CajaTrampa : MonoBehaviour
{
    public Sprite[] reposo;
    public Sprite[] impulso;
    public float fpsImpulso = 18f;
    public int cuadroDeLanzamiento = 2;

    [Header("Boing")]
    public AudioClip sonidoBoing;
    const float inicioBoing = 0.05f;
    const float duracionBoing = 0.62f;
    const float volumenBoing = 0.34f;

    bool disparada;
    BoxCollider2D caja;
    SpriteRenderer vista;

    void Awake()
    {
        caja = GetComponent<BoxCollider2D>();
        vista = GetComponent<SpriteRenderer>();
        Mostrar(reposo, 0);
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
            SonarBoing();
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
                SonarBoing();
            }

            yield return new WaitForSeconds(paso);
        }

        if (!lanzo)
        {
            grace.LanzarAlCielo();
            SonarBoing();
        }
    }

    void SonarBoing()
    {
        AudioClip clip = sonidoBoing;
#if UNITY_EDITOR
        if (clip == null)
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/SFX/boing.mp3");
#endif
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFXForDuration(clip, duracionBoing, volumenBoing, inicioBoing);
    }

    void Mostrar(Sprite[] secuencia, int indice)
    {
        if (vista == null || secuencia == null || indice < 0 || indice >= secuencia.Length)
            return;
        if (secuencia[indice] != null)
            vista.sprite = secuencia[indice];
    }
}
