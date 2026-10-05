using UnityEngine;
using UnityEngine.SceneManagement;

public class ContainerCaeTrigger : MonoBehaviour
{
    public Animator containerAnimator;
    public string estadoNormal = "ContainerMeciendose";
    public string estadoCaida = "ContainerCae";
    public float tiempoHastaImpacto = 0.7f;

    bool activado;

    void Awake()
    {
        if (containerAnimator != null)
        {
            containerAnimator.enabled = true;
            containerAnimator.Play(estadoNormal, 0, 0f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activado || other.GetComponent<Grace>() == null || containerAnimator == null)
            return;

        activado = true;
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.enabled = false;

        containerAnimator.enabled = true;
        containerAnimator.Play(estadoCaida, 0, 0f);
        Invoke(nameof(Impactar), tiempoHastaImpacto);
    }

    void Impactar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
