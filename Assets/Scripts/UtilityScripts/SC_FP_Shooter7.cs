using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class SC_FP_Shooter7 : MonoBehaviour
{
    // ==========================================================
    // MOVIMIENTO
    // ==========================================================

    [Header("Movimiento")]
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float AlturaPersonaje = 2f;

    [Header("Movimiento - Estado")]
    public bool canMove = true;

    private float horizontal;
    private float vertical;
    private bool isRunning;

    // ==========================================================
    // CAMARA
    // ==========================================================

    [Header("Camara")]
    public Camera playerCamera;
    public Transform vrCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    private float rotationX = 0;

    // ==========================================================
    // UI
    // ==========================================================

    [Header("UI")]
    public Text TextPositionX;
    public Text TextPositionY;
    public Text TextPositionZ;

    public Text TextOrientacion;
    public Text TextAzimut;
    public Text TextAltitud;

    // ==========================================================
    // INFORMACION DE ESCENA
    // ==========================================================

    [Header("Información de escena")]

    [Tooltip("Muestra siempre el nombre de la escena actual.")]
    public Text TextNombreEscena;

    [Tooltip("Muestra el punto de interés más cercano.")]
    public Text TextPosicionEscena;

    // ==========================================================
    // ESCENA ACTUAL
    // ==========================================================

    private string nombreEscenaActual;

    // ==========================================================
    // PUNTOS DE INTERES
    // ==========================================================

    [System.Serializable]
    public class PuntoInteres
    {
        [Tooltip("Nombre que se mostrará al acercarse.")]
        public string nombre;

        [Tooltip("Objeto que representa físicamente el punto.")]
        public Transform punto;
    }

    [Header("Puntos de interés")]

    [Tooltip("Puntos dentro de esta escena que serán detectados.")]
    public List<PuntoInteres> puntosInteres =
        new List<PuntoInteres>();

    [Tooltip("Distancia máxima para detectar un punto de interés.")]
    public float distanciaPuntoInteres = 10f;

    [Tooltip("Mostrar la distancia al punto de interés.")]
    public bool mostrarDistanciaPunto = true;

    // ==========================================================
    // PUNTO DE INTERES ACTUAL
    // ==========================================================

    private PuntoInteres puntoInteresActual = null;

    private float distanciaPuntoActual =
        Mathf.Infinity;

    // ==========================================================
    // COMPONENTES
    // ==========================================================

    private CharacterController characterController;

    [HideInInspector]
    public Animator anim;

    private Vector3 moveDirection = Vector3.zero;

    // ==========================================================
    // AWAKE
    // ==========================================================

    void Awake()
    {
        // ======================================================
        // OBTENER COMPONENTES DEL MISMO GAMEOBJECT
        // ======================================================

        characterController =
            GetComponent<CharacterController>();

        anim =
            GetComponent<Animator>();

        if (characterController == null)
        {
            Debug.LogError(
                "SC_FP_Shooter7: El Player necesita " +
                "un CharacterController."
            );
        }

        if (anim == null)
        {
            Debug.LogError(
                "SC_FP_Shooter7: El Player necesita " +
                "un Animator directamente en el mismo GameObject."
            );
        }
    }

    // ==========================================================
    // START
    // ==========================================================

    void Start()
    {
        // ======================================================
        // CONFIGURAR CHARACTER CONTROLLER
        // ======================================================

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (characterController != null)
        {
            characterController.height =
                AlturaPersonaje;

            characterController.center =
                new Vector3(
                    0,
                    AlturaPersonaje / 2f,
                    0
                );
        }

        // ======================================================
        // CONFIGURAR ANIMATOR
        // ======================================================

        if (anim != null)
        {
            // IMPORTANTE:
            // El CharacterController mueve al Player.
            // El Animator solamente reproduce las animaciones.

            anim.applyRootMotion = false;
        }

        // ======================================================
        // INFORMACION DE ESCENA
        // ======================================================

        ObtenerInformacionEscena();

        // ======================================================
        // PUNTO DE INTERES
        // ======================================================

        OcultarPuntoInteres();
    }

    // ==========================================================
    // UPDATE
    // ==========================================================

    void Update()
    {
        if (!canMove)
        {
            DetenerAnimacionesMovimiento();
            return;
        }

        // ======================================================
        // INPUTS
        // ======================================================

        ProcesarInputs();

        // ======================================================
        // MOVIMIENTO
        // ======================================================

        MovimientoYFisicas();

        // ======================================================
        // ROTACION
        // ======================================================

        Rotacion();

        // ======================================================
        // ANIMACIONES
        // ======================================================

        ActualizarAnimaciones();

        // ======================================================
        // HUD
        // ======================================================

        textUpdate2();

        // ======================================================
        // PUNTOS DE INTERES
        // ======================================================

        DetectarPuntoInteres();
    }

    // ==========================================================
    // INPUTS
    // ==========================================================

    void ProcesarInputs()
    {
        horizontal = 0;
        vertical = 0;
        isRunning = false;

        if (Keyboard.current == null)
            return;

        // A
        if (Keyboard.current.aKey.isPressed)
        {
            horizontal = -1;
        }

        // D
        if (Keyboard.current.dKey.isPressed)
        {
            horizontal = 1;
        }

        // W
        if (Keyboard.current.wKey.isPressed)
        {
            vertical = 1;
        }

        // S
        if (Keyboard.current.sKey.isPressed)
        {
            vertical = -1;
        }

        // SHIFT
        isRunning =
            Keyboard.current.leftShiftKey.isPressed;
    }

    // ==========================================================
    // MOVIMIENTO Y FISICAS
    // ==========================================================

    void MovimientoYFisicas()
    {
        if (characterController == null)
            return;

        // ======================================================
        // DIRECCIONES
        // ======================================================

        Vector3 forward =
            transform.TransformDirection(
                Vector3.forward
            );

        Vector3 right =
            transform.TransformDirection(
                Vector3.right
            );

        // ======================================================
        // MOVIMIENTO HORIZONTAL
        // ======================================================

        Vector3 movimiento =
            (forward * vertical) +
            (right * horizontal);

        // Evitar velocidad diagonal excesiva.
        movimiento =
            Vector3.ClampMagnitude(
                movimiento,
                1f
            );

        // ======================================================
        // VELOCIDAD
        // ======================================================

        float velocidad =
            isRunning
            ? runningSpeed
            : walkingSpeed;

        moveDirection.x =
            movimiento.x * velocidad;

        moveDirection.z =
            movimiento.z * velocidad;

        // ======================================================
        // GRAVEDAD Y SALTO
        // ======================================================

        if (characterController.isGrounded)
        {
            // Mantener al personaje pegado al suelo.
            if (moveDirection.y < 0)
            {
                moveDirection.y = -0.5f;
            }

            // SALTO
            if (Keyboard.current != null &&
                Keyboard.current.spaceKey
                    .wasPressedThisFrame)
            {
                moveDirection.y =
                    jumpSpeed;

                if (anim != null &&
                    TieneParametro(
                        "isJumping",
                        AnimatorControllerParameterType.Trigger))
                {
                    anim.SetTrigger(
                        "isJumping"
                    );
                }
            }
        }
        else
        {
            // GRAVEDAD
            moveDirection.y -=
                gravity *
                Time.deltaTime;
        }

        // ======================================================
        // CHARACTER CONTROLLER
        // ======================================================

        characterController.Move(
            moveDirection *
            Time.deltaTime
        );
    }

    // ==========================================================
    // ROTACION
    // ==========================================================

    void Rotacion()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue() *
            lookSpeed *
            0.1f;

        rotationX -=
            mouseDelta.y;

        rotationX =
            Mathf.Clamp(
                rotationX,
                -lookXLimit,
                lookXLimit
            );

        // Rotación vertical de cámara
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation =
                Quaternion.Euler(
                    rotationX,
                    0,
                    0
                );
        }

        // Rotación horizontal del Player
        transform.Rotate(
            Vector3.up *
            mouseDelta.x
        );
    }

    // ==========================================================
    // ANIMACIONES DE MOVIMIENTO
    // ==========================================================

    void ActualizarAnimaciones()
    {
        if (anim == null)
            return;

        // ======================================================
        // DETERMINAR SI SE MUEVE
        // ======================================================

        bool isMoving =
            horizontal != 0 ||
            vertical != 0;

        bool grounded =
            characterController != null &&
            characterController.isGrounded;

        bool caminando =
            isMoving &&
            grounded;

        bool corriendo =
            caminando &&
            isRunning;

        // ======================================================
        // WALK
        // ======================================================

        if (TieneParametro(
            "isWalking",
            AnimatorControllerParameterType.Bool))
        {
            anim.SetBool(
                "isWalking",
                caminando
            );
        }

        // ======================================================
        // RUN
        // ======================================================

        if (TieneParametro(
            "isRunning",
            AnimatorControllerParameterType.Bool))
        {
            anim.SetBool(
                "isRunning",
                corriendo
            );
        }
    }

    // ==========================================================
    // DETENER ANIMACIONES
    // ==========================================================

    void DetenerAnimacionesMovimiento()
    {
        if (anim == null)
            return;

        if (TieneParametro(
            "isWalking",
            AnimatorControllerParameterType.Bool))
        {
            anim.SetBool(
                "isWalking",
                false
            );
        }

        if (TieneParametro(
            "isRunning",
            AnimatorControllerParameterType.Bool))
        {
            anim.SetBool(
                "isRunning",
                false
            );
        }
    }

    // ==========================================================
    // COMPROBAR PARAMETRO DEL ANIMATOR
    // ==========================================================

    bool TieneParametro(
        string nombre,
        AnimatorControllerParameterType tipo)
    {
        if (anim == null)
            return false;

        foreach (
            AnimatorControllerParameter parametro
            in anim.parameters)
        {
            if (parametro.name == nombre &&
                parametro.type == tipo)
            {
                return true;
            }
        }

        return false;
    }

    // ==========================================================
    // INFORMACION DE ESCENA
    // ==========================================================

    void ObtenerInformacionEscena()
    {
        Scene escenaActual =
            SceneManager.GetActiveScene();

        nombreEscenaActual =
            escenaActual.name;

        if (TextNombreEscena != null)
        {
            TextNombreEscena.text =
                nombreEscenaActual;

            TextNombreEscena.gameObject
                .SetActive(true);
        }
    }

    // ==========================================================
    // PUNTOS DE INTERES
    // ==========================================================

    void DetectarPuntoInteres()
    {
        puntoInteresActual = null;

        distanciaPuntoActual =
            Mathf.Infinity;

        if (puntosInteres == null ||
            puntosInteres.Count == 0)
        {
            OcultarPuntoInteres();
            return;
        }

        foreach (PuntoInteres punto in puntosInteres)
        {
            if (punto == null ||
                punto.punto == null)
            {
                continue;
            }

            Vector3 posicionPlayer =
                transform.position;

            Vector3 posicionPunto =
                punto.punto.position;

            // Ignorar altura.
            posicionPlayer.y = 0;
            posicionPunto.y = 0;

            float distancia =
                Vector3.Distance(
                    posicionPlayer,
                    posicionPunto
                );

            if (distancia <=
                distanciaPuntoInteres)
            {
                if (distancia <
                    distanciaPuntoActual)
                {
                    distanciaPuntoActual =
                        distancia;

                    puntoInteresActual =
                        punto;
                }
            }
        }

        if (puntoInteresActual != null)
        {
            MostrarPuntoInteres();
        }
        else
        {
            OcultarPuntoInteres();
        }
    }

    // ==========================================================
    // MOSTRAR PUNTO DE INTERES
    // ==========================================================

    void MostrarPuntoInteres()
    {
        if (TextPosicionEscena == null)
            return;

        TextPosicionEscena.gameObject
            .SetActive(true);

        if (mostrarDistanciaPunto)
        {
            TextPosicionEscena.text =
                puntoInteresActual.nombre +
                "\nDistancia: " +
                distanciaPuntoActual.ToString("F1") +
                " m";
        }
        else
        {
            TextPosicionEscena.text =
                puntoInteresActual.nombre;
        }
    }

    // ==========================================================
    // OCULTAR PUNTO DE INTERES
    // ==========================================================

    void OcultarPuntoInteres()
    {
        if (TextPosicionEscena != null)
        {
            TextPosicionEscena.gameObject
                .SetActive(false);
        }
    }

    // ==========================================================
    // INFORMACION DE POSICION
    // ==========================================================

    private void textUpdate2()
    {
        if (TextPositionX != null)
            TextPositionX.text =
                Mathf.RoundToInt(
                    transform.position.x
                ).ToString();

        if (TextPositionY != null)
            TextPositionY.text =
                Mathf.RoundToInt(
                    transform.position.y
                ).ToString();

        if (TextPositionZ != null)
            TextPositionZ.text =
                Mathf.RoundToInt(
                    transform.position.z
                ).ToString();

        if (vrCamera != null)
        {
            if (TextOrientacion != null)
                TextOrientacion.text =
                    Mathf.RoundToInt(
                        vrCamera.eulerAngles.y
                    ).ToString();

            if (TextAzimut != null)
                TextAzimut.text =
                    Mathf.RoundToInt(
                        (vrCamera.eulerAngles.x -
                        360f) *
                        -1f
                    ).ToString();
        }

        if (TextAltitud != null)
            TextAltitud.text =
                Mathf.RoundToInt(
                    transform.position.y
                ).ToString();
    }
}
