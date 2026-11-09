using System.Collections.Generic;
using UnityEngine;

// Esfera que reproduce un audio mientras el puntero está encima.
[RequireComponent(typeof(AudioSource))]
public class EsferaAudio : EsferaInteractiva
{
    [Header("Audio")]
    public AudioClip audioClip;
    public string titulo = "";
    [Range(0f, 1f)] public float volumen = 0.8f;

    [Tooltip("Si está activo el audio se desvanece al quitar el puntero; si no, sigue hasta terminar.")]
    public bool detenerAlSalir = true;
    [Tooltip("Al volver a señalar la esfera, sigue donde se quedó en lugar de empezar de nuevo.")]
    public bool continuarDondeQuedo = true;
    public float duracionDesvanecido = 0.6f;

    private AudioSource fuente;
    private bool sonando;
    private bool pausado;

    // Esferas que están sonando, para mostrarlas en el HUD aunque no se las señale
    private static readonly List<EsferaAudio> activas = new List<EsferaAudio>();

    string Nombre => string.IsNullOrEmpty(titulo) ? audioClip.name : titulo;

    public override string TextoAyuda =>
        audioClip == null ? "(sin audio)" : (Suena ? "Sonando: " : "Audio: ") + Nombre;

    bool Suena => fuente != null && fuente.isPlaying && fuente.volume > 0f;

    // Texto del audio que sigue sonando (el último que empezó), o null si no suena ninguno
    public static string TextoSonando()
    {
        for (int i = activas.Count - 1; i >= 0; i--)
            if (activas[i] != null && activas[i].Suena) return "Sonando: " + activas[i].Nombre;
        return null;
    }

    protected override void Awake()
    {
        base.Awake();
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.clip = audioClip;
        fuente.volume = 0f;
    }

    public override void AlEntrarPuntero()
    {
        base.AlEntrarPuntero();
        if (audioClip == null) return;

        if (fuente.clip != audioClip) { fuente.clip = audioClip; pausado = false; }

        if (pausado && continuarDondeQuedo) fuente.UnPause();
        else if (!fuente.isPlaying) { fuente.Stop(); fuente.Play(); }
        pausado = false;
        sonando = true;
        activas.Remove(this);
        activas.Add(this);
    }

    public override void AlSalirPuntero()
    {
        base.AlSalirPuntero();
        if (detenerAlSalir) sonando = false;
    }

    protected override void Update()
    {
        base.Update();
        if (fuente == null) return;

        float objetivo = sonando ? volumen : 0f;
        float paso = duracionDesvanecido > 0f ? volumen / duracionDesvanecido * Time.deltaTime : 1f;
        fuente.volume = Mathf.MoveTowards(fuente.volume, objetivo, paso);

        if (!sonando && fuente.isPlaying && fuente.volume <= 0f) { fuente.Pause(); pausado = true; }
        if (sonando && !fuente.isPlaying) sonando = false; // terminó el clip
        if (!sonando && !fuente.isPlaying) activas.Remove(this);

        // Pulso suave mientras suena
        brilloExtra = fuente.isPlaying && fuente.volume > 0f ? 0.4f + 0.3f * Mathf.Sin(Time.time * 6f) : 0f;
    }

    void OnDisable()
    {
        activas.Remove(this);
    }
}
