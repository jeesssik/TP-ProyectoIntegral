using UnityEngine;

[ExecuteAlways]
public class AguaGradienteColor : MonoBehaviour
{
    [SerializeField] Color colorInicial = Color.white;
    [SerializeField] Color colorFinal = new Color(0.34f, 0.42f, 0.58f, 1f);
    [SerializeField] float xInicioMundo = 90f;
    [SerializeField] float xFinMundo = 210f;
    [SerializeField] Shader shaderGradiente;

    SpriteRenderer[] renderersAgua;
    Material materialGradiente;

    void OnEnable()
    {
        PrepararMaterial();
        ActualizarColores();
    }

    void OnValidate()
    {
        PrepararMaterial();
        ActualizarColores();
    }

    void ActualizarColores()
    {
        if (xFinMundo <= xInicioMundo)
        {
            Debug.LogError("El final del gradiente del agua debe ser mayor que el inicio.", this);
            return;
        }

        if (renderersAgua == null)
            renderersAgua = ObtenerRenderersAgua();

        foreach (SpriteRenderer rendererAgua in renderersAgua)
        {
            if (rendererAgua == null)
                continue;

            rendererAgua.color = Color.white;
        }
    }

    void PrepararMaterial()
    {
        if (materialGradiente == null)
        {
            Shader shader = shaderGradiente != null
                ? shaderGradiente
                : Shader.Find("Custom/AguaGradiente");
            if (shader == null)
            {
                Debug.LogError("No se encontró el shader Custom/AguaGradiente.", this);
                return;
            }

            materialGradiente = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        materialGradiente.SetColor("_ColorInicial", colorInicial);
        materialGradiente.SetColor("_ColorFinal", colorFinal);
        materialGradiente.SetFloat("_XInicio", xInicioMundo);
        materialGradiente.SetFloat("_XFin", xFinMundo);

        if (renderersAgua == null)
            renderersAgua = ObtenerRenderersAgua();

        foreach (SpriteRenderer rendererAgua in renderersAgua)
        {
            if (rendererAgua != null)
                rendererAgua.sharedMaterial = materialGradiente;
        }
    }

    SpriteRenderer[] ObtenerRenderersAgua()
    {
        SpriteRenderer[] todosLosRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        var renderers = new System.Collections.Generic.List<SpriteRenderer>();

        foreach (SpriteRenderer rendererAgua in todosLosRenderers)
        {
            if (rendererAgua.gameObject.name.StartsWith("agua", System.StringComparison.OrdinalIgnoreCase))
                renderers.Add(rendererAgua);
        }

        return renderers.ToArray();
    }
}
