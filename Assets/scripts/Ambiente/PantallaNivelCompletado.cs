using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla el Canvas-Zona1Superada de la escena (editable como Canvas-Pausa).
/// El script vive en el mismo canvas; no se auto-desactiva en Awake.
/// </summary>
public class PantallaNivelCompletado : MonoBehaviour
{
    public static PantallaNivelCompletado Instancia { get; private set; }

    [Header("Vista en escena")]
    [Tooltip("Si está vacío, se usa este mismo GameObject.")]
    [SerializeField] GameObject canvasVictoria;
    [SerializeField] string nombreCanvas = "Canvas-Zona1Superada";

    [Header("Botones")]
    [SerializeField] Button botonReiniciar;
    [SerializeField] Button botonMenu;

    [Header("Escenas")]
    [SerializeField] string escenaMenu = "Menu";

    bool visible;
    bool botonesCableados;
    bool yaInicializado;

    void Awake()
    {
        Instancia = this;
        Inicializar();
    }

    void OnEnable()
    {
        // Por si el GO arrancó inactivo: Awake corre al activarse.
        Instancia = this;
        Inicializar();
    }

    void OnDestroy()
    {
        if (Instancia == this)
            Instancia = null;
    }

    void Inicializar()
    {
        if (yaInicializado)
            return;

        ResolverCanvas();
        CablearBotones();
        yaInicializado = true;
    }

    public static void Mostrar()
    {
        if (Instancia == null)
            Instancia = FindObjectOfType<PantallaNivelCompletado>(true);

        if (Instancia == null)
        {
            Debug.LogWarning("PantallaNivelCompletado: no hay Canvas-Zona1Superada en la escena.");
            return;
        }

        Instancia.MostrarVista();
    }

    public void MostrarVista()
    {
        ResolverCanvas();

        GameObject vista = canvasVictoria != null ? canvasVictoria : gameObject;
        visible = true;
        vista.SetActive(true);

        // Cablear después de activar (Awake/OnEnable ya corrieron).
        CablearBotones();
        Time.timeScale = 0f;
    }

    public void OcultarVista()
    {
        visible = false;
        GameObject vista = canvasVictoria != null ? canvasVictoria : gameObject;
        if (vista != null)
            vista.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        visible = false;
        SceneManager.LoadScene(escenaMenu);
    }

    void ResolverCanvas()
    {
        if (canvasVictoria == null)
            canvasVictoria = gameObject;
    }

    void CablearBotones()
    {
        if (botonesCableados)
            return;

        ResolverCanvas();

        if (botonReiniciar == null)
            botonReiniciar = BuscarBoton("Button-Reiniciar", "Reiniciar", "Button (4)");
        if (botonMenu == null)
            botonMenu = BuscarBoton("Button-Menu", "Button-Menú", "Menú principal", "Button (2)");

        if (botonReiniciar != null)
        {
            botonReiniciar.onClick.RemoveListener(Reiniciar);
            botonReiniciar.onClick.AddListener(Reiniciar);
        }

        if (botonMenu != null)
        {
            botonMenu.onClick.RemoveListener(IrAlMenu);
            botonMenu.onClick.AddListener(IrAlMenu);
        }

        botonesCableados = true;
    }

    Button BuscarBoton(params string[] nombres)
    {
        GameObject raiz = canvasVictoria != null ? canvasVictoria : gameObject;
        Button[] botones = raiz.GetComponentsInChildren<Button>(true);
        for (int n = 0; n < nombres.Length; n++)
        {
            for (int i = 0; i < botones.Length; i++)
            {
                if (botones[i] != null && botones[i].gameObject.name == nombres[n])
                    return botones[i];
            }
        }
        return null;
    }
}
