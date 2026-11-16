using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Museo > Crear HUD del museo
//   1. Genera los sprites del HUD (círculo, anillo, triángulo) en Assets/HUD/Museo.
//   2. Arma el prefab Assets/Prefabs/HUD/HUDMuseo.prefab, que luego se retoca a mano.
//   3. En el prefab Player borra el HUD viejo (CanvasHUD, CameraMap, CameraMirror) y pone el nuevo.
public static class CrearHUDMuseo
{
    const string CarpetaSprites = "Assets/HUD/Museo";
    const string RutaPrefab = "Assets/Prefabs/HUD/HUDMuseo.prefab";
    const string RutaPlayer = "Assets/Prefabs/Player.prefab";
    static readonly string[] ObjetosViejos = { "CanvasHUD", "CameraMap", "CameraMirror" };

    static readonly Color Fondo = new Color(0.07f, 0.075f, 0.09f, 0.78f);
    static readonly Color Dorado = new Color(0.85f, 0.71f, 0.38f, 1f);
    static readonly Color Texto = new Color(0.95f, 0.94f, 0.91f, 1f);
    static readonly Color Secundario = new Color(0.66f, 0.64f, 0.6f, 1f);

    static Sprite circulo, anillo, triangulo;

    [MenuItem("Museo/Crear HUD del museo")]
    static void Crear()
    {
        if (File.Exists(RutaPrefab) &&
            !EditorUtility.DisplayDialog("HUD del museo",
                "Ya existe HUDMuseo.prefab. Si lo vuelves a crear se pierden los retoques que le hayas hecho.",
                "Reemplazar", "Cancelar"))
            return;

        CrearCarpeta(CarpetaSprites);
        CrearCarpeta(Path.GetDirectoryName(RutaPrefab).Replace('\\', '/'));
        circulo = ObtenerSprite("Circulo", 256, DibujarCirculo);
        anillo = ObtenerSprite("Anillo", 512, DibujarAnillo);
        triangulo = ObtenerSprite("Triangulo", 64, DibujarTriangulo);

        var raiz = ConstruirHUD();
        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);

        string resultadoPlayer = ActualizarPlayer(prefab);
        EditorUtility.DisplayDialog("HUD del museo",
            "Prefab creado en " + RutaPrefab + ".\n\n" + resultadoPlayer, "Vale");
        Selection.activeObject = prefab;
    }

    // ---------------------------------------------------------------- Prefab

    static GameObject ConstruirHUD()
    {
        var raiz = new GameObject("HUDMuseo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                  typeof(CanvasGroup), typeof(HUDMuseo));
        raiz.layer = LayerMask.NameToLayer("UI");
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var escalador = raiz.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        var t = raiz.transform;

        ConstruirBrujula(t);
        ConstruirMinimapa(t);
        ConstruirSala(t);
        ConstruirInteraccion(t);
        return raiz;
    }

    static void ConstruirBrujula(Transform padre)
    {
        var brujula = Nuevo("Brujula", padre);
        Anclar(brujula, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(560f, 76f));
        var comp = brujula.gameObject.AddComponent<BrujulaHUD>();

        var ventana = Nuevo("Ventana", brujula);
        ventana.anchorMin = new Vector2(0f, 1f);
        ventana.anchorMax = new Vector2(1f, 1f);
        ventana.pivot = new Vector2(0.5f, 1f);
        ventana.sizeDelta = new Vector2(0f, 32f);
        ventana.anchoredPosition = Vector2.zero;
        var mascara = ventana.gameObject.AddComponent<RectMask2D>();
        mascara.softness = new Vector2Int(90, 0);

        var linea = Imagen("Linea", ventana, null, new Color(Dorado.r, Dorado.g, Dorado.b, 0.35f));
        linea.rectTransform.anchorMin = new Vector2(0f, 0f);
        linea.rectTransform.anchorMax = new Vector2(1f, 0f);
        linea.rectTransform.pivot = new Vector2(0.5f, 0f);
        linea.rectTransform.sizeDelta = new Vector2(0f, 1f);
        linea.rectTransform.anchoredPosition = Vector2.zero;

        var marcas = Nuevo("Marcas", ventana);
        Anclar(marcas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var indicador = Imagen("Indicador", brujula, triangulo, Dorado);
        Anclar(indicador.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(10f, 8f));

        var grados = Tmp("Grados", brujula, "000°", 15f, Texto);
        grados.characterSpacing = 4f;
        Anclar(grados.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(120f, 24f));

        comp.marcas = marcas;
        comp.textoGrados = grados;
    }

    static void ConstruirMinimapa(Transform padre)
    {
        const float tam = 230f;
        var mini = Nuevo("Minimapa", padre);
        mini.anchorMin = mini.anchorMax = Vector2.zero;
        mini.pivot = Vector2.zero;
        mini.anchoredPosition = new Vector2(40f, 40f);
        mini.sizeDelta = new Vector2(tam, tam);
        var comp = mini.gameObject.AddComponent<MinimapaHUD>();

        var fondo = Imagen("Fondo", mini, circulo, Fondo);
        Estirar(fondo.rectTransform, 0f);

        var mascara = Imagen("Mascara", mini, circulo, Color.white);
        Estirar(mascara.rectTransform, 6f);
        mascara.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        var mapaGO = Nuevo("Mapa", mascara.rectTransform);
        Estirar(mapaGO, 0f);
        var mapa = mapaGO.gameObject.AddComponent<RawImage>();
        mapa.raycastTarget = false;
        mapa.color = new Color(1f, 1f, 1f, 0.92f);

        var aro = Imagen("Anillo", mini, anillo, Dorado);
        Estirar(aro.rectTransform, 0f);

        var flecha = Imagen("Flecha", mini, triangulo, Texto);
        Anclar(flecha.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 20f));

        var norte = Nuevo("Norte", mini);
        Anclar(norte, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, tam * 0.5f), new Vector2(28f, 28f));
        var fondoNorte = Imagen("Fondo", norte, circulo, new Color(0.07f, 0.075f, 0.09f, 1f));
        Estirar(fondoNorte.rectTransform, 0f);
        var letra = Tmp("N", norte, "N", 16f, Dorado);
        letra.fontStyle = FontStyles.Bold;
        Estirar(letra.rectTransform, 0f);

        comp.mapa = mapa;
        comp.norte = norte;
        comp.radioNorte = tam * 0.5f;
    }

    static void ConstruirSala(Transform padre)
    {
        var sala = Nuevo("Sala", padre);
        sala.anchorMin = sala.anchorMax = Vector2.one;
        sala.pivot = Vector2.one;
        sala.anchoredPosition = new Vector2(-44f, -30f);
        sala.sizeDelta = new Vector2(420f, 84f);
        var grupo = sala.gameObject.AddComponent<CanvasGroup>();
        var comp = sala.gameObject.AddComponent<SalaHUD>();

        var filete = Imagen("Filete", sala, null, Dorado);
        Anclar(filete.rectTransform, Vector2.one, Vector2.one, Vector2.zero, new Vector2(120f, 2f), Vector2.one);

        var nombre = Tmp("Nombre", sala, "VESTÍBULO", 28f, Texto);
        nombre.fontStyle = FontStyles.Bold;
        nombre.characterSpacing = 6f;
        nombre.alignment = TextAlignmentOptions.TopRight;
        Anclar(nombre.rectTransform, Vector2.one, Vector2.one, new Vector2(0f, -12f), new Vector2(420f, 38f), Vector2.one);

        var sub = Tmp("Subtitulo", sala, "MUSEO VIRTUAL", 14f, Secundario);
        sub.characterSpacing = 10f;
        sub.alignment = TextAlignmentOptions.TopRight;
        Anclar(sub.rectTransform, Vector2.one, Vector2.one, new Vector2(0f, -52f), new Vector2(420f, 22f), Vector2.one);

        comp.textoNombre = nombre;
        comp.grupo = grupo;
    }

    static void ConstruirInteraccion(Transform padre)
    {
        var raiz = Nuevo("Interaccion", padre);
        Estirar(raiz, 0f);
        var comp = raiz.gameObject.AddComponent<InteraccionHUD>();

        var mira = Imagen("Mira", raiz, circulo, new Color(0.95f, 0.94f, 0.91f, 0.7f));
        Anclar(mira.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));

        var mensaje = Nuevo("Mensaje", raiz);
        Anclar(mensaje, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), Vector2.zero, new Vector2(0.5f, 0f));
        var fondo = mensaje.gameObject.AddComponent<Image>();
        fondo.color = Fondo;
        fondo.raycastTarget = false;
        var grupo = mensaje.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        var hlg = mensaje.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(26, 26, 12, 12);
        hlg.spacing = 18f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
        var ajuste = mensaje.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Filete("FileteIzq", mensaje);
        var texto = Tmp("Texto", mensaje, "Título de la obra", 24f, Texto);
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        Filete("FileteDer", mensaje);

        comp.mira = mira;
        comp.grupoMensaje = grupo;
        comp.textoMensaje = texto;
    }

    static void Filete(string nombre, Transform padre)
    {
        var img = Imagen(nombre, padre, null, Dorado);
        var le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 48f;
        le.preferredHeight = 1.5f;
    }

    // ---------------------------------------------------------------- Player

    static string ActualizarPlayer(GameObject prefabHUD)
    {
        if (!File.Exists(RutaPlayer)) return "No encontré " + RutaPlayer + "; añade el HUD a mano.";

        var raiz = PrefabUtility.LoadPrefabContents(RutaPlayer);
        try
        {
            int borrados = 0;
            foreach (var nombre in ObjetosViejos)
            {
                var hijo = raiz.transform.Find(nombre);
                if (hijo == null) continue;
                Object.DestroyImmediate(hijo.gameObject);
                borrados++;
            }

            var viejo = raiz.transform.Find(prefabHUD.name);
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefabHUD, raiz.transform);
            hud.name = prefabHUD.name;

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPlayer);
            return $"Player actualizado: {borrados} objeto(s) del HUD viejo borrados y HUDMuseo añadido.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    // ---------------------------------------------------------------- Ayudas de UI

    static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        return rt;
    }

    static Image Imagen(string nombre, Transform padre, Sprite sprite, Color color)
    {
        var rt = Nuevo(nombre, padre);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Tmp(string nombre, Transform padre, string texto, float tamano, Color color)
    {
        var rt = Nuevo(nombre, padre);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Anclar(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 tam)
    {
        Anclar(rt, min, max, pos, tam, new Vector2(0.5f, 0.5f));
    }

    static void Anclar(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 tam, Vector2 pivote)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Estirar(RectTransform rt, float margen)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    // ---------------------------------------------------------------- Sprites generados

    delegate float Forma(float x, float y, int n);

    static Sprite ObtenerSprite(string nombre, int tam, Forma forma)
    {
        string ruta = $"{CarpetaSprites}/{nombre}.png";
        if (!File.Exists(ruta))
        {
            var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            var px = new Color32[tam * tam];
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                {
                    float a = Mathf.Clamp01(forma(x + 0.5f, y + 0.5f, tam));
                    px[y * tam + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta);

            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Trilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // Cada forma devuelve la cobertura (0-1) del píxel, con 1 px de antialiasing
    static float DibujarCirculo(float x, float y, int n)
    {
        float r = n * 0.5f - 1f;
        float d = Vector2.Distance(new Vector2(x, y), new Vector2(n * 0.5f, n * 0.5f));
        return r - d + 0.5f;
    }

    static float DibujarAnillo(float x, float y, int n)
    {
        float exterior = n * 0.5f - 2f;
        float grosor = n * 0.012f;
        float d = Vector2.Distance(new Vector2(x, y), new Vector2(n * 0.5f, n * 0.5f));
        return Mathf.Min(exterior - d, d - (exterior - grosor)) + 0.5f;
    }

    static float DibujarTriangulo(float x, float y, int n)
    {
        // Apunta hacia arriba (en las texturas, y crece hacia arriba)
        Vector2 p = new Vector2(x, y);
        Vector2 a = new Vector2(n * 0.5f, n * 0.94f);
        Vector2 b = new Vector2(n * 0.06f, n * 0.06f);
        Vector2 c = new Vector2(n * 0.94f, n * 0.06f);
        float d = Mathf.Min(DistanciaLado(p, a, b), Mathf.Min(DistanciaLado(p, b, c), DistanciaLado(p, c, a)));
        return d + 0.5f;
    }

    // Distancia con signo al lado ab (positiva hacia el interior de un triángulo en sentido antihorario)
    static float DistanciaLado(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 normal = new Vector2(-ab.y, ab.x).normalized;
        return Vector2.Dot(p - a, normal);
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}
