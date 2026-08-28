using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Proyectil2 : MonoBehaviour
{
    // ================= CONTADOR GLOBAL =================
    public static int objetosDestruidos = 0;

    [Header("Rebote")]
    [Range(0f, 1f)]
    public float fuerzaRebote = 0.8f;
    public int maxRebotes = 3;

    [Header("Control de vida")]
    public float tiempoDeVida = 5f;
    public float velocidadMinima = 0.5f;

    [Header("Efectos (Asignar en el Inspector)")]
    public AudioClip sonidoImpacto;          // 🔊 arrastrar audio aquí
    [Range(0f, 1f)] public float volumen = 1f;

    public GameObject efectoParticulas;      // 💥 prefab de partículas
    public float tiempoParticulas = 2f;      // duración del efecto

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
        // ================= DESTRUIR OBJETOS =================
        if (collision.gameObject.CompareTag("ItemObstaculo"))
        {
            objetosDestruidos++;
            ReproducirEfectos();
            Destroy(collision.gameObject);
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

            // 🔊 efecto leve en rebote
            ReproducirEfectos();

            return;
        }

        // ================= DESTRUIR PROYECTIL =================
        DestruirProyectil();
    }

    void ReproducirEfectos()
    {
        // 🔊 SONIDO
        if (sonidoImpacto != null)
        {
            AudioSource.PlayClipAtPoint(sonidoImpacto, transform.position, volumen);
        }

        // 💥 PARTÍCULAS
        if (efectoParticulas != null)
        {
            GameObject efecto = Instantiate(
                efectoParticulas,
                transform.position,
                Quaternion.identity
            );

            Destroy(efecto, tiempoParticulas);
        }
    }

    void DestruirProyectil()
    {
        ReproducirEfectos();
        Destroy(gameObject);
    }
}
