using UnityEngine;
using UnityEngine.Video;

// Marco o pantalla que muestra una imagen o un video.
// Hijos esperados: "Marco" (cubo del borde) y "Pantalla" (Quad con material Unlit/Texture).
// La pantalla mira hacia -Z local: el visitante se coloca delante, del lado -Z.
// Con "ajustarProporcion" el marco se redimensiona a la proporción del contenido
// sin pasarse de "tamanoMaximo".
// Los videos solo se reproducen mientras el puntero (PunteroInteraccion) señala el marco;
// el collider del hijo "Marco" es el que recibe el rayo.
[ExecuteAlways]
public class MarcoMultimedia : MonoBehaviour, IInteractuable
{
    public enum TipoContenido { Vacio, Imagen, Video }

    [Header("Contenido")]
    public TipoContenido tipo = TipoContenido.Vacio;
    public Texture imagen;
    public VideoClip video;
    [Tooltip("Alternativa a 'video': http(s)://... o un archivo dentro de StreamingAssets.")]
    public string urlVideo = "";
    public string titulo = "";

    [Header("Tamaño")]
    public Vector2 tamanoMaximo = new Vector2(2.8f, 2f);
    public bool ajustarProporcion = true;
    public float grosorBorde = 0.1f;
    public float profundidadMarco = 0.08f;

    [Header("Video")]
    [Tooltip("El video se carga cuando el visitante está a esta distancia, para que empiece al instante al señalarlo.")]
    public float distanciaPrecarga = 15f;
    [Tooltip("Al volver a señalar el marco, sigue donde se quedó en lugar de empezar de nuevo.")]
    public bool continuarDondeQuedo = true;
    public bool repetir = true;
    public bool conSonido = true;
    [Range(0f, 1f)] public float volumen = 0.8f;

    [Header("Referencias")]
    public Transform marco;
    public Renderer pantalla;

    private VideoPlayer reproductor;
    private AudioSource fuente;
    private RenderTexture textura;
    private MaterialPropertyBlock bloque;
    private Vector2 proporcionVideo;
    private bool preparando;
    private bool senalado;
    private static readonly int idTextura = Shader.PropertyToID("_MainTex");

    bool TieneVideo => video != null || !string.IsNullOrEmpty(urlVideo);

    public string TextoAyuda
    {
        get
        {
            if (tipo != TipoContenido.Video || !TieneVideo) return titulo;
            string nombre = !string.IsNullOrEmpty(titulo) ? titulo : video != null ? video.name : "Video";
            if (reproductor == null || preparando) return "Cargando: " + nombre;
            return reproductor.isPlaying ? "Reproduciendo: " + nombre : nombre;
        }
    }

    public void AlEntrarPuntero()
    {
        senalado = true;
        if (reproductor != null && !continuarDondeQuedo) reproductor.time = 0;
    }

    public void AlSalirPuntero()
    {
        senalado = false;
        if (reproductor != null && reproductor.isPlaying) reproductor.Pause();
    }

    public void AlHacerClic() { }

    void OnEnable()
    {
        BuscarReferencias();
        Aplicar();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Cambiar transforms dentro de OnValidate da avisos; se aplica en el siguiente tick del editor
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) { BuscarReferencias(); Aplicar(); } };
    }
#endif

    void BuscarReferencias()
    {
        if (marco == null) marco = transform.Find("Marco");
        if (pantalla == null) { Transform p = transform.Find("Pantalla"); if (p) pantalla = p.GetComponent<Renderer>(); }
    }

    // Ajusta tamaño y textura de la pantalla según el contenido actual
    public void Aplicar()
    {
        if (pantalla == null) return;

        Vector2 proporcion = Vector2.zero;
        Texture tex = Texture2D.blackTexture;

        if (tipo == TipoContenido.Imagen && imagen != null)
        {
            tex = imagen;
            proporcion = new Vector2(imagen.width, imagen.height);
        }
        else if (tipo == TipoContenido.Video)
        {
            if (textura != null) tex = textura;
            if (proporcionVideo != Vector2.zero) proporcion = proporcionVideo;
            else if (video != null) proporcion = new Vector2(video.width, video.height);
        }

        Vector2 tam = tamanoMaximo;
        if (ajustarProporcion && proporcion.x > 0f && proporcion.y > 0f)
        {
            float aspecto = proporcion.x / proporcion.y;
            if (aspecto > tamanoMaximo.x / tamanoMaximo.y) tam = new Vector2(tamanoMaximo.x, tamanoMaximo.x / aspecto);
            else tam = new Vector2(tamanoMaximo.y * aspecto, tamanoMaximo.y);
        }

        pantalla.transform.localPosition = new Vector3(0f, 0f, -profundidadMarco / 2f - 0.003f);
        pantalla.transform.localRotation = Quaternion.identity;
        pantalla.transform.localScale = new Vector3(tam.x, tam.y, 1f);
        if (marco != null)
        {
            marco.localPosition = Vector3.zero;
            marco.localScale = new Vector3(tam.x + grosorBorde * 2f, tam.y + grosorBorde * 2f, profundidadMarco);
        }

        if (bloque == null) bloque = new MaterialPropertyBlock();
        pantalla.GetPropertyBlock(bloque);
        bloque.SetTexture(idTextura, tex);
        pantalla.SetPropertyBlock(bloque);
    }

    void Update()
    {
        if (!Application.isPlaying || tipo != TipoContenido.Video || !TieneVideo) return;

        // Precargar cuando el visitante se acerca (o en cuanto lo señala)
        Camera cam = Camera.main;
        bool cerca = cam != null && Vector3.Distance(cam.transform.position, transform.position) <= distanciaPrecarga;
        if ((cerca || senalado) && reproductor == null) CrearReproductor();
        if (reproductor == null) return;

        if (!reproductor.isPrepared)
        {
            if (!preparando) { preparando = true; reproductor.Prepare(); }
            return;
        }

        // Reproducir solo mientras se le señala
        if (senalado && !reproductor.isPlaying) reproductor.Play();
        else if (!senalado && reproductor.isPlaying) reproductor.Pause();
    }

    void CrearReproductor()
    {
        reproductor = gameObject.AddComponent<VideoPlayer>();
        reproductor.playOnAwake = false;
        reproductor.isLooping = repetir;
        reproductor.renderMode = VideoRenderMode.RenderTexture;

        if (video != null)
        {
            reproductor.source = VideoSource.VideoClip;
            reproductor.clip = video;
        }
        else
        {
            reproductor.source = VideoSource.Url;
            reproductor.url = urlVideo.Contains("://") ? urlVideo
                : System.IO.Path.Combine(Application.streamingAssetsPath, urlVideo);
        }

        if (conSonido)
        {
            fuente = gameObject.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 1f;
            fuente.rolloffMode = AudioRolloffMode.Linear;
            fuente.minDistance = 2f;
            fuente.maxDistance = distanciaPrecarga;
            fuente.volume = volumen;
            reproductor.audioOutputMode = VideoAudioOutputMode.AudioSource;
            reproductor.SetTargetAudioSource(0, fuente);
        }
        else
        {
            reproductor.audioOutputMode = VideoAudioOutputMode.None;
        }

        reproductor.prepareCompleted += AlPrepararse;
        reproductor.errorReceived += (vp, msg) => Debug.LogWarning(name + ": no se pudo reproducir el video (" + msg + ")");
    }

    void AlPrepararse(VideoPlayer vp)
    {
        preparando = false;
        int w = (int)vp.width, h = (int)vp.height;
        if (w <= 0 || h <= 0) { w = 1280; h = 720; }

        textura = new RenderTexture(w, h, 0);
        textura.Create();
        vp.targetTexture = textura;
        proporcionVideo = new Vector2(w, h);
        Aplicar();
    }

    void OnDisable()
    {
        if (reproductor != null) reproductor.Pause();
    }

    void OnDestroy()
    {
        if (textura != null) { textura.Release(); Destroy(textura); }
    }
}
