using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Caja de diálogo del guía, independiente del HUD del Player.
// Construye su propio Canvas al arrancar, así que basta con añadir el componente a un objeto vacío.
//
// Dos modos:
//  - Conversación (Mostrar): bloquea la atención del visitante. E / Espacio / Enter completan
//    o avanzan la línea, 1-9 eligen opción, 0 vuelve, Esc cierra.
//  - Aviso (MostrarAviso): una frase que se escribe sola y se oculta tras unos segundos, sin teclas.
public class UIDialogoGuia : MonoBehaviour
{
    [Header("Aspecto")]
    public Color colorFondo = new Color(0.07f, 0.075f, 0.09f, 0.92f);
    public Color colorAcento = new Color(0.85f, 0.71f, 0.38f, 1f);
    public Color colorTexto = new Color(0.95f, 0.94f, 0.91f, 1f);
    public Color colorSecundario = new Color(0.66f, 0.64f, 0.6f, 1f);
    public float anchoCaja = 1100f;
    public float margenInferior = 48f;

    [Header("Ritmo")]
    [Tooltip("Caracteres por segundo del efecto máquina de escribir.")]
    public float velocidadEscritura = 50f;
    public float duracionFundido = 0.18f;

    // Estado de la conversación
    readonly List<string> lineas = new List<string>();
    readonly List<string> opciones = new List<string>();
    string opcionVolver;
    Action<int> alElegir;
    Action alCerrar;
    int indiceLinea;
    float caracteresVisibles;
    bool escribiendo;
    bool modal;
    float finAviso;
    int frameApertura;

    // Elementos construidos
    Canvas canvas;
    CanvasGroup grupo;
    TextMeshProUGUI textoNombre, textoCuerpo, textoPista;
    RectTransform contenedorOpciones;
    readonly List<TextMeshProUGUI> lineasOpcion = new List<TextMeshProUGUI>();
    float alphaObjetivo;

    static readonly Key[] teclasNumero =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
    };
    static readonly Key[] teclasNumpad =
    {
        Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
    };

    public bool Abierta => alphaObjetivo > 0f;
    public bool EnConversacion => Abierta && modal;

    // Para que otros sistemas (el HUD) sepan si hay una conversación en curso
    static readonly List<UIDialogoGuia> instancias = new List<UIDialogoGuia>();
    public static bool HayConversacion
    {
        get
        {
            foreach (var ui in instancias)
                if (ui.EnConversacion) return true;
            return false;
        }
    }

    void OnEnable() { instancias.Add(this); }
    void OnDisable() { instancias.Remove(this); }

    void Awake()
    {
        Construir();
        grupo.alpha = 0f;
        canvas.enabled = false;
    }

    // ---------------------------------------------------------------- API

    public void Mostrar(string nombre, string titulo, IList<string> nuevasLineas, IList<string> nuevasOpciones,
                        string volver, Action<int> elegir, Action cerrar)
    {
        modal = true;
        lineas.Clear();
        lineas.AddRange(nuevasLineas);
        opciones.Clear();
        if (nuevasOpciones != null) opciones.AddRange(nuevasOpciones);
        opcionVolver = volver;
        alElegir = elegir;
        alCerrar = cerrar;
        frameApertura = Time.frameCount;
        PonerNombre(nombre, titulo);
        EmpezarLinea(0);
        Abrir();
    }

    public void MostrarAviso(string nombre, string titulo, string texto, float segundos)
    {
        modal = false;
        lineas.Clear();
        lineas.Add(texto);
        opciones.Clear();
        opcionVolver = null;
        alElegir = null;
        alCerrar = null;
        finAviso = Time.time + segundos;
        PonerNombre(nombre, titulo);
        EmpezarLinea(0);
        Abrir();
    }

    public void Ocultar()
    {
        alphaObjetivo = 0f;
        alElegir = null;
        alCerrar = null;
    }

    // ---------------------------------------------------------------- Ciclo

    void Update()
    {
        // Fundido
        if (!Mathf.Approximately(grupo.alpha, alphaObjetivo))
        {
            float paso = duracionFundido > 0f ? Time.unscaledDeltaTime / duracionFundido : 1f;
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, alphaObjetivo, paso);
            if (grupo.alpha <= 0f) canvas.enabled = false;
        }
        if (!Abierta) return;

        // Máquina de escribir
        if (escribiendo)
        {
            caracteresVisibles += velocidadEscritura * Time.unscaledDeltaTime;
            int total = textoCuerpo.textInfo.characterCount;
            textoCuerpo.maxVisibleCharacters = Mathf.FloorToInt(caracteresVisibles);
            if (caracteresVisibles >= total) TerminarLinea();
        }

        if (!modal)
        {
            if (Time.time >= finAviso) Ocultar();
            return;
        }

        LeerTeclas();
    }

    void LeerTeclas()
    {
        var teclado = Keyboard.current;
        if (teclado == null || Time.frameCount == frameApertura) return;

        if (teclado.escapeKey.wasPressedThisFrame)
        {
            var cerrar = alCerrar;
            Ocultar();
            cerrar?.Invoke();
            return;
        }

        bool avanzar = teclado.eKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame ||
                       teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame;

        if (escribiendo)
        {
            if (avanzar) TerminarLinea();
            return;
        }

        bool ultima = indiceLinea >= lineas.Count - 1;
        if (!ultima)
        {
            if (avanzar) EmpezarLinea(indiceLinea + 1);
            return;
        }

        if (opciones.Count == 0)
        {
            if (avanzar) Elegir(-1);
            return;
        }

        for (int i = 0; i < opciones.Count && i < teclasNumero.Length; i++)
        {
            if (teclado[teclasNumero[i]].wasPressedThisFrame || teclado[teclasNumpad[i]].wasPressedThisFrame)
            {
                Elegir(i);
                return;
            }
        }
        if (!string.IsNullOrEmpty(opcionVolver) &&
            (teclado.digit0Key.wasPressedThisFrame || teclado.numpad0Key.wasPressedThisFrame))
            Elegir(-1);
    }

    void Elegir(int indice)
    {
        var elegir = alElegir;
        alElegir = null;
        elegir?.Invoke(indice);
    }

    // ---------------------------------------------------------------- Contenido

    void Abrir()
    {
        canvas.enabled = true;
        alphaObjetivo = 1f;
    }

    void PonerNombre(string nombre, string titulo)
    {
        textoNombre.text = string.IsNullOrEmpty(titulo)
            ? $"<b>{nombre.ToUpperInvariant()}</b>"
            : $"<b>{nombre.ToUpperInvariant()}</b>  <color=#{ColorUtility.ToHtmlStringRGB(colorSecundario)}>·  {titulo}</color>";
    }

    void EmpezarLinea(int indice)
    {
        indiceLinea = indice;
        textoCuerpo.text = lineas[indice];
        textoCuerpo.ForceMeshUpdate();
        textoCuerpo.maxVisibleCharacters = 0;
        caracteresVisibles = 0f;
        escribiendo = true;
        MostrarOpciones(false);
        textoPista.text = "";
    }

    void TerminarLinea()
    {
        escribiendo = false;
        textoCuerpo.maxVisibleCharacters = int.MaxValue;
        if (!modal) return;

        bool ultima = indiceLinea >= lineas.Count - 1;
        bool conOpciones = ultima && opciones.Count > 0;
        MostrarOpciones(conOpciones);

        string acento = ColorUtility.ToHtmlStringRGB(colorAcento);
        if (conOpciones)
        {
            string rango = opciones.Count == 1 ? "1" : $"1-{opciones.Count}";
            string volver = string.IsNullOrEmpty(opcionVolver) ? "" : $"      <color=#{acento}>0</color>  {opcionVolver.ToLowerInvariant()}";
            textoPista.text = $"<color=#{acento}>{rango}</color>  elegir{volver}      <color=#{acento}>Esc</color>  cerrar";
        }
        else
        {
            textoPista.text = $"<color=#{acento}>E</color>  {(ultima ? "terminar" : "continuar")}      <color=#{acento}>Esc</color>  cerrar";
        }
    }

    void MostrarOpciones(bool mostrar)
    {
        contenedorOpciones.gameObject.SetActive(mostrar);
        if (!mostrar) return;

        int total = opciones.Count + (string.IsNullOrEmpty(opcionVolver) ? 0 : 1);
        while (lineasOpcion.Count < total)
            lineasOpcion.Add(CrearTexto("Opcion", contenedorOpciones, 26f, colorTexto, FontStyles.Normal));

        string acento = ColorUtility.ToHtmlStringRGB(colorAcento);
        for (int i = 0; i < lineasOpcion.Count; i++)
        {
            bool activa = i < total;
            lineasOpcion[i].gameObject.SetActive(activa);
            if (!activa) continue;
            bool esVolver = i == opciones.Count;
            string tecla = esVolver ? "0" : (i + 1).ToString();
            string texto = esVolver ? opcionVolver : opciones[i];
            lineasOpcion[i].text = $"<color=#{acento}><b>{tecla}</b></color>    {texto}";
            lineasOpcion[i].color = esVolver ? colorSecundario : colorTexto;
        }
    }

    // ---------------------------------------------------------------- Construcción

    void Construir()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var escalador = gameObject.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // Caja
        var caja = new GameObject("Caja", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var rtCaja = (RectTransform)caja.transform;
        rtCaja.SetParent(transform, false);
        rtCaja.anchorMin = rtCaja.anchorMax = new Vector2(0.5f, 0f);
        rtCaja.pivot = new Vector2(0.5f, 0f);
        rtCaja.anchoredPosition = new Vector2(0f, margenInferior);
        rtCaja.sizeDelta = new Vector2(anchoCaja, 0f);
        caja.GetComponent<Image>().color = colorFondo;
        var vlg = caja.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(44, 44, 30, 24);
        vlg.spacing = 14f;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        caja.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Filete dorado superior (fuera del layout)
        var acento = new GameObject("Acento", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rtAcento = (RectTransform)acento.transform;
        rtAcento.SetParent(rtCaja, false);
        acento.GetComponent<LayoutElement>().ignoreLayout = true;
        rtAcento.anchorMin = new Vector2(0f, 1f);
        rtAcento.anchorMax = new Vector2(1f, 1f);
        rtAcento.pivot = new Vector2(0.5f, 1f);
        rtAcento.sizeDelta = new Vector2(0f, 3f);
        acento.GetComponent<Image>().color = colorAcento;

        textoNombre = CrearTexto("Nombre", rtCaja, 21f, colorAcento, FontStyles.Normal);
        textoNombre.characterSpacing = 6f;

        textoCuerpo = CrearTexto("Texto", rtCaja, 30f, colorTexto, FontStyles.Normal);
        textoCuerpo.lineSpacing = 8f;

        var opcionesGO = new GameObject("Opciones", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contenedorOpciones = (RectTransform)opcionesGO.transform;
        contenedorOpciones.SetParent(rtCaja, false);
        var vlgOp = opcionesGO.GetComponent<VerticalLayoutGroup>();
        vlgOp.padding = new RectOffset(8, 0, 6, 4);
        vlgOp.spacing = 8f;
        vlgOp.childControlWidth = vlgOp.childControlHeight = true;
        vlgOp.childForceExpandWidth = true;
        vlgOp.childForceExpandHeight = false;

        textoPista = CrearTexto("Pista", rtCaja, 18f, colorSecundario, FontStyles.Normal);
        textoPista.alignment = TextAlignmentOptions.Right;
        textoPista.characterSpacing = 2f;
    }

    static TextMeshProUGUI CrearTexto(string nombre, Transform padre, float tamano, Color color, FontStyles estilo)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = tamano;
        tmp.color = color;
        tmp.fontStyle = estilo;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }
}
