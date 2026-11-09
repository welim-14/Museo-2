using System.Collections.Generic;
using UnityEngine;

// Textos de sala (cartela de entrada) de cada sección del museo.
// Cada CartelaSala busca aquí la entrada cuyo "sala" coincide con el nombre de su sección.
[CreateAssetMenu(fileName = "TextosSalas", menuName = "Museo/Textos de sala")]
public class TextosSalas : ScriptableObject
{
    [System.Serializable]
    public class Dato
    {
        public string etiqueta;
        public string valor;
    }

    [System.Serializable]
    public class Sala
    {
        [Tooltip("Nombre exacto del objeto de la sección en la escena (hijo de \"Secciones\").")]
        public string sala;
        public string titulo;
        public string tituloOriginal;
        public List<Dato> ficha = new List<Dato>();
        [TextArea(4, 10)]
        public string texto;
    }

    public List<Sala> salas = new List<Sala>
    {
        new Sala
        {
            sala = "Bleach",
            titulo = "Bleach",
            tituloOriginal = "Burīchi",
            ficha = new List<Dato>
            {
                new Dato { etiqueta = "Autor", valor = "Tite Kubo" },
                new Dato { etiqueta = "Manga", valor = "Weekly Shōnen Jump, 2001-2016 · 74 volúmenes" },
                new Dato { etiqueta = "Anime", valor = "Studio Pierrot, 2004-2012 · 366 episodios" },
            },
            texto = "Ichigo Kurosaki, un estudiante capaz de ver espíritus, recibe los poderes de la shinigami Rukia Kuchiki " +
                    "y asume su labor: proteger a los vivos de los hollows y guiar a las almas hacia la Sociedad de Almas. " +
                    "Junto a One Piece y Naruto formó los llamados «tres grandes» de la Shōnen Jump de los años 2000. Su estilo gráfico, " +
                    "sus espadas zanpakutō y las liberaciones bankai lo convirtieron en un referente del género. En 2022 regresó " +
                    "al anime con la adaptación del arco final, La guerra sangrienta de los mil años.",
        },
        new Sala
        {
            sala = "Death Note",
            titulo = "Death Note",
            tituloOriginal = "Desu Nōto",
            ficha = new List<Dato>
            {
                new Dato { etiqueta = "Guion", valor = "Tsugumi Ohba" },
                new Dato { etiqueta = "Dibujo", valor = "Takeshi Obata" },
                new Dato { etiqueta = "Manga", valor = "Weekly Shōnen Jump, 2003-2006 · 12 volúmenes" },
                new Dato { etiqueta = "Anime", valor = "Madhouse, 2006-2007 · 37 episodios" },
            },
            texto = "Light Yagami, un estudiante brillante, encuentra un cuaderno caído del mundo de los shinigami: " +
                    "quien tenga su nombre escrito en él, muere. Decidido a erradicar el crimen bajo el nombre de Kira, " +
                    "se enfrenta al enigmático detective L en un duelo de inteligencias. Thriller psicológico antes que " +
                    "relato de acción, plantea preguntas sobre la justicia, el poder y la moral que lo han convertido en " +
                    "una de las obras más recomendadas como puerta de entrada al anime.",
        },
        new Sala
        {
            sala = "JoJo's Bizarre Adventure",
            titulo = "JoJo's Bizarre Adventure",
            tituloOriginal = "JoJo no Kimyō na Bōken",
            ficha = new List<Dato>
            {
                new Dato { etiqueta = "Autor", valor = "Hirohiko Araki" },
                new Dato { etiqueta = "Manga", valor = "Desde 1987 · Weekly Shōnen Jump y Ultra Jump" },
                new Dato { etiqueta = "Anime", valor = "David Production, desde 2012" },
            },
            texto = "Una saga que sigue a varias generaciones de la familia Joestar, cada parte protagonizada por un «JoJo» " +
                    "distinto en una época y un lugar diferentes. A partir de la tercera parte, Stardust Crusaders, introdujo " +
                    "los Stands: manifestaciones del espíritu con habilidades únicas que redefinieron los combates del shōnen. " +
                    "Sus poses, su estética cercana a la moda y sus referencias a la música occidental la han convertido en " +
                    "una de las obras más influyentes y citadas del manga.",
        },
        new Sala
        {
            sala = "Evangelion",
            titulo = "Neon Genesis Evangelion",
            tituloOriginal = "Shin Seiki Evangerion",
            ficha = new List<Dato>
            {
                new Dato { etiqueta = "Creador", valor = "Hideaki Anno" },
                new Dato { etiqueta = "Anime", valor = "Gainax y Tatsunoko, 1995-1996 · 26 episodios" },
                new Dato { etiqueta = "Películas", valor = "The End of Evangelion (1997) · Rebuild (2007-2021)" },
            },
            texto = "En Tokio-3, quince años después de una catástrofe global, el adolescente Shinji Ikari es reclutado por " +
                    "la organización NERV para pilotar la Eva-01 contra unos seres llamados Ángeles. Bajo la apariencia de " +
                    "una serie de robots gigantes, Evangelion explora la depresión, la soledad y la dificultad de relacionarse " +
                    "con los demás. Su deconstrucción del género mecha y su simbolismo marcaron a toda una generación de " +
                    "creadores y siguen generando debate décadas después.",
        },
        new Sala
        {
            sala = "Ranma ½",
            titulo = "Ranma ½",
            tituloOriginal = "Ranma Nibun-no-Ichi",
            ficha = new List<Dato>
            {
                new Dato { etiqueta = "Autora", valor = "Rumiko Takahashi" },
                new Dato { etiqueta = "Manga", valor = "Weekly Shōnen Sunday, 1987-1996 · 38 volúmenes" },
                new Dato { etiqueta = "Anime", valor = "Studio Deen, 1989-1992 · MAPPA, 2024" },
            },
            texto = "Durante un entrenamiento en China, el joven artista marcial Ranma Saotome cae en un manantial maldito de " +
                    "Jusenkyō: desde entonces se convierte en chica al mojarse con agua fría y recupera su forma con agua " +
                    "caliente. Comprometido a la fuerza con Akane Tendō, su vida se llena de rivales, pretendientes y combates " +
                    "disparatados. Comedia romántica y de artes marciales, fue especialmente popular en Latinoamérica gracias " +
                    "a su doblaje, y en 2024 recibió una nueva adaptación animada.",
        },
    };

    public Sala Buscar(string nombre)
    {
        foreach (var s in salas)
            if (s.sala == nombre) return s;
        return null;
    }

    // Avisar a las cartelas abiertas en el editor para que se actualicen al editar los textos
    public static event System.Action AlCambiar;
    void OnValidate() { AlCambiar?.Invoke(); }
}
