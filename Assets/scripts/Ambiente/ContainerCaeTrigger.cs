using UnityEngine;
using UnityEngine.SceneManagement;

public class ContainerCaeTrigger : MonoBehaviour
{
    public Animator containerAnimator;
    public Transform containerTransform;
    public string estadoNormal = "ContainerMeciendose";
    public string estadoCaida = "ContainerCae";
    public float tiempoHastaImpacto = 0.7f;
    public float tiempoAnimacionMuerte = 1.65f;
    public float velocidadCaidaContainer = 14f;
    public float distanciaSobreGrace = 1.2f;

    bool activado;
    bool containerCayendo;
    Grace graceImpactada;
    Vector3 posicionTirantesInicial;
    Quaternion rotacionTirantesInicial;

    void Update()
    {
        if (activado && containerAnimator != null)
        {
            containerAnimator.transform.position = posicionTirantesInicial;
            containerAnimator.transform.rotation = rotacionTirantesInicial;
        }

        if (!containerCayendo || graceImpactada == null || containerTransform == null)
            return;

        Vector3 posicionObjetivo = graceImpactada.transform.position;
        posicionObjetivo.y += distanciaSobreGrace;
        containerTransform.position = Vector3.MoveTowards(
            containerTransform.position,
            posicionObjetivo,
            velocidadCaidaContainer * Time.deltaTime
        );
    }

    void Awake()
    {
        if (containerAnimator != null)
        {
            posicionTirantesInicial = containerAnimator.transform.position;
            rotacionTirantesInicial = containerAnimator.transform.rotation;
            containerAnimator.enabled = true;
            containerAnimator.Play(estadoNormal, 0, 0f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activado || other.GetComponent<Grace>() == null || containerAnimator == null)
            return;

        activado = true;
        graceImpactada = other.GetComponent<Grace>();
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.enabled = false;

        containerAnimator.enabled = true;
        containerAnimator.Play(estadoCaida, 0, 0f);
        Invoke(nameof(Impactar), tiempoHastaImpacto);
    }

    void Impactar()
    {
        if (graceImpactada != null)
        {
            containerCayendo = true;
            if (containerTransform != null)
                containerTransform.gameObject.SetActive(true);

            graceImpactada.MorirPorContainer();
        }

        Invoke(nameof(ReiniciarEscena), tiempoAnimacionMuerte);
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
