using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Proyectil3 : MonoBehaviour
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

    [Header("Impacto final (suelo / destrucción)")]
    public AudioClip sonidoImpactoSuelo;
    public GameObject efectoImpactoSuelo;

    [Header("Configuración general")]
    [Range(0f, 1f)] public float volumen = 1f;
    public float tiempoParticulas = 2f;

    [Header("Audio 3D")]
    public float minDistancia = 2f;
    public float maxDistancia = 20f;

    private int rebotesActuales = 0;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    void OnCollisionEnter(Collision collision)
    {
        bool esObstaculo = collision.gameObject.CompareTag("ItemObstaculo");

        // ================= IMPACTO CON OBJETO =================
        if (esObstaculo)
        {
            objetosDestruidos++;

            ReproducirEfectos(sonidoImpactoObjeto, efectoImpactoObjeto);

            Destroy(collision.gameObject);
            Destroy(gameObject);
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

            // 🔊 sonido de rebote (3D)
            ReproducirEfectos(sonidoRebote, null);

            return;
        }

        // ================= DESTRUCCIÓN FINAL =================
        DestruirProyectil();
    }

    void ReproducirEfectos(AudioClip sonido, GameObject particulas)
    {
        GameObject temp = new GameObject("EfectoTemporal");
        temp.transform.position = transform.position;

        // 🔊 AUDIO 3D CON ATENUACIÓN
        if (sonido != null)
        {
            AudioSource audio = temp.AddComponent<AudioSource>();
            audio.clip = sonido;
            audio.volume = volumen;

            // 🎯 AUDIO ESPACIAL (CLAVE)
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Logarithmic;
            audio.minDistance = minDistancia;
            audio.maxDistance = maxDistancia;

            audio.Play();
        }

        // 💥 PARTÍCULAS
        if (particulas != null)
        {
            GameObject efecto = Instantiate(particulas, transform.position, Quaternion.identity);
            efecto.transform.SetParent(temp.transform);
        }

        Destroy(temp, tiempoParticulas);
    }

    void DestruirProyectil()
    {
        ReproducirEfectos(sonidoImpactoSuelo, efectoImpactoSuelo);
        Destroy(gameObject);
    }
}