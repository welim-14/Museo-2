using UnityEngine;

public class Tp : MonoBehaviour
{
    public Transform Destino;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                // Desactivar el CharacterController es obligatorio antes de mover la posición
                cc.enabled = false;
                other.transform.position = Destino.position;
                cc.enabled = true;
            }
            else
            {
                other.transform.position = Destino.position;
            }
        }
    }
}