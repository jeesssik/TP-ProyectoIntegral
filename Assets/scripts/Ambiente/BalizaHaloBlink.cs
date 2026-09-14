using UnityEngine;

public class BalizaHaloBlink : MonoBehaviour
{
    public SpriteRenderer halo;

    [Header("Alpha del halo")]
    public float alphaMin = 0.05f;
    public float alphaMax = 0.85f;

    [Header("Velocidad")]
    public float velocidad = 2f;

    [Header("Escala opcional")]
    public bool animarEscala = true;
    public float escalaMin = 0.9f;
    public float escalaMax = 1.05f;

    [Header("Desfase aleatorio")]
    public bool randomOffset = true;

    private Vector3 escalaInicial;
    private float offset;

    void Start()
    {
        if (halo == null)
        {
            Debug.LogWarning("No asignaste el SpriteRenderer del halo.");
            return;
        }

        escalaInicial = halo.transform.localScale;

        if (randomOffset)
        {
            offset = Random.Range(0f, 10f);
        }
    }

    void Update()
    {
        if (halo == null) return;

        float t = (Mathf.Sin((Time.time + offset) * velocidad) + 1f) / 2f;

        float alphaActual = Mathf.Lerp(alphaMin, alphaMax, t);

        Color color = halo.color;
        color.a = alphaActual;
        halo.color = color;

        if (animarEscala)
        {
            float escalaActual = Mathf.Lerp(escalaMin, escalaMax, t);
            halo.transform.localScale = escalaInicial * escalaActual;
        }
    }
}