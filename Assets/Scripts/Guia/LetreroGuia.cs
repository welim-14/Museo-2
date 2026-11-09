using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Placa flotante sobre la cabeza del guía: su nombre y un aviso corto ("[E] Hablar", "Sígame, por favor"...).
// Construye su propio Canvas en el mundo y siempre mira a la cámara.
public class LetreroGuia : MonoBehaviour
{
    public Color colorFondo = new Color(0.07f, 0.075f, 0.09f, 0.85f);
    public Color colorAcento = new Color(0.85f, 0.71f, 0.38f, 1f);
    public Color colorTexto = new Color(0.95f, 0.94f, 0.91f, 1f);
    [Tooltip("Metros por píxel del canvas.")]
    public float escala = 0.0032f;
    public float duracionFundido = 0.2f;

    Canvas canvas;
    CanvasGroup grupo;
    TextMeshProUGUI textoNombre, textoAviso;
    float alphaObjetivo;
    Camera camara;

    void Awake()
    {
        Construir();
        // En la capa UI para que no salga en el minimapa
        int capaUI = LayerMask.NameToLayer("UI");
        if (capaUI >= 0)
            foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = capaUI;
        grupo.alpha = 0f;
        canvas.enabled = false;
    }

    public void PonerNombre(string nombre)
    {
        textoNombre.text = nombre.ToUpperInvariant();
    }

    public void Mostrar(string aviso)
    {
        if (textoAviso.text != aviso) textoAviso.text = aviso;
        canvas.enabled = true;
        alphaObjetivo = 1f;
    }

    public void Ocultar()
    {
        alphaObjetivo = 0f;
    }

    void LateUpdate()
    {
        if (!Mathf.Approximately(grupo.alpha, alphaObjetivo))
        {
            float paso = duracionFundido > 0f ? Time.deltaTime / duracionFundido : 1f;
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, alphaObjetivo, paso);
            if (grupo.alpha <= 0f) canvas.enabled = false;
        }
        if (!canvas.enabled) return;

        if (camara == null || !camara.isActiveAndEnabled) camara = Camera.main;
        if (camara != null) transform.rotation = camara.transform.rotation;
    }

    void Construir()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(420f, 120f);
        rt.localScale = Vector3.one * escala;

        var placa = new GameObject("Placa", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var rtPlaca = (RectTransform)placa.transform;
        rtPlaca.SetParent(rt, false);
        rtPlaca.anchorMin = rtPlaca.anchorMax = new Vector2(0.5f, 0f);
        rtPlaca.pivot = new Vector2(0.5f, 0f);
        placa.GetComponent<Image>().color = colorFondo;
        var vlg = placa.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(28, 28, 14, 14);
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;
        var ajuste = placa.GetComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var filete = new GameObject("Acento", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rtFilete = (RectTransform)filete.transform;
        rtFilete.SetParent(rtPlaca, false);
        filete.GetComponent<LayoutElement>().ignoreLayout = true;
        rtFilete.anchorMin = new Vector2(0f, 1f);
        rtFilete.anchorMax = new Vector2(1f, 1f);
        rtFilete.pivot = new Vector2(0.5f, 1f);
        rtFilete.sizeDelta = new Vector2(0f, 3f);
        filete.GetComponent<Image>().color = colorAcento;

        textoNombre = CrearTexto("Nombre", rtPlaca, 22f, colorAcento);
        textoNombre.characterSpacing = 8f;
        textoNombre.fontStyle = FontStyles.Bold;
        textoAviso = CrearTexto("Aviso", rtPlaca, 30f, colorTexto);
    }

    static TextMeshProUGUI CrearTexto(string nombre, Transform padre, float tamano, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = tamano;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }
}
