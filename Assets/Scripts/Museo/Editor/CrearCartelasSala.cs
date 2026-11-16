using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// Museo > Colocar cartelas de sala
// Añade al prefab SalaSeccion una cartela exenta (estela oscura con filete dorado) a la derecha de la
// llegada, mirando a quien entra. Como todas las salas usan ese prefab, aparece en las cinco y cada una
// muestra su texto de Assets/Datos/TextosSalas.asset. Si ya existe, la reconstruye.
public static class CrearCartelasSala
{
    const string RutaPrefab = "Assets/Prefabs/Museo/SalaSeccion.prefab";
    const string RutaTextos = "Assets/Datos/TextosSalas.asset";
    const string RutaMaterialPanel = "Assets/Materials/Museo/Cartela_Oscuro.mat";
    const string RutaDorado = "Assets/Materials/Museo/Dorado.mat";
    const string RutaPiedra = "Assets/Materials/Museo/Piedra_Clara.mat";
    const string NombreCartela = "Cartela";

    // Posición en el espacio de la sala: a la derecha de la llegada (0, 0.3, -8.5), mirando hacia ella
    static readonly Vector3 Posicion = new Vector3(3.0f, 0f, -6.4f);
    const float Giro = 38f;

    // Medidas de la estela (metros)
    const float Ancho = 1.0f, Alto = 1.70f, Fondo = 0.12f;

    static readonly Color Crema = new Color(0.93f, 0.91f, 0.86f, 1f);
    static readonly Color Dorado = new Color(0.83f, 0.66f, 0.30f, 1f);
    static readonly Color Gris = new Color(0.68f, 0.66f, 0.62f, 1f);

    [MenuItem("Museo/Colocar cartelas de sala")]
    static void Crear()
    {
        var textos = Ejecutar();
        EditorUtility.DisplayDialog("Cartelas de sala",
            "Cartela añadida a SalaSeccion: aparece en todas las salas.\n\n" +
            "Los textos se editan en " + RutaTextos + ".", "Vale");
        Selection.activeObject = textos;
    }

    // Sin diálogos, para poder lanzarlo también desde fuera del menú
    public static TextosSalas Ejecutar()
    {
        var textos = ObtenerTextos();
        var matPanel = ObtenerMaterialPanel();
        var matDorado = AssetDatabase.LoadAssetAtPath<Material>(RutaDorado);
        var matPiedra = AssetDatabase.LoadAssetAtPath<Material>(RutaPiedra);

        var raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        try
        {
            var vieja = raiz.transform.Find(NombreCartela);
            if (vieja != null) Object.DestroyImmediate(vieja.gameObject);

            var cartela = new GameObject(NombreCartela);
            cartela.transform.SetParent(raiz.transform, false);
            cartela.transform.localPosition = Posicion;
            cartela.transform.localRotation = Quaternion.Euler(0f, Giro, 0f);
            Construir(cartela.transform, textos, matPanel, matDorado, matPiedra);

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        return textos;
    }

    static void Construir(Transform padre, TextosSalas textos, Material panel, Material dorado, Material piedra)
    {
        // Zócalo de piedra y estela. El frente (donde va el texto) mira hacia -Z local.
        Caja("Zocalo", padre, new Vector3(0f, 0.04f, 0f), new Vector3(Ancho + 0.12f, 0.08f, Fondo + 0.14f), piedra);
        Caja("Estela", padre, new Vector3(0f, 0.08f + Alto * 0.5f, 0f), new Vector3(Ancho, Alto, Fondo), panel);
        Caja("Remate", padre, new Vector3(0f, 0.08f + Alto + 0.01f, 0f), new Vector3(Ancho + 0.04f, 0.02f, Fondo + 0.04f), dorado);

        float frente = -Fondo * 0.5f - 0.004f;
        float margen = 0.08f;
        float anchoTexto = Ancho - margen * 2f;
        float x = 0f;
        float arriba = 0.08f + Alto; // canto superior de la estela

        var cTitulo = Texto("Titulo", padre, new Vector3(x, arriba - 0.18f, frente), new Vector2(anchoTexto, 0.16f), 1.0f, 0.5f, Crema);
        cTitulo.fontStyle = FontStyles.Bold;
        cTitulo.characterSpacing = 4f;
        cTitulo.alignment = TextAlignmentOptions.BottomLeft;

        var cOriginal = Texto("TituloOriginal", padre, new Vector3(x, arriba - 0.305f, frente), new Vector2(anchoTexto, 0.07f), 0.34f, 0.25f, Gris);
        cOriginal.fontStyle = FontStyles.Italic;
        cOriginal.alignment = TextAlignmentOptions.TopLeft;

        // Filete dorado bajo el título
        Caja("Filete", padre, new Vector3(-anchoTexto * 0.5f + 0.14f, arriba - 0.365f, frente + 0.002f), new Vector3(0.28f, 0.006f, 0.002f), dorado);

        var cFicha = Texto("Ficha", padre, new Vector3(x, arriba - 0.51f, frente), new Vector2(anchoTexto, 0.24f), 0.32f, 0.2f, Crema);
        cFicha.alignment = TextAlignmentOptions.TopLeft;
        cFicha.lineSpacing = 12f;

        var cTexto = Texto("Texto", padre, new Vector3(x, arriba - 1.03f, frente), new Vector2(anchoTexto, 0.74f), 0.38f, 0.2f, Crema);
        cTexto.alignment = TextAlignmentOptions.TopLeft;
        cTexto.lineSpacing = 18f;

        var comp = padre.gameObject.AddComponent<CartelaSala>();
        comp.textos = textos;
        comp.titulo = cTitulo;
        comp.tituloOriginal = cOriginal;
        comp.ficha = cFicha;
        comp.texto = cTexto;
        comp.Aplicar();
    }

    static void Caja(string nombre, Transform padre, Vector3 centro, Vector3 tam, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = centro;
        go.transform.localScale = tam;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        // Sin "Contribute GI": así se ilumina con las luces y sondas de la sala sin tener que rehornear
    }

    static TextMeshPro Texto(string nombre, Transform padre, Vector3 centro, Vector2 tam, float tamMax, float tamMin, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(padre, false);
        var tmp = go.GetComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.rectTransform.sizeDelta = tam;
        tmp.rectTransform.localPosition = centro;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = tamMax;
        tmp.fontSizeMin = tamMin;
        tmp.fontSize = tamMax;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.richText = true;
        return tmp;
    }

    static TextosSalas ObtenerTextos()
    {
        var t = AssetDatabase.LoadAssetAtPath<TextosSalas>(RutaTextos);
        if (t != null) return t;
        CrearCarpeta(Path.GetDirectoryName(RutaTextos).Replace('\\', '/'));
        t = ScriptableObject.CreateInstance<TextosSalas>();
        AssetDatabase.CreateAsset(t, RutaTextos);
        AssetDatabase.SaveAssets();
        return t;
    }

    static Material ObtenerMaterialPanel()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialPanel);
        if (m != null) return m;
        m = new Material(Shader.Find("Standard")) { name = "Cartela_Oscuro" };
        m.color = new Color(0.085f, 0.09f, 0.105f, 1f);
        m.SetFloat("_Glossiness", 0.35f);
        m.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(m, RutaMaterialPanel);
        return m;
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}
