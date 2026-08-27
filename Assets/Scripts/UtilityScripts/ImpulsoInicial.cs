using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ImpulsoInicial : MonoBehaviour
{
    public Vector3 direccionImpulso = new Vector3(0, 1, 0);
    public float fuerza = 5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.AddForce(direccionImpulso.normalized * fuerza, ForceMode.Impulse);
    }
}
