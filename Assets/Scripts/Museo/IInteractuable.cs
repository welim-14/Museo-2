// Objeto del museo que reacciona al puntero (centro de la pantalla o cursor del mouse).
// PunteroInteraccion busca este componente en el objeto golpeado o en sus padres.
public interface IInteractuable
{
    // Texto corto que se muestra junto a la mira mientras el puntero está encima.
    string TextoAyuda { get; }

    void AlEntrarPuntero();
    void AlSalirPuntero();
    void AlHacerClic();
}
