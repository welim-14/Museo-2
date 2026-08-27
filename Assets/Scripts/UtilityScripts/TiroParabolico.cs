 using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TiroParabolico : MonoBehaviour
{
    [Header("Parámetros del lanzamiento")]
    [Tooltip("Velocidad inicial del proyectil")]
    public float velocidadInicial = 10f;

    [Tooltip("Ángulo de lanzamiento en grados")]
    [Range(0, 90)]
    public float angulo = 45f;

    private Rigidbody rb;
    private bool lanzado = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Input Manager (tecla Space)
        if (Input.GetKeyDown(KeyCode.Space) && !lanzado)
        {
            Lanzar();
        }
    }

    void Lanzar()
    {
        float anguloRad = angulo * Mathf.Deg2Rad;

        float vx = velocidadInicial * Mathf.Cos(anguloRad);
        float vy = velocidadInicial * Mathf.Sin(anguloRad);

        Vector3 velocidad = new Vector3(vx, vy, 0);

        rb.linearVelocity = Vector3.zero; 
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(velocidad, ForceMode.Impulse);

        lanzado = true;
    }
}

