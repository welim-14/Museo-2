using System.Collections;
using UnityEngine;

public class Tp : MonoBehaviour
{
    public Transform Destino;

    [Tooltip("Segundos entre el contacto con el portal y el teletransporte.")]
    [Min(0f)]
    public float delay = 0.5f;

    [Tooltip("Al llegar, el Player mira hacia donde apunta el eje Z del Destino.")]
    public bool orientarAlDestino = true;

    private bool teletransportando = false;

    private void OnTriggerEnter(Collider other)
    {
        if (Destino == null)
        {
            Debug.LogWarning(name + ": asigna un Destino al Tp.");
            return;
        }

        if (other.CompareTag("Player") && !teletransportando)
        {
            StartCoroutine(Teletransportar(other));
        }
    }

    private IEnumerator Teletransportar(Collider other)
    {
        teletransportando = true;

        // Congelar al Player dentro del portal mientras dura el delay
        SC_FP_Shooter7 jugador = other.GetComponent<SC_FP_Shooter7>();
        if (jugador != null)
            jugador.canMove = false;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        CharacterController cc = other.GetComponent<CharacterController>();

        // Desactivar el CharacterController es obligatorio antes de mover la posición
        if (cc != null) cc.enabled = false;
        other.transform.position = Destino.position;
        if (orientarAlDestino)
            other.transform.rotation = Quaternion.Euler(0f, Destino.eulerAngles.y, 0f);
        if (cc != null) cc.enabled = true;

        if (jugador != null)
            jugador.canMove = true;

        teletransportando = false;
    }
}
