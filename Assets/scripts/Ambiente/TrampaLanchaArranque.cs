using UnityEngine;

public class TrampaLanchaArranque : MonoBehaviour
{
    [Header("Velocidades")]
    public float velocidadInicio = 2f;
    public float velocidadAcelerado = 18f;
    public float aceleracion = 25f;
    public float tiempoHastaAceleron = 0.4f;

    [Header("Inclinación del Morro")]
    public float anguloElevacionMaximo = 15f;
    public float velocidadInclinacion = 4f;

    [Header("Referencias")]
    public LanchaFlotando scriptFlotacion;
    public OlaSalpicon salpicon;

    [Header("Sonido")]
    public AudioClip sonidoArranque;
    [Range(0f, 1f)] public float volumenArranque = 0.5f;

    const float VidaTrasAceleron = 4f;

    float velocidadActual;
    float anguloActualZ;
    float tiempoArranque;
    bool trampaIniciada;
    bool aceleracionViolenta;
    AudioSource motor;

    void Update()
    {
        if (!trampaIniciada)
            return;

        if (!aceleracionViolenta)
        {
            velocidadActual = velocidadInicio;

            if (Time.time >= tiempoArranque + tiempoHastaAceleron)
                DispararAceleron();
        }
        else
        {
            velocidadActual = Mathf.MoveTowards(velocidadActual, velocidadAcelerado, aceleracion * Time.deltaTime);

            float porcentajeVelocidad = velocidadActual / velocidadAcelerado;
            float anguloObjetivo = porcentajeVelocidad * anguloElevacionMaximo;
            anguloActualZ = Mathf.Lerp(anguloActualZ, anguloObjetivo, Time.deltaTime * velocidadInclinacion);
            transform.localRotation = Quaternion.Euler(0f, 0f, anguloActualZ);
        }

        transform.Translate(Vector3.right * velocidadActual * Time.deltaTime, Space.World);
        ActualizarMotor();
    }

    public void ActivarDesdeTrigger()
    {
        if (trampaIniciada)
            return;

        trampaIniciada = true;
        tiempoArranque = Time.time;

        if (scriptFlotacion != null)
            scriptFlotacion.enabled = false;

        EncenderMotor();
    }

    void EncenderMotor()
    {
        if (sonidoArranque == null || AudioManager.Instance == null || AudioManager.Instance.GetSFXMuted())
            return;

        GameObject nodo = new GameObject("MotorLanchaArranque");
        if (AudioManager.Instance != null)
            nodo.transform.SetParent(AudioManager.Instance.transform, false);
        motor = nodo.AddComponent<AudioSource>();
        motor.playOnAwake = false;
        motor.loop = false;
        motor.spatialBlend = 0f;
        motor.pitch = 0.96f;
        motor.volume = 0f;
        motor.clip = sonidoArranque;
        motor.Play();
    }

    void ActualizarMotor()
    {
        if (motor == null)
            return;

        if (Time.timeScale <= 0f)
        {
            if (motor.isPlaying)
                motor.Pause();
            return;
        }

        if (!motor.isPlaying)
            motor.UnPause();

        float techo = AudioManager.Instance == null || AudioManager.Instance.GetSFXMuted()
            ? 0f
            : AudioManager.Instance.GetSFXVolume() * volumenArranque;
        float empuje = Mathf.Clamp01(velocidadActual / Mathf.Max(0.01f, velocidadAcelerado));
        float cerca = CercaniaEnEscena();
        motor.pitch = Mathf.Lerp(0.9f, Mathf.Lerp(0.96f, 1.08f, empuje), cerca);
        motor.volume = techo * cerca;

        if (cerca <= 0.001f && motor.isPlaying)
            motor.Stop();
    }

    float CercaniaEnEscena()
    {
        Camera camara = Camera.main;
        if (camara == null)
            return 1f;

        float mitad = camara.orthographic
            ? camara.orthographicSize * camara.aspect
            : 12f;
        float adelante = transform.position.x - camara.transform.position.x;
        float empiezaALejos = mitad * 0.2f;
        float desaparece = mitad + 16f;
        if (adelante <= empiezaALejos)
            return 1f;

        float t = Mathf.InverseLerp(empiezaALejos, desaparece, adelante);
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }

    void OnDestroy()
    {
        if (motor == null)
            return;

        if (motor.volume <= 0.001f)
        {
            Destroy(motor.gameObject);
            motor = null;
            return;
        }

        FadeMotorAlSalir cola = motor.gameObject.AddComponent<FadeMotorAlSalir>();
        cola.Seguir(motor);
        motor = null;
    }

    void DispararAceleron()
    {
        aceleracionViolenta = true;

        if (salpicon != null)
            salpicon.Reproducir();

        Destroy(gameObject, VidaTrasAceleron);
    }
}

public class FadeMotorAlSalir : MonoBehaviour
{
    AudioSource motor;
    float desde;

    public void Seguir(AudioSource fuente)
    {
        motor = fuente;
        desde = fuente != null ? fuente.volume : 0f;
    }

    void Update()
    {
        if (motor == null)
        {
            Destroy(gameObject);
            return;
        }

        desde = Mathf.MoveTowards(desde, 0f, desde * Time.unscaledDeltaTime / 0.9f + 0.002f);
        motor.volume = desde;
        if (desde > 0.001f)
            return;

        motor.Stop();
        Destroy(gameObject);
    }
}
