using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SC_FP_Shooter7 : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkingSpeed = 7.5f;       
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float AlturaPersonaje = 2f;

    [Header("Camara")]
    public Camera playerCamera;
    public Transform vrCamera; // IMPORTANTE
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    [Header("UI")]
    public Text TextScore;
    public Text TextVidas;
    public Text TextPociones;
    public Text TextMonedas;
    public Text TextMuertes;
    public Text TextArmo;
    public Text TextArsenal;
    public Text TextPositionX;
    public Text TextPositionY;
    public Text TextPositionZ;
    public Text TextOrientacion;
    public Text TextAzimut;
    public Text TextAltitud;
    public Text NombreUsuario;
    public Text textoObjetosDestruidos;

    public Text TextStatusBar;

    // 🔥 NUEVO SISTEMA STATUS BAR
    [Header("Status Bar Config")]
    public float tiempoMensaje = 2.5f;
    private Queue<string> colaMensajes = new Queue<string>();
    private bool mostrandoMensaje = false;

    [Header("Combate")]
    public GameObject balaPrefab;
    public Transform lanzador;
    public float VelDisparo = 10f;
    public float tiempoDisparo = 0.3f;
    public float lifetime = 5f;

    [Header("Gameplay")]
    public GameObject avatar;
    public float Xposition;
    public float Yposition;
    public float Zposition;
    public string sceneName;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector] public Animator anim;

    float inicioDisparar;

    int contadorArmo = 0;
    int contadorScore = 0;
    int contadorVidas = 3;
    int contadorMonedas = 0;
    int contadorPociones = 0;
    int contadorMuertes = 0;

    int countBullet = 0;

    public int Arsenal = 1000;
    public bool canMove = true;

    float horizontal;
    float vertical;
    bool isRunning;

    public static SC_FP_Shooter7 instancia;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();

        characterController.height = AlturaPersonaje;
        characterController.center = new Vector3(0, AlturaPersonaje / 2, 0);

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    void Update()
    {
        if (!canMove) return;

        ProcesarInputs();
        MovimientoYFisicas();
        Rotacion();
        Disparo();
        ActualizarAnimaciones();

        textUpdate();
        textUpdate2();
    }

    void ProcesarInputs()
    {
        if (Keyboard.current == null) return;

        horizontal = 0;
        vertical = 0;

        if (Keyboard.current.aKey.isPressed) horizontal = -1;
        if (Keyboard.current.dKey.isPressed) horizontal = 1;
        if (Keyboard.current.wKey.isPressed) vertical = 1;
        if (Keyboard.current.sKey.isPressed) vertical = -1;

        isRunning = Keyboard.current.leftShiftKey.isPressed;
    }

    void MovimientoYFisicas()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = (isRunning ? runningSpeed : walkingSpeed) * vertical;
        float curSpeedY = (isRunning ? runningSpeed : walkingSpeed) * horizontal;

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (characterController.isGrounded)
        {
            moveDirection.y = -0.5f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                moveDirection.y = jumpSpeed;
                anim.SetTrigger("isJumping");
                MostrarMensaje("🦘 Salto realizado");
            }
        }
        else
        {
            moveDirection.y = movementDirectionY - (gravity * Time.deltaTime);
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    void Rotacion()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * lookSpeed * 0.1f;

        rotationX -= mouseDelta.y;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    void Disparo()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.isPressed && Time.time > inicioDisparar && Arsenal > 0)
        {
            inicioDisparar = Time.time + tiempoDisparo;

            Arsenal--;
            contadorArmo++;

            GameObject bala = Instantiate(balaPrefab, lanzador.position, lanzador.rotation);
            Rigidbody rb = bala.GetComponent<Rigidbody>();

            if (rb != null)
                rb.AddForce(lanzador.forward * 100 * VelDisparo);

            bala.name = "Bala " + countBullet++;
            bala.AddComponent<Proyectil>();

            Destroy(bala, lifetime);

            MostrarMensaje("🔫 Disparo realizado");
        }

        if (Arsenal <= 0)
        {
            MostrarMensaje("⚠ Sin munición");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TeleportTrigger"))
        {
            MostrarMensaje("🚪 Cambiando de zona...");
            SceneManager.LoadScene(sceneName);
        }

        if (other.CompareTag("Teletrans"))
        {
            avatar.transform.position = new Vector3(Xposition, Yposition, Zposition);
            MostrarMensaje("🌀 Teletransportado");
        }

        if (other.CompareTag("ItemVidas"))
        {
            Destroy(other.gameObject);
            contadorVidas++;
            if (TextVidas != null) TextVidas.text = contadorVidas.ToString();
            contadorScore += 10;
            MostrarMensaje("❤️ Vida obtenida");
        }

        if (other.CompareTag("ItemMonedas"))
        {
            Destroy(other.gameObject);
            contadorMonedas++;
            if (TextMonedas != null) TextMonedas.text = contadorMonedas.ToString();
            contadorScore += 10;
            MostrarMensaje("🪙 Moneda recogida");
        }

        if (other.CompareTag("ItemPociones"))
        {
            Destroy(other.gameObject);
            contadorPociones++;
            if (TextPociones != null) TextPociones.text = contadorPociones.ToString();
            contadorScore += 25;
            MostrarMensaje("🧪 Poción recogida");
        }

        if (other.CompareTag("ItemMuertes"))
        {
            Destroy(other.gameObject);
            contadorMuertes++;
            if (TextMuertes != null) TextMuertes.text = contadorMuertes.ToString();
            contadorScore -= 10;
            MostrarMensaje("☠ Objeto peligroso");
        }

        if (other.CompareTag("NPC"))
        {
            MostrarMensaje("🧑 NPC: Hola viajero!");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ItemObstaculo"))
        {
            IncrementarObjetosDestruidos();
            Destroy(collision.gameObject);

            MostrarMensaje("💥 Obstáculo destruido +50");
        }
    }

    void ActualizarAnimaciones()
    {
        bool isMoving = (horizontal != 0 || vertical != 0) && characterController.isGrounded;

        if (anim != null)
        {
            anim.SetBool("isWalking", isMoving);
            anim.SetBool("isRunning", isMoving && isRunning);

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                foreach (var param in anim.parameters)
                {
                    if (param.name == "isAttack")
                    {
                        anim.SetTrigger("isAttack");
                        MostrarMensaje("⚔ Ataque");
                        break;
                    }
                }
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                foreach (var param in anim.parameters)
                {
                    if (param.name == "isTrouwing")
                    {
                        anim.SetTrigger("isTrouwing");
                        break;
                    }
                }
            }
        }
    }

    private void textUpdate()
    {
        if (TextArmo != null)
            TextArmo.text = contadorArmo.ToString();

        if (TextArsenal != null)
            TextArsenal.text = Arsenal.ToString();

        if (TextScore != null)
            TextScore.text = contadorScore.ToString();
    }

    private void textUpdate2()
    {
        if (TextPositionX != null)
            TextPositionX.text = Mathf.RoundToInt(transform.position.x).ToString();

        if (TextPositionY != null)
            TextPositionY.text = Mathf.RoundToInt(transform.position.y).ToString();

        if (TextPositionZ != null)
            TextPositionZ.text = Mathf.RoundToInt(transform.position.z).ToString();

        if (vrCamera != null)
        {
            if (TextOrientacion != null)
                TextOrientacion.text = Mathf.RoundToInt(vrCamera.eulerAngles.y).ToString();

            if (TextAzimut != null)
                TextAzimut.text = Mathf.RoundToInt((vrCamera.eulerAngles.x - 360f) * -1f).ToString();
        }

        if (TextAltitud != null)
            TextAltitud.text = Mathf.RoundToInt(transform.position.y - 1f).ToString();
    }

    // ================= STATUS BAR =================

    public void MostrarMensaje(string mensaje)
    {
        colaMensajes.Enqueue(mensaje);

        if (!mostrandoMensaje)
            StartCoroutine(MostrarMensajes());
    }

    IEnumerator MostrarMensajes()
    {
        mostrandoMensaje = true;

        while (colaMensajes.Count > 0)
        {
            string mensajeActual = colaMensajes.Dequeue();

            if (TextStatusBar != null)
                TextStatusBar.text = mensajeActual;

            yield return new WaitForSeconds(tiempoMensaje);
        }

        if (TextStatusBar != null)
            TextStatusBar.text = "";

        mostrandoMensaje = false;
    }

    public void IncrementarObjetosDestruidos()
    {
        contadorScore += 50;

        if (textoObjetosDestruidos != null)
            textoObjetosDestruidos.text = Proyectil.objetosDestruidos.ToString();

        if (TextScore != null)
            TextScore.text = contadorScore.ToString();
    }
}