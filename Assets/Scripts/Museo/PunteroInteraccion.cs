using UnityEngine;
using UnityEngine.InputSystem;

// Va en la cámara del Player. Lanza un rayo cada frame y avisa a los IInteractuable
// cuando el puntero entra, sale o hace clic. Con el cursor bloqueado (modo primera
// persona) el puntero es el centro de la pantalla; al señalar algo interactivo se
// dibuja un punto y el texto de ayuda (el HUD ya tiene su propia mira).
[RequireComponent(typeof(Camera))]
public class PunteroInteraccion : MonoBehaviour
{
    [Tooltip("Distancia máxima a la que se puede señalar un objeto.")]
    public float distanciaMaxima = 20f;
    public LayerMask capas = ~0;
    [Tooltip("Si el cursor quedó libre (por ejemplo tras abrir un enlace), un clic en el vacío lo vuelve a bloquear.")]
    public bool rebloquearCursorAlHacerClic = true;

    [Header("Indicador")]
    public bool mostrarIndicador = true;
    public Color colorIndicador = new Color(1f, 0.8f, 0.3f, 1f);

    private Camera cam;
    private IInteractuable actual;
    private GUIStyle estiloAyuda;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        IInteractuable nuevo = null;

        Ray rayo = Cursor.lockState == CursorLockMode.Locked || Mouse.current == null
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Se ignoran los triggers para que las zonas de los portales no tapen los objetos
        if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, capas, QueryTriggerInteraction.Ignore))
            nuevo = hit.collider.GetComponentInParent<IInteractuable>();

        if (!ReferenceEquals(nuevo, actual))
        {
            if (actual as Object != null) actual.AlSalirPuntero();
            actual = nuevo;
            if (actual != null) actual.AlEntrarPuntero();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (actual != null)
                actual.AlHacerClic();
            else if (rebloquearCursorAlHacerClic && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void OnDisable()
    {
        if (actual as Object != null) actual.AlSalirPuntero();
        actual = null;
    }

    void OnGUI()
    {
        if (!mostrarIndicador || actual == null) return;

        Vector2 centro = Cursor.lockState == CursorLockMode.Locked || Mouse.current == null
            ? new Vector2(Screen.width / 2f, Screen.height / 2f)
            : new Vector2(Mouse.current.position.ReadValue().x, Screen.height - Mouse.current.position.ReadValue().y);
        const float tam = 10f;
        Color previo = GUI.color;
        GUI.color = colorIndicador;
        GUI.DrawTexture(new Rect(centro.x - tam / 2f, centro.y - tam / 2f, tam, tam), Texture2D.whiteTexture);
        GUI.color = previo;

        if (!string.IsNullOrEmpty(actual.TextoAyuda))
        {
            if (estiloAyuda == null)
            {
                estiloAyuda = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.UpperCenter };
                estiloAyuda.normal.textColor = Color.white;
            }
            GUI.Label(new Rect(centro.x - 200f, centro.y + 16f, 400f, 30f), actual.TextoAyuda, estiloAyuda);
        }
    }
}
