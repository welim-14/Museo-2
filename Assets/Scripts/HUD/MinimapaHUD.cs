using UnityEngine;
using UnityEngine.UI;

// Minimapa circular que gira con el visitante: "adelante" siempre queda arriba y la flecha
// central no se mueve; la marca "N" recorre el anillo indicando dónde está el norte.
// Crea su propia cámara cenital ortográfica y su RenderTexture al arrancar.
// La cámara se coloca poco por encima del visitante (alturaCorte) para que los techos no tapen la planta.
public class MinimapaHUD : MonoBehaviour
{
    [Header("Referencias")]
    public RawImage mapa;
    [Tooltip("Marca del norte; se mueve sobre el anillo.")]
    public RectTransform norte;
    public float radioNorte = 115f;

    [Header("Cámara")]
    [Tooltip("Metros de mundo que caben de lado a lado del mapa.")]
    public float metrosVisibles = 30f;
    [Tooltip("Altura sobre los pies del visitante desde la que mira la cámara (corta paredes y techos).")]
    public float alturaCorte = 2.6f;
    public int resolucion = 512;
    public Color colorFondo = new Color(0.05f, 0.055f, 0.065f, 1f);
    [Tooltip("Capas que se dibujan en el mapa. La capa UI se excluye siempre.")]
    public LayerMask capas = ~0;

    Camera camara;
    RenderTexture textura;
    Transform jugador;

    void Start()
    {
        textura = new RenderTexture(resolucion, resolucion, 16) { name = "Minimapa" };
        textura.Create();

        var go = new GameObject("Cámara Minimapa");
        camara = go.AddComponent<Camera>();
        camara.orthographic = true;
        camara.orthographicSize = metrosVisibles * 0.5f;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = colorFondo;
        camara.nearClipPlane = 0.05f;
        camara.farClipPlane = 80f;
        int capaUI = LayerMask.NameToLayer("UI");
        camara.cullingMask = capaUI >= 0 ? capas & ~(1 << capaUI) : capas;
        camara.targetTexture = textura;
        camara.allowHDR = false;
        camara.allowMSAA = false;
        camara.depth = -50f;

        if (mapa != null) mapa.texture = textura;
    }

    void LateUpdate()
    {
        if (jugador == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go == null) return;
            jugador = go.transform;
        }

        float rumbo = jugador.eulerAngles.y;
        camara.transform.SetPositionAndRotation(jugador.position + Vector3.up * alturaCorte,
                                                Quaternion.Euler(90f, rumbo, 0f));

        if (norte != null)
        {
            float rad = -rumbo * Mathf.Deg2Rad;
            norte.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radioNorte;
        }
    }

    void OnDestroy()
    {
        if (camara != null) Destroy(camara.gameObject);
        if (textura != null)
        {
            textura.Release();
            Destroy(textura);
        }
    }
}
