using UnityEngine;

// Esfera que abre un enlace en el navegador al hacerle clic.
public class EsferaEnlace : EsferaInteractiva
{
    [Header("Enlace")]
    [Tooltip("Dirección completa, por ejemplo https://es.wikipedia.org")]
    public string url = "";
    public string titulo = "";

    public override string TextoAyuda =>
        string.IsNullOrEmpty(url) ? "(sin enlace)" : "Clic: " + (string.IsNullOrEmpty(titulo) ? url : titulo);

    public override void AlHacerClic()
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning(name + ": asigna una url a la EsferaEnlace.");
            return;
        }
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            Debug.LogWarning(name + ": la url debe empezar con http:// o https:// (" + url + ")");
            return;
        }

        // Liberar el cursor para que el usuario pueda usar el navegador
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Application.OpenURL(url);
    }
}
