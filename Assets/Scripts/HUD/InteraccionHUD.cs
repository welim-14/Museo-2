using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mira central y mensaje inferior ("——— Ichigo Kurosaki ———").
// Lee de PunteroInteraccion qué se está señalando y qué mensaje toca mostrar
// (título de la obra, audio o video en reproducción), y apaga el indicador antiguo de ese script.
public class InteraccionHUD : MonoBehaviour
{
    [Header("Referencias")]
    public Image mira;
    public CanvasGroup grupoMensaje;
    public TextMeshProUGUI textoMensaje;

    [Header("Mira")]
    public float tamanoMira = 6f;
    public float tamanoMiraActiva = 12f;
    public Color colorMira = new Color(0.95f, 0.94f, 0.91f, 0.7f);
    public Color colorMiraActiva = new Color(0.85f, 0.71f, 0.38f, 1f);
    [Tooltip("Velocidad de la transición de la mira.")]
    public float suavizado = 14f;

    public float duracionFundido = 0.2f;

    PunteroInteraccion puntero;

    void LateUpdate()
    {
        if (puntero == null)
        {
            puntero = FindFirstObjectByType<PunteroInteraccion>();
            if (puntero != null) puntero.mostrarIndicador = false;
        }

        bool senalando = puntero != null && puntero.Senalando;
        string mensaje = puntero != null ? puntero.Mensaje : "";

        if (mira != null)
        {
            float t = 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime);
            float objetivo = senalando ? tamanoMiraActiva : tamanoMira;
            var rt = mira.rectTransform;
            float tam = Mathf.Lerp(rt.sizeDelta.x, objetivo, t);
            rt.sizeDelta = new Vector2(tam, tam);
            mira.color = Color.Lerp(mira.color, senalando ? colorMiraActiva : colorMira, t);
            // Con el cursor libre la mira no representa el puntero
            mira.enabled = Cursor.lockState == CursorLockMode.Locked;
        }

        bool hayMensaje = !string.IsNullOrEmpty(mensaje);
        if (hayMensaje && textoMensaje != null && textoMensaje.text != mensaje) textoMensaje.text = mensaje;
        if (grupoMensaje != null)
        {
            float paso = duracionFundido > 0f ? Time.unscaledDeltaTime / duracionFundido : 1f;
            grupoMensaje.alpha = Mathf.MoveTowards(grupoMensaje.alpha, hayMensaje ? 1f : 0f, paso);
        }
    }
}
