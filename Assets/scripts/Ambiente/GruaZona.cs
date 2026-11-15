using System.Collections;
using UnityEngine;

/// <summary>
/// La plataforma espera en el último muelle de la parte 1.
/// Del medio de la plataforma hasta el final, Grace queda quieta donde aterrizó
/// y el brazo gira por arriba.
/// y la deja en el muelle del galpón.
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

    void Awake()
    {
        angulo = anguloInicial;
        if (plataforma != null)
            piso = plataforma.GetComponent<BoxCollider2D>();
        if (Application.isPlaying)
            Aplicar();
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
