using UnityEngine;

public static class EntradaJuego
{
    public const string EjeHorizontal = "Horizontal";
    const float umbralHorizontal = 0.1f;
    public const float DuracionConservarDireccion = 0.5f;

    /// <summary>Si está activo, izquierda/derecha se invierten (láser → checkpoint).</summary>
    public static bool InvertidoHorizontal;

    static bool hayDireccionMundoFija;
    static float direccionMundoFija;
    static float fijoHasta;
    static bool frenoPendiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEstaticos()
    {
        InvertidoHorizontal = false;
        hayDireccionMundoFija = false;
        direccionMundoFija = 0f;
        fijoHasta = 0f;
        frenoPendiente = false;
    }

    public static float HorizontalCrudo => Input.GetAxisRaw(EjeHorizontal);

    public static float Horizontal
    {
        get
        {
            if (hayDireccionMundoFija)
                return direccionMundoFija;
            if (frenoPendiente)
                return 0f;
            return HorizontalCrudo * (InvertidoHorizontal ? -1f : 1f);
        }
    }

    public static bool HayMovimientoHorizontal => Mathf.Abs(Horizontal) > umbralHorizontal;

    public static bool HayInputHorizontalCrudo => Mathf.Abs(HorizontalCrudo) > umbralHorizontal;

    /// <summary>
    /// Conserva la dirección mundo medio segundo; después Grace frena y usa el mapeo nuevo.
    /// </summary>
    public static void ConservarDireccionMundo(float direccionMundo, float duracion = DuracionConservarDireccion)
    {
        if (Mathf.Abs(direccionMundo) <= umbralHorizontal)
            return;

        hayDireccionMundoFija = true;
        direccionMundoFija = Mathf.Sign(direccionMundo);
        fijoHasta = Time.time + duracion;
        frenoPendiente = false;
    }

    public static void ActualizarDireccionFija()
    {
        if (!hayDireccionMundoFija)
            return;

        if (Time.time < fijoHasta)
            return;

        hayDireccionMundoFija = false;
        frenoPendiente = true;
    }

    public static bool ConsumirFrenoPorCambioDeZona()
    {
        if (!frenoPendiente)
            return false;

        frenoPendiente = false;
        return true;
    }

    public static void SoltarDireccionFija()
    {
        hayDireccionMundoFija = false;
        frenoPendiente = false;
    }

    public static bool Corre =>
        Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    public static bool Salto => Input.GetKeyDown(KeyCode.Space);
}
