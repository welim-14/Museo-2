using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Guía del museo. Espera en su puesto del vestíbulo, saluda al visitante que se acerca,
// conversa con él (UIDialogoGuia) y, si se lo pide, lo acompaña con NavMesh hasta el portal
// de una sección. El guía nunca cruza los portales: al llegar se aparta, presenta la sala
// y vuelve a su puesto.
//
// Estados:
//   EnPuesto    -> el visitante se acerca               -> Atento
//   Atento      -> E: Conversando | se aleja o pasa el tiempo -> EnPuesto / Volviendo
//   Conversando -> elige sección: Guiando | termina o Esc: Atento
//   Guiando     -> llega frente al portal               -> EnPortal (si el visitante se queda atrás, lo espera)
//   EnPortal    -> pasa el tiempo o el visitante cruza   -> Volviendo
//   Volviendo   -> llega al puesto                      -> EnPuesto (con E se le puede hablar por el camino)
//
// El modelo 3D va como hijo con su propio Animator (parámetros "Velocidad" y "Saludar"),
// así que cambiar de personaje es sustituir ese hijo.
[RequireComponent(typeof(NavMeshAgent))]
public class GuiaMuseo : MonoBehaviour
{
    public enum Estado { EnPuesto, Atento, Conversando, Guiando, EnPortal, Volviendo }

    [Header("Datos")]
    public DialogoGuia dialogo;

    [Header("Referencias")]
    [Tooltip("Dónde espera el guía y hacia dónde mira. Si está vacío, usa su posición inicial.")]
    public Transform puesto;
    public Animator animator;
    [Tooltip("Si está vacío, se crea uno al arrancar.")]
    public UIDialogoGuia ui;
    [Tooltip("Si está vacío, se crea uno sobre la cabeza del modelo al arrancar.")]
    public LetreroGuia letrero;

    [Header("Distancias (m)")]
    public float distanciaAtencion = 4f;
    [Tooltip("Si el visitante se queda más lejos que esto mientras lo guía, el guía se detiene a esperarlo.")]
    public float distanciaEspera = 6f;
    public float distanciaReanudar = 3.5f;
    [Tooltip("Más allá de esto se considera que el visitante se fue (por ejemplo, cruzó un portal).")]
    public float distanciaAbandono = 20f;
    public float distanciaFrentePortal = 2.2f;
    public float desplazamientoLateral = 1.4f;

    [Header("Tiempos (s)")]
    public float esperaEnPortal = 10f;
    public float duracionAviso = 6f;
    [Tooltip("Tiempo que se queda atento lejos de su puesto antes de regresar.")]
    public float esperaFueraDePuesto = 8f;
    public float pausaEntreSaludos = 30f;

    [Header("Comportamiento")]
    public bool saludarAlAcercarse = true;
    public float velocidadGiro = 300f;

    static readonly int idVelocidad = Animator.StringToHash("Velocidad");
    static readonly int idSaludar = Animator.StringToHash("Saludar");

    NavMeshAgent agente;
    Transform jugador;
    SC_FP_Shooter7 controlJugador;
    Vector3 posPuesto;
    Quaternion rotPuesto;
    readonly List<Transform> portales = new List<Transform>();

    Estado estado = Estado.EnPuesto;
    int seccionDestino = -1;
    float temporizador;
    float ultimoSaludo = -999f;
    bool yaSePresento;
    bool esperandoVisitante;
    bool avisoNavMeshDado;

    public Estado EstadoActual => estado;

    // ---------------------------------------------------------------- Arranque

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    void Start()
    {
        if (dialogo == null)
        {
            Debug.LogError(name + ": asigna un DialogoGuia al GuiaMuseo.", this);
            enabled = false;
            return;
        }

        posPuesto = puesto != null ? puesto.position : transform.position;
        rotPuesto = Quaternion.Euler(0f, (puesto != null ? puesto.eulerAngles.y : transform.eulerAngles.y), 0f);

        if (NavMesh.SamplePosition(posPuesto, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            posPuesto = hit.position;
            agente.Warp(posPuesto);
        }
        else
        {
            AvisarSinNavMesh();
        }
        transform.rotation = rotPuesto;

        if (ui == null)
        {
            var go = new GameObject("UI Diálogo Guía");
            ui = go.AddComponent<UIDialogoGuia>();
        }
        if (letrero == null) CrearLetrero();
        letrero.PonerNombre(dialogo.nombre);

        foreach (var seccion in dialogo.secciones)
        {
            var portal = string.IsNullOrEmpty(seccion.nombrePortal) ? null : GameObject.Find(seccion.nombrePortal);
            if (portal == null)
                Debug.LogWarning($"{name}: no encontré el portal \"{seccion.nombrePortal}\" de la sección {seccion.nombre}.", this);
            portales.Add(portal != null ? portal.transform : null);
        }

        BuscarJugador();
    }

    void BuscarJugador()
    {
        var go = GameObject.FindWithTag("Player");
        if (go == null) return;
        jugador = go.transform;
        controlJugador = go.GetComponent<SC_FP_Shooter7>();
    }

    void CrearLetrero()
    {
        var go = new GameObject("Letrero", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, AlturaModelo() + 0.35f, 0f);
        letrero = go.AddComponent<LetreroGuia>();
    }

    float AlturaModelo()
    {
        var renders = GetComponentsInChildren<Renderer>();
        if (renders.Length == 0) return agente.height;
        Bounds b = renders[0].bounds;
        foreach (var r in renders)
            if (!(r is ParticleSystemRenderer) && r.gameObject != gameObject) b.Encapsulate(r.bounds);
        return Mathf.Max(0.5f, b.max.y - transform.position.y);
    }

    // ---------------------------------------------------------------- Ciclo

    void Update()
    {
        if (jugador == null) BuscarJugador();
        float distancia = DistanciaAlJugador();
        bool pulsoE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        switch (estado)
        {
            case Estado.EnPuesto:
                GirarHacia(rotPuesto);
                if (distancia < distanciaAtencion) EntrarAtento(true);
                break;

            case Estado.Atento:
                MirarAlJugador();
                if (pulsoE && distancia < distanciaAtencion + 1f)
                {
                    AbrirConversacion();
                    break;
                }
                bool enPuesto = EstaEnPuesto();
                if (!enPuesto) temporizador -= Time.deltaTime;
                if (distancia > distanciaAtencion + 1.5f || (!enPuesto && temporizador <= 0f))
                {
                    if (enPuesto) EntrarEnPuesto();
                    else EntrarVolviendo();
                }
                break;

            case Estado.Conversando:
                MirarAlJugador();
                break;

            case Estado.Guiando:
                ActualizarGuia(distancia);
                break;

            case Estado.EnPortal:
                MirarAlJugador();
                if (pulsoE && distancia < distanciaAtencion)
                {
                    ui.Ocultar();
                    AbrirConversacion();
                    break;
                }
                temporizador -= Time.deltaTime;
                if (temporizador <= 0f || distancia > distanciaAbandono) EntrarVolviendo();
                break;

            case Estado.Volviendo:
                bool cerca = distancia < distanciaAtencion;
                if (cerca) letrero.Mostrar(dialogo.avisoHablar);
                else letrero.Ocultar();
                if (cerca && pulsoE)
                {
                    Detener();
                    AbrirConversacion();
                    break;
                }
                if (Llego()) EntrarEnPuesto();
                break;
        }

        if (animator != null)
            animator.SetFloat(idVelocidad, agente.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    void ActualizarGuia(float distancia)
    {
        if (!esperandoVisitante && distancia > distanciaEspera)
        {
            esperandoVisitante = true;
            agente.isStopped = true;
            agente.updateRotation = false;
            letrero.Mostrar(dialogo.avisoEspera);
        }
        else if (esperandoVisitante && distancia < distanciaReanudar)
        {
            esperandoVisitante = false;
            agente.isStopped = false;
            agente.updateRotation = true;
            letrero.Mostrar(dialogo.avisoSigame);
        }

        if (esperandoVisitante)
        {
            MirarAlJugador();
            if (distancia > distanciaAbandono) EntrarVolviendo();
            return;
        }

        if (Llego()) EntrarEnPortal();
    }

    // ---------------------------------------------------------------- Transiciones

    void EntrarEnPuesto()
    {
        estado = Estado.EnPuesto;
        Detener();
        letrero.Ocultar();
    }

    void EntrarAtento(bool saludar)
    {
        estado = Estado.Atento;
        Detener();
        temporizador = esperaFueraDePuesto;
        letrero.Mostrar(dialogo.avisoHablar);

        if (saludar && saludarAlAcercarse && animator != null && Time.time - ultimoSaludo > pausaEntreSaludos)
        {
            ultimoSaludo = Time.time;
            animator.SetTrigger(idSaludar);
        }
    }

    void EntrarVolviendo()
    {
        estado = Estado.Volviendo;
        esperandoVisitante = false;
        letrero.Ocultar();
        if (!IrA(posPuesto)) EntrarEnPuesto();
    }

    void IniciarGuia(int indice)
    {
        Transform portal = indice >= 0 && indice < portales.Count ? portales[indice] : null;
        if (portal == null || !agente.isOnNavMesh)
        {
            if (!agente.isOnNavMesh) AvisarSinNavMesh();
            EntrarAtento(false);
            return;
        }

        Vector3 frente = Vector3.ProjectOnPlane(portal.forward, Vector3.up).normalized;
        Vector3 lado = Vector3.Cross(Vector3.up, frente);
        Vector3 punto = portal.position - frente * distanciaFrentePortal + lado * desplazamientoLateral;

        seccionDestino = indice;
        esperandoVisitante = false;
        estado = Estado.Guiando;
        letrero.Mostrar(dialogo.avisoSigame);
        if (!IrA(punto)) EntrarEnPortal();
    }

    void EntrarEnPortal()
    {
        estado = Estado.EnPortal;
        Detener();
        letrero.Ocultar();
        temporizador = esperaEnPortal;

        if (seccionDestino >= 0 && seccionDestino < dialogo.secciones.Count)
            ui.MostrarAviso(dialogo.nombre, dialogo.titulo, dialogo.secciones[seccionDestino].lineaLlegada, duracionAviso);
    }

    // ---------------------------------------------------------------- Conversación

    void AbrirConversacion()
    {
        estado = Estado.Conversando;
        Detener();
        letrero.Ocultar();
        CongelarJugador(true);

        var lineas = new List<string>();
        if (yaSePresento) lineas.Add(dialogo.reencuentro);
        else lineas.AddRange(dialogo.bienvenida);
        yaSePresento = true;

        MostrarMenu(lineas);
    }

    void MostrarMenu(List<string> lineas)
    {
        var opciones = new[] { dialogo.opcionSecciones, dialogo.opcionMuseo, dialogo.opcionNada };
        ui.Mostrar(dialogo.nombre, dialogo.titulo, lineas, opciones, null, ElegirMenu, CerrarConversacion);
    }

    void ElegirMenu(int opcion)
    {
        switch (opcion)
        {
            case 0:
                MostrarSecciones();
                break;
            case 1:
                MostrarMenu(new List<string>(dialogo.infoMuseo));
                break;
            default:
                ui.Mostrar(dialogo.nombre, dialogo.titulo, new[] { dialogo.despedida }, null, null,
                           _ => CerrarConversacion(), CerrarConversacion);
                break;
        }
    }

    void MostrarSecciones()
    {
        var nombres = new List<string>();
        foreach (var s in dialogo.secciones) nombres.Add(s.nombre);

        ui.Mostrar(dialogo.nombre, dialogo.titulo, new[] { dialogo.elegirSeccion }, nombres, dialogo.opcionVolver,
                   indice =>
                   {
                       if (indice < 0)
                       {
                           MostrarMenu(new List<string> { dialogo.reencuentro });
                           return;
                       }
                       string frase = string.Format(dialogo.acompanar, dialogo.secciones[indice].nombre);
                       ui.Mostrar(dialogo.nombre, dialogo.titulo, new[] { frase }, null, null,
                                  _ => { TerminarConversacion(); IniciarGuia(indice); },
                                  () => { TerminarConversacion(); IniciarGuia(indice); });
                   },
                   CerrarConversacion);
    }

    // Cierra la caja y devuelve el control sin cambiar de estado
    void TerminarConversacion()
    {
        ui.Ocultar();
        CongelarJugador(false);
    }

    void CerrarConversacion()
    {
        TerminarConversacion();
        EntrarAtento(false);
    }

    void CongelarJugador(bool congelar)
    {
        if (controlJugador != null) controlJugador.canMove = !congelar;
    }

    void OnDisable()
    {
        if (estado == Estado.Conversando) CongelarJugador(false);
    }

    // ---------------------------------------------------------------- Movimiento

    bool IrA(Vector3 destino)
    {
        if (!agente.isOnNavMesh)
        {
            AvisarSinNavMesh();
            return false;
        }
        if (NavMesh.SamplePosition(destino, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            destino = hit.position;
        agente.isStopped = false;
        agente.updateRotation = true;
        return agente.SetDestination(destino);
    }

    void Detener()
    {
        if (!agente.isOnNavMesh) return;
        agente.isStopped = true;
        agente.ResetPath();
        agente.updateRotation = false;
    }

    bool Llego()
    {
        if (!agente.isOnNavMesh) return true;
        if (agente.pathPending) return false;
        return agente.remainingDistance <= agente.stoppingDistance + 0.05f;
    }

    bool EstaEnPuesto()
    {
        Vector3 d = transform.position - posPuesto;
        d.y = 0f;
        return d.sqrMagnitude < 0.6f * 0.6f;
    }

    float DistanciaAlJugador()
    {
        if (jugador == null) return float.PositiveInfinity;
        Vector3 d = jugador.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    void MirarAlJugador()
    {
        if (jugador == null) return;
        Vector3 d = jugador.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f) GirarHacia(Quaternion.LookRotation(d));
    }

    void GirarHacia(Quaternion objetivo)
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo, velocidadGiro * Time.deltaTime);
    }

    void AvisarSinNavMesh()
    {
        if (avisoNavMeshDado) return;
        avisoNavMeshDado = true;
        Debug.LogWarning(name + ": no hay NavMesh bajo el guía. Hornéalo con Museo > Hornear NavMesh del vestíbulo.", this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.85f, 0.71f, 0.38f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, distanciaAtencion);
        Gizmos.color = new Color(0.9f, 0.4f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, distanciaEspera);
    }
}
