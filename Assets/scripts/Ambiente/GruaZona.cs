using System.Collections;
using UnityEngine;

/// <summary>
/// La plataforma espera en el último muelle de la parte 1.
/// Del medio de la plataforma hasta el final, Grace queda quieta donde aterrizó
/// y el brazo gira por arriba hasta dejarla en el muelle del galpón.
/// El eje está en el círculo del pilote. La plataforma cuelga de la punta.
/// </summary>
[ExecuteAlways]
public class GruaZona : MonoBehaviour
{
    public Transform eje;
    public Transform nivelador;
    public Transform plataforma;

    public float anguloInicial = 0f;
    public float anguloFinal = -180f;
    public float duracionGiro = 4.6f;
    public float zonaCentro = 0.85f;

    float angulo;
    bool ocupada;
    BoxCollider2D piso;

    public static bool ZonaAlcanzada { get; private set; }
    public static Vector2 Reaparicion { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimpiarCheckpoint()
    {
        ZonaAlcanzada = false;
    }

    public static void OlvidarCheckpoint()
    {
        ZonaAlcanzada = false;
    }

    void Awake()
    {
        angulo = ZonaAlcanzada ? anguloFinal : anguloInicial;
        ocupada = ZonaAlcanzada;
        if (plataforma != null)
            piso = plataforma.GetComponent<BoxCollider2D>();
        if (Application.isPlaying)
        {
            Aplicar();
            if (ZonaAlcanzada)
                Physics2D.SyncTransforms();
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;
        if (ocupada || eje == null || plataforma == null || piso == null)
            return;

        Grace grace = FindObjectOfType<Grace>();
        if (grace == null || !grace.ListaParaLaGrua)
            return;

        Collider2D cuerpo = grace.ColisionCuerpo;
        if (cuerpo == null)
            return;

        Bounds pisoB = piso.bounds;
        float x = cuerpo.bounds.center.x;
        float desde = pisoB.center.x - zonaCentro;
        if (x < desde || x > pisoB.max.x + 0.15f)
            return;

        float pie = cuerpo.bounds.min.y;
        if (pie > pisoB.max.y + 0.55f || pie < pisoB.max.y - 0.6f)
            return;

        ocupada = true;
        Rigidbody2D cuerpoRb = grace.CuerpoRigido;
        if (cuerpoRb != null)
            cuerpoRb.velocity = Vector2.zero;
        StartCoroutine(Subir(grace));
    }

    void LateUpdate()
    {
        if (Application.isPlaying)
            Aplicar();
        else
            EnderezarPlataforma();
    }

    IEnumerator Subir(Grace grace)
    {
        grace.EngancharEnGrua(plataforma);

        float pisoLocal = piso.offset.y + piso.size.y * 0.5f;
        Transform raiz = grace.CuerpoRigido != null ? grace.CuerpoRigido.transform : grace.transform;
        Vector3 local = raiz.localPosition;
        local.y = pisoLocal - grace.DistanciaPies();
        local.z = 0f;
        grace.AcomodarEnAncla(local);
        float xLocal = local.x;

        float t = 0f;
        while (t < duracionGiro)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionGiro));
            angulo = Mathf.Lerp(anguloInicial, anguloFinal, k);
            yield return null;
        }

        angulo = anguloFinal;

        Vector3 pies = plataforma.TransformPoint(new Vector3(xLocal, pisoLocal, 0f));
        grace.SoltarDeGrua(pies);

        Aplicar();
        Physics2D.SyncTransforms();
        Reaparicion = PuntoEnElDeck();
        ZonaAlcanzada = true;
    }

    Vector2 PuntoEnElDeck()
    {
        Bounds pisoB = piso.bounds;
        float x = pisoB.max.x + 0.25f;
        for (int i = 0; i < 16; i++)
        {
            RaycastHit2D golpe = Physics2D.Raycast(new Vector2(x, pisoB.max.y + 0.6f), Vector2.down, 2.5f);
            if (golpe.collider != null && golpe.collider != piso && !golpe.collider.isTrigger && golpe.normal.y > 0.5f)
                return new Vector2(golpe.point.x + 1.4f, golpe.point.y);

            x += 0.35f;
        }

        return new Vector2(pisoB.max.x + 1.8f, pisoB.max.y);
    }

    void Aplicar()
    {
        if (eje != null)
            eje.localEulerAngles = new Vector3(0f, 0f, angulo);
        EnderezarPlataforma();
    }

    void EnderezarPlataforma()
    {
        if (eje == null || nivelador == null)
            return;

        float z = Application.isPlaying ? angulo : eje.localEulerAngles.z;
        if (Mathf.Abs(Mathf.DeltaAngle(nivelador.localEulerAngles.z, -z)) > 0.1f)
            nivelador.localEulerAngles = new Vector3(0f, 0f, -z);
    }
}
