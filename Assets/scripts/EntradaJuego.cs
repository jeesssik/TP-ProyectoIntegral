using UnityEngine;

public static class EntradaJuego
{
    public const string EjeHorizontal = "Horizontal";
    const float umbralHorizontal = 0.1f;

    public static float Horizontal => Input.GetAxisRaw(EjeHorizontal);

    public static bool HayMovimientoHorizontal => Mathf.Abs(Horizontal) > umbralHorizontal;

    public static bool Corre =>
        Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    public static bool Salto => Input.GetKeyDown(KeyCode.Space);
}
