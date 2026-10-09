using UnityEngine;

public static class EntradaJuego
{
    public const string EjeHorizontal = "Horizontal";
    const float umbralHorizontal = 0.1f;

    /// <summary>Si está activo, izquierda/derecha se invierten (láser → checkpoint).</summary>
    public static bool InvertidoHorizontal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEstaticos()
    {
        InvertidoHorizontal = false;
    }

    public static float Horizontal =>
        Input.GetAxisRaw(EjeHorizontal) * (InvertidoHorizontal ? -1f : 1f);

    public static bool HayMovimientoHorizontal => Mathf.Abs(Horizontal) > umbralHorizontal;

    public static bool Corre =>
        Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    public static bool Salto => Input.GetKeyDown(KeyCode.Space);
}
