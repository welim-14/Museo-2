using UnityEngine;

public class ControlPanelText : MonoBehaviour
{
    public GameObject panelTexto;

    void Start()
    {
        if (panelTexto != null)
            panelTexto.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (panelTexto != null && other.CompareTag("Player"))
            panelTexto.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (panelTexto != null && other.CompareTag("Player"))
            panelTexto.SetActive(false);
    }
}
