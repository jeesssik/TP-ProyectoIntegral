using System;
using UnityEngine;
using UnityEngine.Events;

public class OlaSobreMuelle : MonoBehaviour
{
    [Header("Dos SpriteRenderer en la misma posición local")]
    [SerializeField] SpriteRenderer olaDetras;
    [SerializeField] SpriteRenderer olaDelante;
    [SerializeField] bool reproducirAlIniciar = true;

    [Header("Eventos: ajustar el empujón a la posición real de Grace")]
    [Range(1, 43)] public int fotogramaEmpuje = 23;
    [Range(1, 43)] public int fotogramaImpactoMar = 29;
    public UnityEvent alAlcanzarGrace;
    public UnityEvent alImpactarMar;

    const int Cuadros = 43;
    const float Fps = 24f;

    Sprite[] detras;
    Sprite[] delante;
    float tiempo;
    int cuadro;
    bool reproduciendo;

    void Awake()
    {
        detras = CargarCapa("Ola/detras");
        delante = CargarCapa("Ola/delante");

        if (detras.Length != Cuadros || delante.Length != Cuadros ||
            olaDetras == null || olaDelante == null)
        {
            Debug.LogError("Ola: faltan PNG o referencias a SpriteRenderer.", this);
            enabled = false;
            return;
        }

        olaDetras.sprite = null;
        olaDelante.sprite = null;
    }

    void Start()
    {
        if (reproducirAlIniciar)
            Reproducir();
    }

    public void Reproducir()
    {
        if (!enabled || reproduciendo)
            return;

        tiempo = 0f;
        cuadro = 0;
        reproduciendo = true;
        MostrarCuadro(0);
    }

    void Update()
    {
        if (!reproduciendo)
            return;

        tiempo += Time.deltaTime;
        int destino = Mathf.Min(Cuadros - 1, Mathf.FloorToInt(tiempo * Fps));

        while (cuadro < destino)
        {
            cuadro++;
            MostrarCuadro(cuadro);
            if (cuadro == fotogramaEmpuje - 1)
                alAlcanzarGrace.Invoke();
            if (cuadro == fotogramaImpactoMar - 1)
                alImpactarMar.Invoke();
        }

        if (tiempo < Cuadros / Fps)
            return;

        reproduciendo = false;
        olaDetras.sprite = null;
        olaDelante.sprite = null;
    }

    void MostrarCuadro(int indice)
    {
        olaDetras.sprite = detras[indice];
        olaDelante.sprite = delante[indice];
    }

    static Sprite[] CargarCapa(string ruta)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(ruta);
        Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites;
    }
}
