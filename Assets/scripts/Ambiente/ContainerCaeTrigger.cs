using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ContainerCaeTrigger : MonoBehaviour
{
    public Animator containerAnimator;
    public Transform containerTransform;
    public string estadoNormal = "ContainerMeciendose";
    public string estadoCaida = "ContainerCae";
    public float tiempoHastaImpacto = 0.7f;
    public float velocidadCaidaContainer = 22f;
    [Tooltip("Si al impactar Grace está más lejos que esto, sobrevive.")]
    public float anchoImpacto = 1.6f;
    [Tooltip("Y mundial por debajo del cual el container sale del frame y se apaga.")]
    public float yFueraDePantalla = -14f;
    public LayerMask capaPiso;

    bool activado;
    Grace graceImpactada;
    float xZonaImpacto;
    Vector3 posicionTirantesInicial;
    Quaternion rotacionTirantesInicial;

    void Awake()
    {
        if (capaPiso.value == 0)
            capaPiso = 1 << 6; // misma capa de suelo que Grace

        if (containerAnimator != null)
        {
            posicionTirantesInicial = containerAnimator.transform.position;
            rotacionTirantesInicial = containerAnimator.transform.rotation;
            containerAnimator.enabled = true;
            containerAnimator.Play(estadoNormal, 0, 0f);
        }

        if (containerTransform != null)
        {
            Animator animExtra = containerTransform.GetComponent<Animator>();
            if (animExtra != null)
                animExtra.enabled = false;

            containerTransform.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Mientras corre el clip de cables, el pivot se queda fijo (como antes).
        if (activado && containerAnimator != null && containerAnimator.enabled)
        {
            containerAnimator.transform.position = posicionTirantesInicial;
            containerAnimator.transform.rotation = rotacionTirantesInicial;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activado || other.GetComponent<Grace>() == null || containerAnimator == null)
            return;

        activado = true;
        graceImpactada = other.GetComponent<Grace>();
        xZonaImpacto = transform.position.x;

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.enabled = false;

        containerAnimator.enabled = true;
        containerAnimator.Play(estadoCaida, 0, 0f);
        Invoke(nameof(Impactar), tiempoHastaImpacto);
    }

    void Impactar()
    {
        // El spritesheet termina a media altura: lo escondemos y bajamos el suelto al piso.
        if (containerAnimator != null)
        {
            SpriteRenderer srAnim = containerAnimator.GetComponent<SpriteRenderer>();
            if (srAnim != null)
                srAnim.enabled = false;

            containerAnimator.enabled = false;
        }

        if (containerTransform != null)
            StartCoroutine(CaerHastaElPiso());

        bool mortal = graceImpactada != null &&
            Mathf.Abs(graceImpactada.transform.position.x - xZonaImpacto) <= anchoImpacto;

        if (!mortal)
            return;

        graceImpactada.MorirPorContainer();
        Invoke(nameof(ReiniciarEscena), Grace.DuracionAnimacionMuerte);
    }

    IEnumerator CaerHastaElPiso()
    {
        containerTransform.SetParent(null, true);
        containerTransform.gameObject.SetActive(true);

        Vector3 inicio = new Vector3(
            xZonaImpacto,
            posicionTirantesInicial.y - 1.5f,
            0f
        );
        // Sigue de largo: atraviesa el muelle y sale por abajo del frame.
        Vector3 destino = new Vector3(xZonaImpacto, yFueraDePantalla, 0f);

        containerTransform.SetPositionAndRotation(inicio, Quaternion.Euler(0f, 0f, -18f));

        while ((containerTransform.position - destino).sqrMagnitude > 0.0001f)
        {
            containerTransform.position = Vector3.MoveTowards(
                containerTransform.position,
                destino,
                velocidadCaidaContainer * Time.deltaTime
            );
            yield return null;
        }

        containerTransform.position = destino;
        containerTransform.gameObject.SetActive(false);
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
