using UnityEngine;

public class DisparadorOla : MonoBehaviour
{
    public OlaSobreMuelle ola;

    [Tooltip("Si Grace quedó esta distancia más a la izquierda que cuando empezó la ola, esquivó.")]
    public float distanciaEsquivaAtras = 0.6f;

    bool disparada;
    Grace graceDetectada;
    float xCuandoEmpezoLaOla;

    void OnTriggerEnter2D(Collider2D other)
    {
        Grace grace = other.GetComponent<Grace>();
        if (grace == null)
            return;

        graceDetectada = grace;

        if (disparada || ola == null)
            return;

        disparada = true;
        xCuandoEmpezoLaOla = grace.transform.position.x;
        ola.Reproducir();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Grace grace = other.GetComponent<Grace>();
        if (grace != null)
            graceDetectada = grace;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Grace grace = other.GetComponent<Grace>();
        if (grace != null && grace == graceDetectada)
            graceDetectada = null;
    }

    public void IntentarImpacto()
    {
        Grace grace = graceDetectada != null ? graceDetectada : FindObjectOfType<Grace>();
        if (grace == null)
            return;

        if (grace.EstaFueraDelAlcanceDeLaOla(xCuandoEmpezoLaOla, distanciaEsquivaAtras))
            return;

        grace.EmpujadaPorOla();
    }
}
