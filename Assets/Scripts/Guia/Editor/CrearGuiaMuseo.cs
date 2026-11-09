using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Monta el guía del museo en la escena abierta:
//   Museo > Crear guía del museo
//   Museo > Hornear NavMesh del vestíbulo
public static class CrearGuiaMuseo
{
    const string Carpeta = "Assets/Characters/Guia";
    const string RutaDialogo = Carpeta + "/DialogoGuia.asset";
    const string RutaController = Carpeta + "/AC_Guia.controller";
    const string RutaModelo = "Assets/Models/hamood.fbx";
    const string RutaReposo = "Assets/Animations/hamood@Breathing Idle.fbx";
    const string RutaCaminar = "Assets/Animations/hamood@Walking.fbx";
    const string RutaSaludo = "Assets/Animations/hamood@Joyful Jump.fbx";

    const string NombreRaiz = "Guia del Museo";
    const string NombreVestibulo = "Lobby";

    // Puesto inicial: dentro del vestíbulo, junto a la entrada, mirando hacia quien entra.
    static readonly Vector3 PosicionPuesto = new Vector3(3f, 0.15f, -9f);
    const float GiroPuesto = 180f;

    [MenuItem("Museo/Crear guía del museo")]
    static void Crear()
    {
        if (GameObject.Find(NombreRaiz) != null)
        {
            EditorUtility.DisplayDialog("Guía del museo",
                $"Ya existe \"{NombreRaiz}\" en la escena. Bórralo primero si quieres volver a crearlo.", "Vale");
            return;
        }

        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
        if (modelo == null)
        {
            EditorUtility.DisplayDialog("Guía del museo", "No encontré el modelo " + RutaModelo, "Vale");
            return;
        }

        CrearCarpeta(Carpeta);
        var dialogo = ObtenerDialogo();
        var controller = ObtenerController();
        if (controller == null) return;

        // Contenedor
        var raiz = new GameObject(NombreRaiz);
        Undo.RegisterCreatedObjectUndo(raiz, "Crear guía del museo");

        var puesto = new GameObject("Puesto");
        puesto.transform.SetParent(raiz.transform, false);
        puesto.transform.SetPositionAndRotation(PosicionPuesto, Quaternion.Euler(0f, GiroPuesto, 0f));

        // Guía (lo que se mueve)
        var guia = new GameObject(dialogo.nombre + " (guía)");
        guia.transform.SetParent(raiz.transform, false);
        guia.transform.SetPositionAndRotation(puesto.transform.position, puesto.transform.rotation);

        var agente = guia.AddComponent<NavMeshAgent>();
        agente.speed = 1.5f;
        agente.angularSpeed = 540f;
        agente.acceleration = 8f;
        agente.stoppingDistance = 0.25f;
        agente.radius = 0.35f;
        agente.height = 1.8f;
        agente.autoBraking = true;

        var capsula = guia.AddComponent<CapsuleCollider>();
        capsula.center = new Vector3(0f, 0.9f, 0f);
        capsula.height = 1.8f;
        capsula.radius = 0.3f;

        // Modelo como hijo: para cambiar de personaje basta con sustituir este objeto
        var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo, guia.transform);
        instancia.name = "Modelo";
        instancia.transform.localPosition = Vector3.zero;
        instancia.transform.localRotation = Quaternion.identity;
        var animator = instancia.GetComponent<Animator>();
        if (animator == null) animator = instancia.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        AjustarAltura(instancia.transform);

        var ui = new GameObject("UI Diálogo Guía");
        ui.transform.SetParent(raiz.transform, false);
        var componenteUI = ui.AddComponent<UIDialogoGuia>();

        var componente = guia.AddComponent<GuiaMuseo>();
        componente.dialogo = dialogo;
        componente.puesto = puesto.transform;
        componente.animator = animator;
        componente.ui = componenteUI;

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = guia;

        bool horneando = Hornear(false);
        EditorUtility.DisplayDialog("Guía del museo",
            "Guía creado en " + PosicionPuesto + ".\n\n" +
            (horneando ? "Se está horneando el NavMesh del vestíbulo.\n\n" : "") +
            "Mueve el objeto \"Puesto\" para cambiar dónde espera y guarda la escena (Ctrl+S).", "Vale");
    }

    [MenuItem("Museo/Ajustar tamaño del guía")]
    static void AjustarTamanoMenu()
    {
        var guia = Object.FindFirstObjectByType<GuiaMuseo>();
        if (guia == null)
        {
            EditorUtility.DisplayDialog("Guía del museo", "No hay ningún GuiaMuseo en la escena.", "Vale");
            return;
        }
        var modelo = guia.animator != null ? guia.animator.transform : guia.transform.Find("Modelo");
        if (modelo == null || modelo == guia.transform)
        {
            EditorUtility.DisplayDialog("Guía del museo", "No encontré el modelo hijo del guía.", "Vale");
            return;
        }
        Undo.RecordObject(modelo, "Ajustar tamaño del guía");
        float escala = AjustarAltura(modelo);
        EditorSceneManager.MarkSceneDirty(guia.gameObject.scene);
        EditorUtility.DisplayDialog("Guía del museo",
            $"Modelo ajustado a {AlturaGuia} m (escala {escala:0.###}). Guarda la escena.", "Vale");
    }

    // Escala el modelo para que mida AlturaGuia, sea cual sea la unidad en que se exportó el FBX
    // (hamood.fbx viene 10 veces más grande; el Player también lo usa a escala 0.1).
    const float AlturaGuia = 1.8f;

    static float AjustarAltura(Transform modelo)
    {
        modelo.localScale = Vector3.one;
        var renders = modelo.GetComponentsInChildren<Renderer>();
        if (renders.Length == 0) return 1f;
        Bounds b = renders[0].bounds;
        foreach (var r in renders) b.Encapsulate(r.bounds);
        float alto = b.size.y;
        float escala = alto > 0.01f ? AlturaGuia / alto : 1f;
        modelo.localScale = Vector3.one * escala;
        return escala;
    }

    [MenuItem("Museo/Hornear NavMesh del vestíbulo")]
    static void HornearMenu()
    {
        Hornear(true);
    }

    static bool Hornear(bool avisar)
    {
        var vestibulo = GameObject.Find(NombreVestibulo);
        if (vestibulo == null)
        {
            EditorUtility.DisplayDialog("Guía del museo",
                $"No encontré \"{NombreVestibulo}\" en la escena; añade un NavMeshSurface a mano y hornéalo.", "Vale");
            return false;
        }

        var superficie = vestibulo.GetComponent<NavMeshSurface>();
        if (superficie == null)
        {
            superficie = Undo.AddComponent<NavMeshSurface>(vestibulo);
            superficie.collectObjects = CollectObjects.Children;
            superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }

        NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { superficie });
        EditorSceneManager.MarkSceneDirty(vestibulo.scene);
        if (avisar)
            EditorUtility.DisplayDialog("Guía del museo", "Horneando el NavMesh del vestíbulo. Guarda la escena al terminar.", "Vale");
        return true;
    }

    static DialogoGuia ObtenerDialogo()
    {
        var dialogo = AssetDatabase.LoadAssetAtPath<DialogoGuia>(RutaDialogo);
        if (dialogo != null) return dialogo;
        dialogo = ScriptableObject.CreateInstance<DialogoGuia>();
        AssetDatabase.CreateAsset(dialogo, RutaDialogo);
        AssetDatabase.SaveAssets();
        return dialogo;
    }

    static AnimatorController ObtenerController()
    {
        var existente = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController);
        if (existente != null) return existente;

        var reposo = CargarClip(RutaReposo);
        var caminar = CargarClip(RutaCaminar);
        var saludo = CargarClip(RutaSaludo);
        if (reposo == null || caminar == null)
        {
            EditorUtility.DisplayDialog("Guía del museo", "No encontré las animaciones de reposo y caminar de hamood.", "Vale");
            return null;
        }

        var ac = AnimatorController.CreateAnimatorControllerAtPath(RutaController);
        ac.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ac.AddParameter("Saludar", AnimatorControllerParameterType.Trigger);

        var sm = ac.layers[0].stateMachine;
        var estReposo = sm.AddState("Reposo", new Vector3(300f, 0f));
        estReposo.motion = reposo;
        var estCaminar = sm.AddState("Caminar", new Vector3(300f, 120f));
        estCaminar.motion = caminar;
        sm.defaultState = estReposo;

        var t = estReposo.AddTransition(estCaminar);
        t.hasExitTime = false;
        t.duration = 0.15f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Velocidad");

        t = estCaminar.AddTransition(estReposo);
        t.hasExitTime = false;
        t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");

        if (saludo != null)
        {
            var estSaludo = sm.AddState("Saludo", new Vector3(560f, 0f));
            estSaludo.motion = saludo;

            t = estReposo.AddTransition(estSaludo);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.If, 0f, "Saludar");

            t = estSaludo.AddTransition(estReposo);
            t.hasExitTime = true;
            t.exitTime = 0.9f;
            t.duration = 0.2f;

            // Si empieza a caminar a mitad del saludo, lo interrumpe
            t = estSaludo.AddTransition(estCaminar);
            t.hasExitTime = false;
            t.duration = 0.2f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Velocidad");
        }

        AssetDatabase.SaveAssets();
        return ac;
    }

    static AnimationClip CargarClip(string ruta)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ruta)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}
