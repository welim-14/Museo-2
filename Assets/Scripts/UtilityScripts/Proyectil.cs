using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Proyectil : MonoBehaviour
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

    private int rebotesActuales = 0;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Configuración recomendada para proyectiles rápidos
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        // Destruir automáticamente después de cierto tiempo
        Destroy(gameObject, tiempoDeVida);
    }

    void OnCollisionEnter(Collision collision)
    {
        // ================= DESTRUIR OBJETOS =================
        if (collision.gameObject.CompareTag("ItemObstaculo"))
        {
            objetosDestruidos++;
            Destroy(collision.gameObject);
        }

        // ================= VALIDAR CONTACTO =================
        if (collision.contactCount == 0)
            return;

        // ================= REBOTE =================
        if (rebotesActuales < maxRebotes)
        {
            Vector3 normal = collision.contacts[0].normal;

            // Usar linearVelocity (Unity moderno)
            Vector3 velocidad = rb.linearVelocity;

            Vector3 reflejo = Vector3.Reflect(velocidad, normal) * fuerzaRebote;

            // Evitar rebotes infinitos por velocidad muy baja
            if (reflejo.magnitude < velocidadMinima)
            {
                Destroy(gameObject);
                return;
            }

            rb.linearVelocity = reflejo;
            rebotesActuales++;

            return;
        }

        // ================= DESTRUIR PROYECTIL =================
        Destroy(gameObject);
    }
}

