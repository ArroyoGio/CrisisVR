using System;
using System.Linq;
using CrisisVR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;

namespace CrisisVR.Editor
{
    public static class Act1SceneSetup
    {
        const string ScenePath = "Assets/Scenes/Act1.unity";
        const string RigPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        const string SchoolLaptopPrefabPath = "Assets/school/Prefabs/props/a laptop.prefab";
        const string SchoolChairPrefabPath = "Assets/school/Prefabs/props/chair.prefab";
        const string SchoolTablePrefabPath = "Assets/school/Prefabs/props/table3.prefab";
        const string RootName = "Act1Prototype";
        static readonly Vector3 StudentDeskPosition = new Vector3(15.5f, 0.77f, -67.9f);

        [MenuItem("CrisisVR/Act 1/Preparar aula y simulador PC")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de preparar Act 1.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Abre Assets/Scenes/Act1.unity antes de preparar el acto.");
            var xrManager = UnityEngine.Object.FindAnyObjectByType<XRInteractionManager>();
            if (xrManager == null) throw new InvalidOperationException("Act1 requiere XR Interaction Manager en la escena.");

            var root = GameObject.Find(RootName);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            root = new GameObject(RootName);

            var materials = CreateMaterials();
            var studentDesk = GameObject.Find("Aula 2 mesa 0-2");
            if (studentDesk == null) throw new InvalidOperationException("Falta el escritorio existente Aula 2 mesa 0-2.");
            DisableSceneObject("Puesto laboratorio 08 - pantalla");
            DisableSceneObject("Puesto laboratorio 08 - soporte");
            DisableSceneObject("Puesto laboratorio 08 - teclado");
            DisableSceneObject("Aula 2 asiento 0-2");
            DisableSceneObject("Aula 2 respaldo 0-2");
            MakeLaptop(root.transform, studentDesk.transform, materials);
            PlaceSchoolPrefab(root.transform, "School table profesor", SchoolTablePrefabPath, Vector3.one * .75f,
                new Vector3(16.1f, 0, -70.55f), Quaternion.identity, 0);
            PlaceSchoolPrefab(root.transform, "School chair estudiante", SchoolChairPrefabPath, Vector3.one,
                new Vector3(15.5f, 0, -66.90f), Quaternion.Euler(0, 180, 0), 0);
            MakeTeacher(root.transform, new Vector3(16.1f, 0, -69.25f), materials);
            MakeGradeCard(root.transform, new Vector3(15.005f, .835f, -67.9f), materials);
            AddPlayerAndSimulator();
            var flow = root.AddComponent<Act1Flow>();
            ConfigureFlow(flow, root);
            YarnDialogueSetup.ConfigureScene();
            ConfigurePresentation();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Act1 preparada con prefabs School, colocación por renderer bounds y XR Interaction Simulator para PC.");
        }

        public static void PrepareBatch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Prepare();
        }

        [MenuItem("CrisisVR/Act 1/Configurar presentación y transiciones")]
        public static void ConfigurePresentation()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Abre Assets/Scenes/Act1.unity antes de configurar Act 1.");

            var teacher = GameObject.Find("Profesor Miguel (placeholder visual)");
            if (teacher == null) throw new InvalidOperationException("No se encontró el profesor de Act 1.");
            if (teacher.GetComponent<Act1ProfessorMotion>() == null)
                Undo.AddComponent<Act1ProfessorMotion>(teacher);

            var camera = GameObject.Find("Main Camera");
            var flow = UnityEngine.Object.FindAnyObjectByType<Act1Flow>();
            if (camera == null || flow == null) throw new InvalidOperationException("Falta la cámara XR o el flujo de Act 1.");
            var cardObject = camera.transform.Find("Act1 Transition Card")?.gameObject;
            if (cardObject == null) cardObject = CreateTransitionCard(camera.transform);
            var canvasGroup = cardObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Act 1: animación de Miguel y transiciones con fundido configuradas.");
        }

        static GameObject CreateTransitionCard(Transform camera)
        {
            var card = new GameObject("Act1 Transition Card", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(Act1TransitionCard));
            Undo.RegisterCreatedObjectUndo(card, "Crear tarjeta de transición de Act1");
            card.transform.SetParent(camera, false);
            card.transform.localPosition = new Vector3(0f, 0f, 1.25f);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localScale = Vector3.one * .0008f;

            var rect = (RectTransform)card.transform;
            rect.sizeDelta = new Vector2(1920f, 1080f);
            var canvas = card.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera.GetComponent<Camera>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            var scaler = card.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(card.transform, false);
            var backgroundRect = (RectTransform)backgroundObject.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var background = backgroundObject.GetComponent<Image>();
            background.color = new Color(.055f, .075f, .09f, 1f);
            background.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(card.transform, false);
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = new Vector2(.08f, .35f);
            labelRect.anchorMax = new Vector2(.92f, .65f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 72f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var transition = card.GetComponent<Act1TransitionCard>();
            var canvasGroup = card.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            var so = new SerializedObject(transition);
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("background").objectReferenceValue = background;
            so.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        static (Material wood, Material darkWood, Material charcoal, Material screen, Material paper, Material green) CreateMaterials()
        {
            return (Mat("Act1 roble", new Color(.39f, .24f, .13f)), Mat("Act1 nogal", new Color(.19f, .13f, .1f)),
                Mat("Act1 carbón", new Color(.055f, .065f, .075f)), Mat("Act1 pantalla", new Color(.12f, .2f, .25f)),
                Mat("Act1 papel", new Color(.94f, .93f, .87f)), Mat("Act1 verde botón", new Color(.17f, .48f, .34f)));
        }

        static Material Mat(string name, Color color)
        {
            const string folder = "Assets/_Project/Materials/Act1";
            var path = folder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = name, color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void DisableSceneObject(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) existing.SetActive(false);
        }

        static void MakeLaptop(Transform parent, Transform desk, (Material wood, Material darkWood, Material charcoal, Material screen, Material paper, Material green) mats)
        {
            PlaceSchoolPrefab(parent, "School laptop estudiante", SchoolLaptopPrefabPath, Vector3.one * .7f,
                new Vector3(desk.position.x, 0, desk.position.z - .10f), Quaternion.identity, DeskTop(desk) + .002f);
            var top = DeskTop(desk);
            MakeButton(parent, "ENTREGAR", "ENTREGAR", new Vector3(desk.position.x - .1325f, top + .0245f, desk.position.z + .215f), mats.green, .245f);
            MakeButton(parent, "CORREGIR PROYECTO", "CORREGIR PROYECTO", new Vector3(desk.position.x + .1325f, top + .0245f, desk.position.z + .215f), mats.green, .245f).SetActive(false);
        }

        static float DeskTop(Transform desk)
        {
            var renderers = desk.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("El escritorio del estudiante no tiene renderers.");
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.max.y;
        }

        static GameObject MakeButton(Transform parent, string name, string label, Vector3 position, Material mat, float width)
        {
            var go = Cube(parent, name, new Vector3(width, .045f, .12f), position, mat);
            go.AddComponent<XRSimpleInteractable>();
            var text = Text(go.transform, label, new Vector3(0, .5f, 0), .042f, Color.white);
            text.alignment = TextAlignmentOptions.Center;
            text.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            return go;
        }

        static GameObject PlaceSchoolPrefab(Transform parent, string name, string prefabPath, Vector3 scale, Vector3 center, Quaternion rotation, float bottomY)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new InvalidOperationException("No se encontró el prefab School: " + prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = scale;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("El prefab no tiene renderers: " + prefabPath);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            instance.transform.position = new Vector3(center.x - bounds.center.x, bottomY - bounds.min.y, center.z - bounds.center.z);
            return instance;
        }

        static void MakeTeacher(Transform parent, Vector3 basePosition, (Material wood, Material darkWood, Material charcoal, Material screen, Material paper, Material green) mats)
        {
            var actor = new GameObject("Profesor Miguel (placeholder visual)");
            actor.transform.SetParent(parent, false);
            actor.AddComponent<Act1ProfessorMotion>();
            actor.transform.position = basePosition;
            actor.transform.rotation = Quaternion.LookRotation(new Vector3(15.5f - basePosition.x, 0, -66.2f - basePosition.z));
            Capsule(actor.transform, "Cuerpo", new Vector3(0, .82f, 0), new Vector3(.55f, .82f, .38f), new Color(.22f, .3f, .43f));
            Sphere(actor.transform, "Cabeza", new Vector3(0, 1.70f, 0), new Vector3(.34f, .38f, .34f), new Color(.7f, .5f, .39f));
        }

        static void MakeGradeCard(Transform parent, Vector3 position, (Material wood, Material darkWood, Material charcoal, Material screen, Material paper, Material green) mats)
        {
            var card = Cube(parent, "grade_card", new Vector3(.46f, .03f, .35f), position, mats.paper);
            var title = Text(card.transform, "CALIFICACIÓN\n08 / 20", new Vector3(0, .5f, 0), .09f, new Color(.16f, .13f, .11f));
            title.alignment = TextAlignmentOptions.Center;
            title.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            card.SetActive(false);
        }

        static void ConfigureFlow(Act1Flow flow, GameObject root)
        {
            var so = new SerializedObject(flow);
            var buttons = root.GetComponentsInChildren<XRSimpleInteractable>(true);
            so.FindProperty("deliverInteractable").objectReferenceValue = buttons.Single(b => b.gameObject.name == "ENTREGAR");
            so.FindProperty("correctInteractable").objectReferenceValue = buttons.Single(b => b.gameObject.name == "CORREGIR PROYECTO");
            so.FindProperty("deliverButton").objectReferenceValue = buttons.Single(b => b.gameObject.name == "ENTREGAR").gameObject;
            so.FindProperty("correctButton").objectReferenceValue = buttons.Single(b => b.gameObject.name == "CORREGIR PROYECTO").gameObject;
            so.FindProperty("gradeCard").objectReferenceValue = root.transform.Find("grade_card").gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddPlayerAndSimulator()
        {
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            var simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (rigPrefab == null || simulatorPrefab == null) throw new InvalidOperationException("No se encontraron los prefabs del XR rig y simulador incluidos en XRI 3.5.1.");
            foreach (var camera in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>(true)))
                if (camera.gameObject.name == "Main Camera" && camera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() == null)
                    camera.gameObject.SetActive(false);
            var existingRig = UnityEngine.Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            var rig = existingRig != null ? existingRig.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, SceneManager.GetActiveScene());
            rig.name = "Act1 XR Origin";
            rig.transform.SetPositionAndRotation(new Vector3(15.5f, .02f, -66.2f), Quaternion.Euler(0, 180, 0));
            if (!UnityEngine.Object.FindObjectsByType<XRInteractionSimulator>(FindObjectsSortMode.None).Any())
                PrefabUtility.InstantiatePrefab(simulatorPrefab, SceneManager.GetActiveScene());
        }

        static GameObject Cube(Transform parent, string name, Vector3 scale, Vector3 localPosition, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        static TextMeshPro Text(Transform parent, string value, Vector3 localPosition, float size, Color color)
        {
            var go = new GameObject("Label", typeof(TextMeshPro)); go.transform.SetParent(parent, false); go.transform.localPosition = localPosition;
            var text = go.GetComponent<TextMeshPro>(); text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center; return text;
        }
        static GameObject Capsule(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = Mat(name, color); return go;
        }
        static GameObject Sphere(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = Mat(name, color); return go;
        }
    }
}
