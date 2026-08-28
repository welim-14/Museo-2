using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ImpulsoInicialSpace : MonoBehaviour
{
    public Vector3 direccionImpulso = new Vector3(0, 1, 0);
    public float fuerza = 5f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            rb.AddForce(direccionImpulso.normalized * fuerza, ForceMode.Impulse);
        }
    }
}