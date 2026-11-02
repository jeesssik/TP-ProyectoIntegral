using UnityEngine;
using UnityEngine.SceneManagement;

// Las losas del muelle del galpón son cajas separadas a la misma altura.
// La costura vertical de cada caja frena el pie. Una sola pieza no tiene esas juntas.
public static class UnirPisoMuelle
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Unir()
    {
        if (SceneManager.GetActiveScene().name != "Level-1")
            return;

        BoxCollider2D[] cajas = Object.FindObjectsOfType<BoxCollider2D>();
        int cuenta = 0;
        for (int i = 0; i < cajas.Length; i++)
        {
            if (EsLosaMuelle(cajas[i]))
                cuenta++;
        }

        if (cuenta == 0)
            return;

        var raiz = new GameObject("PisoMuelleUnido");
        raiz.layer = 6;
        Rigidbody2D cuerpo = raiz.AddComponent<Rigidbody2D>();
        cuerpo.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D compuesto = raiz.AddComponent<CompositeCollider2D>();
        compuesto.geometryType = CompositeCollider2D.GeometryType.Polygons;
        compuesto.generationType = CompositeCollider2D.GenerationType.Synchronous;

        for (int i = 0; i < cajas.Length; i++)
        {
            BoxCollider2D caja = cajas[i];
            if (!EsLosaMuelle(caja))
                continue;

            caja.usedByComposite = true;
            caja.transform.SetParent(raiz.transform, true);
        }

        compuesto.GenerateGeometry();
    }

    static bool EsLosaMuelle(BoxCollider2D caja)
    {
        return caja != null && !caja.isTrigger && caja.gameObject.name.StartsWith("muelle_galpon");
    }
}
