using UnityEngine;
using UnityEngine.InputSystem;

public class Move : MonoBehaviour
{
    public float deltaMovement = 10f;
    public float deltaRotation = 70f;
    public float Z = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // No se requiere inicializacion por ahora
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        Rota();
    }

    void Movement()
    {
        // Obtenemos el estado del teclado con el nuevo Input System
        var keyboard = Keyboard.current;

        if(keyboard == null) return;

        if(keyboard.wKey.isPressed)
        {
            transform.Translate(Vector3.forward * deltaMovement * Time.deltaTime);
        }

        if(keyboard.sKey.isPressed)
        {
            transform.Translate(Vector3.back * deltaMovement * Time.deltaTime);
        }

        if(keyboard.aKey.isPressed)
        {
            transform.Translate(Vector3.left * deltaMovement * Time.deltaTime);
        }

        if(keyboard.dKey.isPressed)
        {
            transform.Translate(Vector3.right * deltaMovement * Time.deltaTime);
        }
    }

    void Rota()
    {
        // Obtenemos el estado del teclado con el nuevo Input System
        var keyboard = Keyboard.current;
        
        if(keyboard == null) return;

        if(keyboard.qKey.isPressed)
        {
            transform.Rotate(Vector3.up, -deltaRotation * Time.deltaTime);
        }
        if(keyboard.eKey.isPressed)
        {
            transform.Rotate(Vector3.up, deltaRotation * Time.deltaTime);
        }
    }
}
