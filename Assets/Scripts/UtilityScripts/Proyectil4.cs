using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Proyectil4 : MonoBehaviour
{
    public static int objetosDestruidos = 0;

    [Header("Rebote")]
    [Range(0f, 1f)]
    public float fuerzaRebote = 0.8f;
    public int maxRebotes = 3;

    [Header("Control de vida")]
    public float tiempoDeVida = 5f;
    public float velocidadMinima = 0.5f;

    [Header("Impacto con Objeto (ItemObstaculo)")]
    public AudioClip sonidoImpactoObjeto;
    public GameObject efectoImpactoObjeto;

    [Header("Rebote")]
    public AudioClip sonidoRebote;

    [Header("Impacto final")]
    public AudioClip sonidoImpactoSuelo;
    public GameObject efectoImpactoSuelo;

    [Header("Configuración")]
    [Range(0f, 1f)] public float volumen = 1f;
    public float tiempoParticulas = 2f;

    [Header("Audio 3D")]
    public float minDistancia = 2f;
    public float maxDistancia = 20f;

    private int rebotesActuales = 0;
    private Rigidbody rb;

    private bool yaDestruido = false;
    private bool efectoYaReproducido = false; // 🔒 CLAVE

    // 🔥 NUEVO: CONTROL DE TIPO DE IMPACTO
    private bool impactoFueObstaculo = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        Invoke(nameof(DestruirProyectil), tiempoDeVida);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (yaDestruido) return;

        bool esObstaculo = collision.gameObject.CompareTag("ItemObstaculo");

        // ================= IMPACTO CON OBJETO =================
        if (esObstaculo)
        {
            objetosDestruidos++;
            Destroy(collision.gameObject);

            impactoFueObstaculo = true;

            // 💥 EFECTO ESPECÍFICO DE OBJETO
            ReproducirEfectos(sonidoImpactoObjeto, efectoImpactoObjeto);

            DestruirProyectil();
            return;
        }

        if (collision.contactCount == 0)
            return;

        // ================= REBOTE =================
        if (rebotesActuales < maxRebotes)
        {
            Vector3 normal = collision.contacts[0].normal;
            Vector3 velocidad = rb.linearVelocity;

            Vector3 reflejo = Vector3.Reflect(velocidad, normal) * fuerzaRebote;

            if (reflejo.magnitude < velocidadMinima)
            {
                DestruirProyectil();
                return;
            }

            rb.linearVelocity = reflejo;
            rebotesActuales++;

            // 🔊 SOLO sonido (SIN partículas)
            ReproducirSonido(sonidoRebote);

            return;
        }

        // ================= DESTRUCCIÓN FINAL =================
        DestruirProyectil();
    }

    // ================= EFECTOS =================
    void ReproducirEfectos(AudioClip sonido, GameObject particulas)
    {
        // 🔒 EVITA REPETICIÓN TOTAL
        if (efectoYaReproducido) return;
        efectoYaReproducido = true;

        GameObject temp = new GameObject("EfectoTemporal");
        temp.transform.position = transform.position;

        // 🔊 AUDIO
        if (sonido != null)
        {
            AudioSource audio = temp.AddComponent<AudioSource>();
            audio.clip = sonido;
            audio.volume = volumen;
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Logarithmic;
            audio.minDistance = minDistancia;
            audio.maxDistance = maxDistancia;
            audio.pitch = Random.Range(0.9f, 1.1f);
            audio.Play();
        }

        float duracion = tiempoParticulas;

        // 💥 PARTÍCULAS
        if (particulas != null)
        {
            GameObject efecto = Instantiate(particulas, transform.position, Quaternion.identity);
            efecto.transform.SetParent(temp.transform);

            ParticleSystem ps = efecto.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                var main = ps.main;
                duracion = main.duration + main.startLifetime.constantMax;
            }
        }

        Destroy(temp, duracion);
    }

    void ReproducirSonido(AudioClip sonido)
    {
        if (sonido == null) return;

        GameObject temp = new GameObject("AudioTemp");
        temp.transform.position = transform.position;

        AudioSource audio = temp.AddComponent<AudioSource>();
        audio.clip = sonido;
        audio.volume = volumen;
        audio.spatialBlend = 1f;
        audio.minDistance = minDistancia;
        audio.maxDistance = maxDistancia;
        audio.pitch = Random.Range(0.9f, 1.1f);
        audio.Play();

        Destroy(temp, sonido.length);
    }

    void DestruirProyectil()
    {
        if (yaDestruido) return;
        yaDestruido = true;

        // 🔥 SOLO si NO fue obstáculo
        if (!impactoFueObstaculo)
        {
            ReproducirEfectos(sonidoImpactoSuelo, efectoImpactoSuelo);
        }

        if (TryGetComponent<Collider>(out Collider col))
            col.enabled = false;

        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        if (TryGetComponent<MeshRenderer>(out MeshRenderer mr))
            mr.enabled = false;

        Destroy(gameObject, 0.2f);
    }

    void OnDestroy()
    {
        // 🔒 SOLO si nunca explotó y no fue obstáculo
        if (!yaDestruido && !efectoYaReproducido && !impactoFueObstaculo)
        {
            ReproducirEfectos(sonidoImpactoSuelo, efectoImpactoSuelo);
        }
    }
}