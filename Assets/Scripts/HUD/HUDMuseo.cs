using UnityEngine;
using UnityEngine.InputSystem;

// Raíz del HUD del museo. Muestra u oculta todo el HUD con una tecla (H) y lo atenúa
// mientras el visitante conversa con el guía, para que la caja de diálogo no compita con él.
// Las piezas (BrujulaHUD, MinimapaHUD, SalaHUD, InteraccionHUD) funcionan cada una por su cuenta.
[RequireComponent(typeof(CanvasGroup))]
public class HUDMuseo : MonoBehaviour
{
    public Key teclaOcultar = Key.H;
    [Range(0f, 1f)]
    [Tooltip("Opacidad del HUD mientras hay una conversación con el guía.")]
    public float opacidadEnConversacion = 0.15f;
    public float duracionFundido = 0.25f;

    CanvasGroup grupo;
    bool oculto;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
    }

    void Update()
    {
        bool conversando = UIDialogoGuia.HayConversacion;
        var teclado = Keyboard.current;
        if (!conversando && teclado != null && teclado[teclaOcultar].wasPressedThisFrame)
            oculto = !oculto;

        float objetivo = oculto ? 0f : conversando ? opacidadEnConversacion : 1f;
        float paso = duracionFundido > 0f ? Time.unscaledDeltaTime / duracionFundido : 1f;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, objetivo, paso);
    }
}
