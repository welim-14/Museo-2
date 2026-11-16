using System.Text;
using TMPro;
using UnityEngine;

// Cartela de entrada de una sala: título, título original, ficha técnica y texto de sala.
// Va dentro del prefab SalaSeccion; cada instancia toma sus textos de TextosSalas según el nombre
// de su sección (el objeto hijo de "Secciones"), así que todas las salas comparten la misma cartela.
// Se actualiza también en el editor para ver el resultado sin dar a Play.
[ExecuteAlways]
public class CartelaSala : MonoBehaviour
{
    public TextosSalas textos;
    [Tooltip("Nombre de la sala a mostrar. Vacío = el nombre de la sección que la contiene.")]
    public string sala = "";
    public string raizSecciones = "Secciones";

    [Header("Textos")]
    public TMP_Text titulo;
    public TMP_Text tituloOriginal;
    public TMP_Text ficha;
    public TMP_Text texto;

    public Color colorEtiqueta = new Color(0.83f, 0.66f, 0.30f, 1f);
    [Tooltip("Dónde empieza el valor de cada línea de la ficha (% del ancho).")]
    public float sangriaFicha = 28f;

    void OnEnable()
    {
        TextosSalas.AlCambiar += Aplicar;
        Aplicar();
    }

    void OnDisable()
    {
        TextosSalas.AlCambiar -= Aplicar;
    }

    void OnValidate()
    {
        Aplicar();
    }

    public string NombreSala()
    {
        if (!string.IsNullOrEmpty(sala)) return sala;
        Transform t = transform;
        while (t.parent != null)
        {
            if (t.parent.name == raizSecciones) return t.name;
            t = t.parent;
        }
        return t.name;
    }

    public void Aplicar()
    {
        if (this == null) return;
        var datos = textos != null ? textos.Buscar(NombreSala()) : null;

        if (datos == null)
        {
            // Dentro del prefab (o sin datos) se muestra un texto de ejemplo para maquetar
            Poner(titulo, "Título de la sala");
            Poner(tituloOriginal, "Título original");
            Poner(ficha, Linea("Autor", "Nombre") + "\n" + Linea("Manga", "Años · volúmenes"));
            Poner(texto, "Texto de sala: un párrafo breve que presenta la obra.");
            return;
        }

        Poner(titulo, datos.titulo.ToUpperInvariant());
        Poner(tituloOriginal, datos.tituloOriginal);

        var sb = new StringBuilder();
        foreach (var d in datos.ficha)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(Linea(d.etiqueta, d.valor));
        }
        Poner(ficha, sb.ToString());
        Poner(texto, datos.texto);
    }

    string Linea(string etiqueta, string valor)
    {
        // Sangría colgante: si el valor ocupa dos líneas, la segunda queda alineada con la primera
        return $"<color=#{ColorUtility.ToHtmlStringRGB(colorEtiqueta)}>{etiqueta.ToUpperInvariant()}</color>" +
               $"<indent={sangriaFicha:0.#}%>{valor}</indent>";
    }

    static void Poner(TMP_Text destino, string valor)
    {
        if (destino != null && destino.text != valor) destino.text = valor;
    }
}
