using UnityEngine;

public static class FlotacionAgua
{
    public static float OffsetY(float amplitud, float velocidad)
    {
        return Mathf.Sin(Time.time * velocidad) * amplitud;
    }

    public static Quaternion Rotacion(Quaternion inicial, float amplitud, float velocidad)
    {
        return inicial * Quaternion.Euler(0f, 0f, OffsetY(amplitud, velocidad));
    }

    public static Vector3 AvanzarEnLoopX(
        Vector3 posicion,
        int direccion,
        float velocidad,
        float limiteIzquierdo,
        float limiteDerecho,
        float yAlEnvolver)
    {
        posicion.x += direccion * velocidad * Time.deltaTime;

        if (direccion > 0 && posicion.x > limiteDerecho)
            return new Vector3(limiteIzquierdo, yAlEnvolver, posicion.z);

        if (direccion < 0 && posicion.x < limiteIzquierdo)
            return new Vector3(limiteDerecho, yAlEnvolver, posicion.z);

        return posicion;
    }
}
