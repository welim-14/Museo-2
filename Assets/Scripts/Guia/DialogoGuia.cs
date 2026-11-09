using System.Collections.Generic;
using UnityEngine;

// Textos del guía del museo. Se editan desde el Inspector sin tocar código.
// El orden de "secciones" es el orden en que aparecen en el menú (teclas 1, 2, 3...).
[CreateAssetMenu(fileName = "DialogoGuia", menuName = "Museo/Diálogo del guía")]
public class DialogoGuia : ScriptableObject
{
    [System.Serializable]
    public class Seccion
    {
        [Tooltip("Nombre que se muestra en el menú y en las frases.")]
        public string nombre;
        [Tooltip("Nombre del GameObject del portal en la escena (por ejemplo, \"Portal Bleach\").")]
        public string nombrePortal;
        [TextArea(2, 4)]
        [Tooltip("Lo que dice el guía al llegar frente al portal.")]
        public string lineaLlegada;
    }

    [Header("Identidad")]
    public string nombre = "Hamood";
    public string titulo = "Guía del museo";

    [Header("Conversación")]
    [TextArea(2, 4)]
    public List<string> bienvenida = new List<string>
    {
        "Bienvenido al Museo Virtual. Mi nombre es Hamood y será un placer acompañarle durante su visita.",
        "En este vestíbulo encontrará seis portales; cada uno le llevará a la sala dedicada a una gran obra del anime y del manga.",
        "¿En qué puedo ayudarle?",
    };

    [TextArea(2, 4)]
    public string reencuentro = "Me alegra verle de nuevo. ¿En qué más puedo ayudarle?";

    public string opcionSecciones = "Lléveme a una sección";
    public string opcionMuseo = "Hábleme del museo";
    public string opcionNada = "Nada por ahora, gracias";
    public string opcionVolver = "Volver";

    [TextArea(2, 4)]
    public string elegirSeccion = "Con mucho gusto. ¿Qué sala desea visitar?";

    [TextArea(2, 4)]
    [Tooltip("{0} se sustituye por el nombre de la sección.")]
    public string acompanar = "Excelente elección. Acompáñeme, por favor; la sala de {0} está por aquí.";

    [TextArea(2, 4)]
    public List<string> infoMuseo = new List<string>
    {
        "El Museo Virtual reúne seis obras que han dejado huella en la historia del anime y del manga, cada una con su propia sala.",
        "En las salas encontrará imágenes, videos y música. Basta con señalar una obra para conocer su título, y los videos se reproducen mientras los contempla.",
        "Cada sala cuenta con un portal de regreso a este vestíbulo. Yo estaré aquí por si me necesita.",
        "¿Puedo ayudarle en algo más?",
    };

    [TextArea(2, 4)]
    public string despedida = "Ha sido un placer. Si me necesita, aquí estaré. Que disfrute su visita.";

    [Header("Letrero flotante")]
    public string avisoHablar = "<b>[E]</b>  Hablar";
    public string avisoSigame = "Sígame, por favor";
    public string avisoEspera = "Le espero, sin prisa";

    [Header("Secciones (en el orden de la escena)")]
    public List<Seccion> secciones = new List<Seccion>
    {
        new Seccion
        {
            nombre = "Bleach",
            nombrePortal = "Portal Bleach",
            lineaLlegada = "Hemos llegado a la sala de Bleach, el mundo de los shinigami y la Sociedad de Almas. Adelante, cuando guste.",
        },
        new Seccion
        {
            nombre = "Death Note",
            nombrePortal = "Portal Death Note",
            lineaLlegada = "Esta es la sala de Death Note: un cuaderno, un nombre y un duelo de inteligencias que todavía da que hablar. Adelante, cuando guste.",
        },
        new Seccion
        {
            nombre = "JoJo's Bizarre Adventure",
            nombrePortal = "Portal JoJo's Bizarre Adventure",
            lineaLlegada = "He aquí la sala de JoJo's Bizarre Adventure, una saga de generaciones, poses memorables y Stands. Adelante, cuando guste.",
        },
        new Seccion
        {
            nombre = "Evangelion",
            nombrePortal = "Portal Evangelion",
            lineaLlegada = "Esta es la sala de Evangelion, una obra que transformó para siempre el género de los mechas. Adelante, cuando guste.",
        },
        new Seccion
        {
            nombre = "Ranma ½",
            nombrePortal = "Portal Ranma ½",
            lineaLlegada = "Hemos llegado a la sala de Ranma ½, la comedia de artes marciales de Rumiko Takahashi. Adelante, cuando guste.",
        },
        new Seccion
        {
            nombre = "Zenless Zone Zero",
            nombrePortal = "Portal Zenless Zone Zero",
            lineaLlegada = "Esta es la sala de Zenless Zone Zero, con los personajes de New Eridu. Adelante, cuando guste.",
        },
    };
}
