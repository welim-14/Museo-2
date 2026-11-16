using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Indicador de la sala en la que está el visitante ("VESTÍBULO", "BLEACH"...).
// Cada zona ocupa el volumen de todo lo que cuelga de su objeto (paredes, suelo, techo),
// calculado al arrancar en el espacio local de la zona, así que respeta las salas giradas.
// Fuera de todas las zonas (caminando por el exterior) el indicador se oculta,
// o muestra "nombreExterior" si se rellena.
// Si la lista está vacía, se rellena al arrancar con el vestíbulo y los hijos de "Secciones".
// Al cambiar de sala, el nombre se desvanece y vuelve a aparecer.
public class SalaHUD : MonoBehaviour
{
    [System.Serializable]
    public class Zona
    {
        public string nombre;
        public Transform centro;
        [System.NonSerialized] public Bounds limites;
        [System.NonSerialized] public bool tieneLimites;
    }

    [Header("Referencias")]
    public TextMeshProUGUI textoNombre;
    public CanvasGroup grupo;

    [Header("Zonas")]
    public List<Zona> zonas = new List<Zona>();
    public string nombreVestibulo = "Vestíbulo";
    public string objetoVestibulo = "Lobby";
    public string raizSecciones = "Secciones";
    [Tooltip("Texto a mostrar fuera de todas las salas. Vacío = ocultar el indicador.")]
    public string nombreExterior = "";
    [Tooltip("Margen (m) que se añade alrededor de cada sala.")]
    public float margen = 0.5f;

    public float duracionFundido = 0.35f;

    Transform jugador;
    string actual;
    string pendiente;

    void Start()
    {
        if (zonas.Count == 0) RellenarZonas();
        foreach (var z in zonas) CalcularLimites(z);
        if (grupo != null) grupo.alpha = 0f;
        if (zonas.Count == 0) gameObject.SetActive(false);
    }

    void RellenarZonas()
    {
        var vestibulo = GameObject.Find(objetoVestibulo);
        if (vestibulo != null) zonas.Add(new Zona { nombre = nombreVestibulo, centro = vestibulo.transform });

        var secciones = GameObject.Find(raizSecciones);
        if (secciones != null)
            foreach (Transform hijo in secciones.transform)
                zonas.Add(new Zona { nombre = hijo.name, centro = hijo });
    }

    void LateUpdate()
    {
        if (jugador == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go == null) return;
            jugador = go.transform;
        }

        string zona = ZonaActual() ?? nombreExterior;
        if (zona != actual && zona != pendiente) pendiente = zona;
        else if (zona == actual) pendiente = null;

        if (grupo == null)
        {
            if (pendiente != null) Poner(pendiente);
            return;
        }

        float paso = duracionFundido > 0f ? Time.unscaledDeltaTime / duracionFundido : 1f;
        if (pendiente != null)
        {
            // Primero se desvanece el nombre anterior; al llegar a 0 se cambia el texto
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, 0f, paso);
            if (grupo.alpha <= 0f) Poner(pendiente);
        }
        else
        {
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, string.IsNullOrEmpty(actual) ? 0f : 1f, paso);
        }
    }

    void Poner(string nombre)
    {
        actual = nombre;
        pendiente = null;
        if (textoNombre != null && !string.IsNullOrEmpty(nombre)) textoNombre.text = nombre.ToUpperInvariant();
    }

    // Caja que envuelve todos los renderers de la zona, en el espacio local de su objeto
    void CalcularLimites(Zona z)
    {
        z.tieneLimites = false;
        if (z.centro == null) return;
        Matrix4x4 aLocal = z.centro.worldToLocalMatrix;
        foreach (var r in z.centro.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            Matrix4x4 m = aLocal * r.localToWorldMatrix;
            Bounds lb = r.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = lb.center + Vector3.Scale(lb.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = m.MultiplyPoint3x4(esquina);
                if (!z.tieneLimites)
                {
                    z.limites = new Bounds(p, Vector3.zero);
                    z.tieneLimites = true;
                }
                else z.limites.Encapsulate(p);
            }
        }
        if (z.tieneLimites)
        {
            Vector3 s = z.centro.lossyScale;
            Vector3 extra = new Vector3(margen / Mathf.Max(0.0001f, Mathf.Abs(s.x)), 2f / Mathf.Max(0.0001f, Mathf.Abs(s.y)),
                                        margen / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
            z.limites.Expand(extra * 2f);
        }
    }

    string ZonaActual()
    {
        string mejor = null;
        float mejorVolumen = float.PositiveInfinity;
        Vector3 p = jugador.position;
        foreach (var z in zonas)
        {
            if (z.centro == null || !z.tieneLimites) continue;
            if (!z.limites.Contains(z.centro.InverseTransformPoint(p))) continue;
            // Si dos zonas se solapan, gana la más pequeña (la más específica)
            Vector3 t = z.limites.size;
            float volumen = t.x * t.y * t.z;
            if (volumen < mejorVolumen)
            {
                mejorVolumen = volumen;
                mejor = z.nombre;
            }
        }
        return mejor;
    }
}
