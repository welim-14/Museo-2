using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tira de brújula: marcas cada 15° que se desplazan según hacia dónde mira la cámara,
// cardinales en dorado y el rumbo en grados debajo del indicador central.
// Las marcas se generan al arrancar dentro de "marcas" (hijo de una ventana con RectMask2D).
public class BrujulaHUD : MonoBehaviour
{
    [Header("Referencias")]
    public RectTransform marcas;
    public TextMeshProUGUI textoGrados;

    [Header("Escala")]
    public float pixelesPorGrado = 4f;
    public float paso = 15f;

    [Header("Aspecto")]
    public Color colorCardinal = new Color(0.85f, 0.71f, 0.38f, 1f);
    public Color colorIntercardinal = new Color(0.95f, 0.94f, 0.91f, 0.9f);
    public Color colorMarca = new Color(0.95f, 0.94f, 0.91f, 0.45f);
    public float tamanoCardinal = 20f;
    public float tamanoIntercardinal = 15f;

    struct Marca
    {
        public RectTransform rt;
        public float angulo;
    }

    readonly List<Marca> lista = new List<Marca>();
    float anchoVisible;

    static readonly string[] nombres = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };

    void Awake()
    {
        if (marcas == null) return;
        for (float a = 0f; a < 360f; a += paso)
        {
            int ang = Mathf.RoundToInt(a);
            RectTransform rt;
            if (ang % 45 == 0)
            {
                bool cardinal = ang % 90 == 0;
                var tmp = CrearTexto(nombres[ang / 45], cardinal ? tamanoCardinal : tamanoIntercardinal,
                                     cardinal ? colorCardinal : colorIntercardinal, cardinal);
                rt = tmp.rectTransform;
            }
            else
            {
                var go = new GameObject("Marca " + ang, typeof(RectTransform), typeof(Image));
                rt = (RectTransform)go.transform;
                rt.SetParent(marcas, false);
                rt.sizeDelta = new Vector2(2f, 8f);
                var img = go.GetComponent<Image>();
                img.color = colorMarca;
                img.raycastTarget = false;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            lista.Add(new Marca { rt = rt, angulo = a });
        }
    }

    TextMeshProUGUI CrearTexto(string texto, float tamano, Color color, bool negrita)
    {
        var go = new GameObject(texto, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(marcas, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.color = color;
        tmp.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.rectTransform.sizeDelta = new Vector2(48f, 28f);
        return tmp;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 frente = cam.transform.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 0.0001f) return;
        float rumbo = Quaternion.LookRotation(frente).eulerAngles.y;

        if (anchoVisible <= 0f && marcas.parent is RectTransform ventana)
            anchoVisible = ventana.rect.width * 0.5f + 40f;

        foreach (var m in lista)
        {
            float x = Mathf.DeltaAngle(rumbo, m.angulo) * pixelesPorGrado;
            bool visible = Mathf.Abs(x) < anchoVisible;
            if (m.rt.gameObject.activeSelf != visible) m.rt.gameObject.SetActive(visible);
            if (visible) m.rt.anchoredPosition = new Vector2(x, 0f);
        }

        if (textoGrados != null)
        {
            int grados = Mathf.RoundToInt(rumbo) % 360;
            string texto = grados.ToString("000") + "°";
            if (textoGrados.text != texto) textoGrados.text = texto;
        }
    }
}
