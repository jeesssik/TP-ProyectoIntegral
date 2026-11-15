using UnityEngine;

public class LanchaPatrulla : MonoBehaviour
{
    [Header("Patrulla horizontal")]
    public float limiteIzquierdo = -5f;
    public float limiteDerecho = 5f;
    public float velocidadHorizontal = 1.5f;

    [Header("Movimiento con el agua")]
    public float amplitudVertical = 0.08f;
    public float velocidadVertical = 1.8f;

    [Header("Inclinación")]
    public float amplitudRotacion = 2f;
    public float velocidadRotacion = 1.3f;

    public int direccion = 1;

    [Header("Paso de fondo")]
    [Tooltip("Motor lejano. Solo suena mientras la lancha cruza la pantalla.")]
    public AudioClip sonidoPaso;
    [Range(0f, 1f)] public float volumenPaso = 0.03f;

    const float SegundosDeFade = 1.15f;

    float yInicial;
    Quaternion rotacionInicial;
    SpriteRenderer spriteRenderer;
    AudioSource motor;
    float volumenSuave;

    void Start()
    {
        yInicial = transform.position.y;
        rotacionInicial = transform.rotation;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        MoverHorizontalmente();
        Flotar();
        ActualizarSonidoPaso();
    }

    void OnDisable()
    {
        if (motor != null)
            motor.Stop();
        volumenSuave = 0f;
    }

    void MoverHorizontalmente()
    {
        transform.position += Vector3.right * direccion * velocidadHorizontal * Time.deltaTime;

        if (transform.position.x >= limiteDerecho)
        {
            direccion = -1;
            GirarLancha();
        }
        else if (transform.position.x <= limiteIzquierdo)
        {
            direccion = 1;
            GirarLancha();
        }
    }

    void Flotar()
    {
        Vector3 posicion = transform.position;
        posicion.y = yInicial + FlotacionAgua.OffsetY(amplitudVertical, velocidadVertical);
        transform.position = posicion;
        transform.rotation = FlotacionAgua.Rotacion(rotacionInicial, amplitudRotacion, velocidadRotacion);
    }

    void ActualizarSonidoPaso()
    {
        if (sonidoPaso == null)
            return;

        if (motor == null)
        {
            motor = gameObject.AddComponent<AudioSource>();
            motor.playOnAwake = false;
            motor.loop = true;
            motor.spatialBlend = 0f;
            motor.clip = sonidoPaso;
        }

        float techo = TechoVolumen();
        float objetivo = techo * PresenciaEnPantalla();
        float referencia = Mathf.Max(techo, volumenPaso);
        float paso = referencia * Time.unscaledDeltaTime / SegundosDeFade;
        volumenSuave = Mathf.MoveTowards(volumenSuave, objetivo, Mathf.Max(0.00005f, paso));
        motor.volume = volumenSuave;

        if (volumenSuave > 0.0004f)
        {
            if (!motor.isPlaying)
                motor.Play();
        }
        else if (motor.isPlaying)
            motor.Stop();
    }

    float TechoVolumen()
    {
        if (Time.timeScale <= 0f || AudioManager.Instance == null || AudioManager.Instance.GetSFXMuted())
            return 0f;

        return AudioManager.Instance.GetSFXVolume() * volumenPaso;
    }

    float PresenciaEnPantalla()
    {
        Camera camara = Camera.main;
        if (camara == null)
            return 0f;

        float mitad = camara.orthographic
            ? camara.orthographicSize * camara.aspect
            : 12f;
        float distancia = Mathf.Abs(transform.position.x - camara.transform.position.x);
        float borde = mitad + 2.5f;
        float nucleo = mitad * 0.35f;
        float t = Mathf.InverseLerp(borde, nucleo, distancia);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    void GirarLancha()
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = direccion == -1;
    }
}
