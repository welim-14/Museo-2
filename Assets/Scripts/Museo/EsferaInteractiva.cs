using UnityEngine;

// Base de las esferas del museo: flotan, giran y brillan cuando el puntero está encima.
// Las subclases deciden qué pasa al entrar el puntero o al hacer clic.
public abstract class EsferaInteractiva : MonoBehaviour, IInteractuable
{
    [Header("Apariencia")]
    [Tooltip("Malla de la esfera (debe tener collider). Si está vacío se usa el primer Renderer hijo.")]
    public Renderer esfera;
    public Color colorBrillo = new Color(1f, 0.75f, 0.3f);
    [Range(0f, 3f)] public float intensidadBrillo = 1.2f;
    public float escalaResaltada = 1.12f;
    public float velocidadGiro = 20f;
    public float amplitudFlotacion = 0.05f;

    protected bool resaltada;
    protected float brilloExtra; // las subclases lo usan para pulsar (p. ej. mientras suena el audio)

    private MaterialPropertyBlock bloque;
    private Vector3 escalaBase;
    private Vector3 posicionBase;
    private float brilloActual;
    private static readonly int idEmision = Shader.PropertyToID("_EmissionColor");

    public abstract string TextoAyuda { get; }

    protected virtual void Awake()
    {
        if (esfera == null) esfera = GetComponentInChildren<Renderer>();
        bloque = new MaterialPropertyBlock();
        escalaBase = esfera.transform.localScale;
        posicionBase = esfera.transform.localPosition;
    }

    protected virtual void Update()
    {
        // flotacion y giro
        Transform t = esfera.transform;
        t.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
        t.localPosition = posicionBase + Vector3.up * Mathf.Sin(Time.time * 1.5f) * amplitudFlotacion;

        // escala
        float escala = resaltada ? escalaResaltada : 1f;
        t.localScale = Vector3.Lerp(t.localScale, escalaBase * escala, Time.deltaTime * 10f);

        // brillo
        float objetivo = (resaltada ? intensidadBrillo : 0f) + brilloExtra;
        brilloActual = Mathf.Lerp(brilloActual, objetivo, Time.deltaTime * 8f);
        esfera.GetPropertyBlock(bloque);
        bloque.SetColor(idEmision, colorBrillo * brilloActual);
        esfera.SetPropertyBlock(bloque);
    }

    public virtual void AlEntrarPuntero() { resaltada = true; }
    public virtual void AlSalirPuntero() { resaltada = false; }
    public virtual void AlHacerClic() { }
}
