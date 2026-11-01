using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using Yarn.Unity;

namespace CrisisVR.Editor
{
    // Arma en Act1.unity (el aula compartida) los Actos 2 y 3, la pausa, el cierre, el director de actos,
    // el presentador de hablantes y las teclas de depuración. Es idempotente: borra y recrea lo suyo.
    // Batch: -executeMethod CrisisVR.Editor.ActsSceneSetup.PrepareBatch
    public static class ActsSceneSetup
    {
        const string ScenePath = "Assets/Scenes/Act1.unity";
        const string MaterialFolder = "Assets/_Project/Materials/Acts23";
        const string Act2RootName = "Act2Prototype";
        const string Act3RootName = "Act3Prototype";
        const string SessionUiName = "CrisisVR Session UI";
        const string DirectorName = "CrisisVR Acts Director";
        const string TeacherName = "Profesor Miguel (placeholder visual)";
        const string DialogueSystemName = "CrisisVR Yarn Dialogue System";
        // El estudiante empieza en (15.5, 0, -66.2) mirando hacia -Z; su escritorio está en z = -67.9.
        static readonly Quaternion FacingPlayer = Quaternion.Euler(0f, 180f, 0f);

        [MenuItem("CrisisVR/Actos 2 y 3/Preparar actos, pausa y cierre")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Sal de Play Mode antes de preparar los actos.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Abre Assets/Scenes/Act1.unity antes de preparar los actos.");

            foreach (var name in new[] { Act2RootName, Act3RootName, SessionUiName, DirectorName })
            {
                var old = GameObject.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
            }

            var act1 = UnityEngine.Object.FindAnyObjectByType<Act1Flow>();
            if (act1 == null) throw new InvalidOperationException("Falta Act1Flow: ejecuta antes CrisisVR/Act 1/Preparar aula.");
            var teacher = GameObject.Find(TeacherName);
            var desk = GameObject.Find("Aula 2 mesa 0-2");
            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            var camera = origin != null ? origin.Camera : null;
            var dialogueSystem = GameObject.Find(DialogueSystemName);
            var runner = dialogueSystem != null ? dialogueSystem.GetComponent<DialogueRunner>() : null;
            var card = UnityEngine.Object.FindAnyObjectByType<Act1TransitionCard>(FindObjectsInactive.Include);
            if (teacher == null || desk == null || camera == null || runner == null || card == null)
                throw new InvalidOperationException("Faltan objetos de Act1 (profesor, escritorio, cámara XR, DialogueRunner o tarjeta de transición).");

            float deskTop = DeskTop(desk.transform);
            Vector3 deskCenter = desk.transform.position;

            var sessionUi = new GameObject(SessionUiName);
            var instruction = MakeInstruction(sessionUi.transform);

            var act2Root = new GameObject(Act2RootName);
            var act2 = BuildAct2(act2Root.transform, deskCenter, deskTop);
            var act3Root = new GameObject(Act3RootName);
            var act3 = BuildAct3(act3Root.transform);

            var dialogueCanvas = dialogueSystem.GetComponentInChildren<Canvas>(true);
            var pause = BuildPauseMenu(sessionUi.transform, camera, dialogueCanvas);
            var closing = BuildClosing(sessionUi.transform);

            var director = new GameObject(DirectorName);
            var act2Flow = director.AddComponent<Act2Flow>();
            Assign(act2Flow, ("dialogueRunner", runner), ("actRoot", act2Root), ("teacher", teacher), ("noticePanel", act2.notice),
                ("tabletScreen", act2.tabletScreen), ("rodrigo", act2.rodrigo), ("tabletInteractable", act2.tablet),
                ("folderInteractable", act2.folder), ("instructionText", instruction), ("transitionCard", card));
            var act3Flow = director.AddComponent<Act3Flow>();
            Assign(act3Flow, ("dialogueRunner", runner), ("actRoot", act3Root), ("callScreen", act3.screen), ("optionsRoot", act3.optionsRoot),
                ("optionA", act3.options[0]), ("optionB", act3.options[1]), ("optionC", act3.options[2]), ("optionD", act3.options[3]),
                ("instructionText", instruction), ("transitionCard", card));
            var actsDirector = director.AddComponent<ActsDirector>();
            Assign(actsDirector, ("act1", act1), ("act2", act2Flow), ("act3", act3Flow), ("closing", closing), ("pauseMenu", pause));
            var debugInput = director.AddComponent<EditorDebugInput>();
            Assign(debugInput, ("act1", act1), ("act2", act2Flow), ("act3", act3Flow), ("dialogueRunner", runner));

            ConfigureSpeakers(dialogueSystem, runner, teacher, act2.rodrigo, act3.tileHighlights);

            act2Root.SetActive(false);
            act3Root.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CrisisVR: Actos 2 y 3, pausa, cierre, director y hablantes configurados en Act1.unity.");
        }

        public static void PrepareBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Prepare();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        // ---------- Acto 2 ----------

        static (XRSimpleInteractable folder, XRSimpleInteractable tablet, GameObject tabletScreen, GameObject notice, GameObject rodrigo)
            BuildAct2(Transform root, Vector3 desk, float deskTop)
        {
            var folder = Selectable(root, "carpeta_plan_original", "carpeta", new Vector3(desk.x - .34f, deskTop, desk.z + .02f),
                new Vector3(.24f, .03f, .32f), Mat("Acts23 carpeta", new Color(.55f, .42f, .22f)));
            FlatLabel(folder.transform, "PLAN ORIGINAL\npráctica presencial", .032f, new Vector3(0, .032f, 0), Color.white);

            var tablet = Selectable(root, "tablet_plan_nuevo", "tablet_plan", new Vector3(desk.x + .34f, deskTop, desk.z + .02f),
                new Vector3(.26f, .018f, .18f), Mat("Acts23 tablet", new Color(.1f, .1f, .12f)));
            // La pantalla se enciende con el aviso; su rótulo cuelga de un padre sin escala para no deformarse.
            var screen = new GameObject("Pantalla tablet");
            screen.transform.SetParent(tablet.transform, false);
            var glass = Box(screen.transform, "Vidrio", new Vector3(0, .0195f, 0), new Vector3(.23f, .002f, .15f), Mat("Acts23 pantalla tablet", new Color(.2f, .55f, .75f)));
            UnityEngine.Object.DestroyImmediate(glass.GetComponent<Collider>());
            FlatLabel(screen.transform, "PLAN NUEVO\npráctica virtual", .028f, new Vector3(0, .022f, 0), Color.white);
            screen.SetActive(false);

            var notice = Panel(root, "aviso_panel", new Vector3(15.5f, 1.62f, -69.4f), new Vector2(1.5f, .42f), Mat("Acts23 aviso", new Color(.09f, .2f, .36f)));
            PanelLabel(notice.transform, "AVISO DEL CAMPUS\nLa práctica de hoy pasa a modalidad virtual.\nLos grupos se han reorganizado.", .055f, new Vector2(1.4f, .36f));
            notice.SetActive(false);

            var rodrigo = Npc(root, "Rodrigo (placeholder visual)", new Vector3(14.55f, 0f, -67.45f), new Color(.2f, .45f, .3f));
            rodrigo.SetActive(false);
            return (folder.GetComponent<XRSimpleInteractable>(), tablet.GetComponent<XRSimpleInteractable>(), screen, notice, rodrigo);
        }

        // ---------- Acto 3 ----------

        static (GameObject screen, GameObject optionsRoot, XRSimpleInteractable[] options, Dictionary<string, GameObject> tileHighlights)
            BuildAct3(Transform root)
        {
            var screen = Panel(root, "pantalla_videollamada", new Vector3(15.5f, 1.62f, -70.1f), new Vector2(2.2f, 1.05f), Mat("Acts23 pantalla", new Color(.05f, .06f, .08f)));
            var highlights = new Dictionary<string, GameObject>();
            string[] names = { "Lucía", "Andrés", "Mateo" };
            Color[] colors = { new Color(.62f, .35f, .45f), new Color(.35f, .45f, .62f), new Color(.45f, .55f, .35f) };
            for (int i = 0; i < names.Length; i++)
            {
                var tile = new GameObject("Recuadro " + names[i]);
                tile.transform.SetParent(screen.transform, false);
                tile.transform.localPosition = new Vector3(-.7f + .7f * i, 0f, -.02f);
                var border = Box(tile.transform, "Borde (habla)", new Vector3(0, 0, .004f), new Vector3(.66f, .86f, .004f), Mat("Acts23 borde", new Color(.98f, .8f, .2f)));
                border.SetActive(false);
                highlights[names[i]] = border;
                Box(tile.transform, "Fondo", Vector3.zero, new Vector3(.62f, .82f, .006f), Mat("Acts23 recuadro", new Color(.16f, .18f, .22f)));
                Box(tile.transform, "Silueta cabeza", new Vector3(0, .14f, -.006f), new Vector3(.18f, .2f, .004f), Mat("Acts23 silueta " + i, colors[i]));
                Box(tile.transform, "Silueta hombros", new Vector3(0, -.12f, -.006f), new Vector3(.36f, .22f, .004f), Mat("Acts23 silueta " + i, colors[i]));
                PanelLabel(tile.transform, names[i], .06f, new Vector2(.6f, .12f), new Vector3(0, -.34f, -.01f));
            }

            var optionsRoot = new GameObject("Opciones A-D");
            optionsRoot.transform.SetParent(root, false);
            var options = new XRSimpleInteractable[4];
            for (int code = DecisionCodes.Team_A; code <= DecisionCodes.Team_D; code++)
            {
                string letter = DecisionCodes.TeamLetter(code);
                float x = 15.5f + (-.69f + .46f * (code - 1));
                var button = new GameObject("opcion_" + letter);
                button.transform.SetParent(optionsRoot.transform, false);
                button.transform.position = new Vector3(x, 1.08f, -67.25f);
                button.transform.rotation = FacingPlayer;
                var visual = Box(button.transform, "Fondo", Vector3.zero, new Vector3(.43f, .27f, .025f), Mat("Acts23 opción", new Color(.13f, .38f, .78f)));
                var interactable = button.AddComponent<XRSimpleInteractable>();
                var highlight = button.AddComponent<SelectableHighlight>();
                Assign(highlight, ("target", visual.GetComponent<Renderer>()));
                highlight.id = letter;
                PanelLabel(button.transform, letter + "\n" + ReportTexts.Act3OptionText(code), .022f, new Vector2(.4f, .25f), new Vector3(0, 0, -.016f));
                options[code - 1] = interactable;
            }
            optionsRoot.SetActive(false);
            return (screen, optionsRoot, options, highlights);
        }

        // ---------- Pausa, cierre e instrucción ----------

        static TMP_Text MakeInstruction(Transform parent)
        {
            var go = new GameObject("Instrucción", typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(15.5f, 1.36f, -67.6f);
            go.transform.rotation = FacingPlayer;
            var text = go.GetComponent<TextMeshPro>();
            text.fontSize = .5f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, .93f, .6f);
            text.rectTransform.sizeDelta = new Vector2(1.4f, .2f);
            text.enableWordWrapping = true;
            text.text = string.Empty;
            go.SetActive(false);
            return text;
        }

        static PauseMenu BuildPauseMenu(Transform parent, Camera camera, Canvas dialogueCanvas)
        {
            var holder = new GameObject("Pausa");
            holder.transform.SetParent(parent, false);
            var pause = holder.AddComponent<PauseMenu>();

            var canvas = WorldCanvas("Panel de pausa", camera.transform, new Vector2(900, 560));
            canvas.transform.localPosition = new Vector3(0, 0, .95f);
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one * .001f;
            canvas.GetComponent<Canvas>().sortingOrder = 300;
            Background(canvas.transform, new Color(.06f, .08f, .1f, .96f));
            UiText(canvas.transform, "Pausa", 72, new Vector2(0, 160), new Vector2(800, 120), FontStyles.Bold);
            UiText(canvas.transform, "La experiencia está detenida.", 40, new Vector2(0, 60), new Vector2(800, 80), FontStyles.Normal);
            var resume = UiButton(canvas.transform, "CONTINUAR", new Vector2(-200, -120));
            var end = UiButton(canvas.transform, "TERMINAR SESIÓN", new Vector2(200, -120));
            Assign(pause, ("panel", canvas), ("continueButton", resume), ("endSessionButton", end));
            var so = new SerializedObject(pause);
            var hidden = so.FindProperty("hideWhilePaused");
            hidden.arraySize = dialogueCanvas != null ? 1 : 0;
            if (dialogueCanvas != null) hidden.GetArrayElementAtIndex(0).objectReferenceValue = dialogueCanvas;
            so.ApplyModifiedPropertiesWithoutUndo();
            canvas.SetActive(false);
            return pause;
        }

        static ClosingFlow BuildClosing(Transform parent)
        {
            var holder = new GameObject("Cierre");
            holder.transform.SetParent(parent, false);
            var closing = holder.AddComponent<ClosingFlow>();

            var panel = WorldCanvas("Panel de cierre", holder.transform, new Vector2(1500, 600));
            panel.transform.position = new Vector3(15.5f, 1.6f, -69.2f);
            panel.transform.rotation = FacingPlayer;
            panel.transform.localScale = Vector3.one * .001f;
            Background(panel.transform, new Color(.06f, .08f, .1f, .95f));
            UiText(panel.transform, "Gracias por participar. La experiencia ha terminado.", 60, new Vector2(0, 110), new Vector2(1400, 160), FontStyles.Bold);
            UiText(panel.transform, "No hay respuestas correctas ni incorrectas. Entrega el visor al profesional que te acompaña.", 44, new Vector2(0, -80), new Vector2(1400, 200), FontStyles.Normal);

            var conductor = WorldCanvas("Panel de quien conduce", holder.transform, new Vector2(1000, 300));
            conductor.transform.position = new Vector3(15.5f, 1.12f, -69.2f);
            conductor.transform.rotation = FacingPlayer;
            conductor.transform.localScale = Vector3.one * .001f;
            Background(conductor.transform, new Color(.12f, .14f, .17f, .95f));
            UiText(conductor.transform, "Para quien conduce la sesión", 34, new Vector2(0, 90), new Vector2(900, 60), FontStyles.Italic);
            var newSession = UiButton(conductor.transform, "NUEVA SESIÓN", new Vector2(-220, -40));
            var exit = UiButton(conductor.transform, "SALIR", new Vector2(220, -40));
            Assign(closing, ("closingPanel", panel), ("conductorPanel", conductor), ("newSessionButton", newSession), ("exitButton", exit));
            panel.SetActive(false);
            conductor.SetActive(false);
            return closing;
        }

        static void ConfigureSpeakers(GameObject dialogueSystem, DialogueRunner runner, GameObject teacher, GameObject rodrigo, Dictionary<string, GameObject> tiles)
        {
            foreach (var old in dialogueSystem.GetComponents<SpeakerPresenter>()) UnityEngine.Object.DestroyImmediate(old);
            var presenter = dialogueSystem.AddComponent<SpeakerPresenter>();
            var so = new SerializedObject(presenter);
            var list = so.FindProperty("speakers");
            var entries = new List<(string name, GameObject highlight, Act1ProfessorMotion motion)>
            {
                ("Miguel", null, teacher.GetComponent<Act1ProfessorMotion>()),
                ("Rodrigo", null, rodrigo.GetComponent<Act1ProfessorMotion>()),
            };
            entries.AddRange(tiles.Select(t => (t.Key, t.Value, (Act1ProfessorMotion)null)));
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("characterName").stringValue = entries[i].name;
                element.FindPropertyRelative("highlight").objectReferenceValue = entries[i].highlight;
                element.FindPropertyRelative("motion").objectReferenceValue = entries[i].motion;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var runnerSo = new SerializedObject(runner);
            var presenters = runnerSo.FindProperty("dialoguePresenters");
            for (int i = presenters.arraySize - 1; i >= 0; i--)
                if (presenters.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    presenters.DeleteArrayElementAtIndex(i);
            presenters.arraySize++;
            presenters.GetArrayElementAtIndex(presenters.arraySize - 1).objectReferenceValue = presenter;
            runnerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Utilidades ----------

        static GameObject Selectable(Transform parent, string name, string id, Vector3 bottomCenter, Vector3 size, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = bottomCenter;
            root.transform.rotation = FacingPlayer;
            var visual = Box(root.transform, "Cuerpo", new Vector3(0, size.y / 2f, 0), size, material);
            root.AddComponent<XRSimpleInteractable>();
            var highlight = root.AddComponent<SelectableHighlight>();
            highlight.id = id;
            Assign(highlight, ("target", visual.GetComponent<Renderer>()));
            return root;
        }

        static GameObject Npc(Transform parent, string name, Vector3 basePosition, Color shirt)
        {
            var actor = new GameObject(name);
            actor.transform.SetParent(parent, false);
            actor.transform.position = basePosition;
            actor.transform.rotation = Quaternion.LookRotation(new Vector3(15.5f - basePosition.x, 0, -66.2f - basePosition.z));
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Cuerpo";
            body.transform.SetParent(actor.transform, false);
            body.transform.localPosition = new Vector3(0, .82f, 0);
            body.transform.localScale = new Vector3(.55f, .82f, .38f);
            body.GetComponent<Renderer>().sharedMaterial = Mat("Acts23 " + name + " cuerpo", shirt);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Cabeza";
            head.transform.SetParent(actor.transform, false);
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            head.transform.localScale = new Vector3(.34f, .38f, .34f);
            head.GetComponent<Renderer>().sharedMaterial = Mat("Acts23 piel", new Color(.62f, .45f, .34f));
            actor.AddComponent<Act1ProfessorMotion>();
            return actor;
        }

        static GameObject Panel(Transform parent, string name, Vector3 center, Vector2 size, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = center;
            root.transform.rotation = FacingPlayer;
            Box(root.transform, "Fondo", Vector3.zero, new Vector3(size.x, size.y, .03f), material);
            return root;
        }

        static GameObject Box(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // Texto 3D vertical; su cara legible mira hacia el estudiante (-Z local del panel apunta a +Z mundo).
        static TextMeshPro PanelLabel(Transform parent, string value, float size, Vector2 area, Vector3? localPosition = null)
        {
            var go = new GameObject("Texto", typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition ?? new Vector3(0, 0, -.02f);
            go.transform.localRotation = Quaternion.identity;
            var text = go.GetComponent<TextMeshPro>();
            text.text = value;
            text.fontSize = size * 10f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.rectTransform.sizeDelta = area;
            return text;
        }

        // Texto 3D tendido sobre el escritorio, legible desde la silla del estudiante.
        static TextMeshPro FlatLabel(Transform parent, string value, float size, Vector3 localPosition, Color color)
        {
            var go = new GameObject("Rótulo", typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var text = go.GetComponent<TextMeshPro>();
            text.text = value;
            text.fontSize = size * 10f;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(.24f, .14f);
            return text;
        }

        static GameObject WorldCanvas(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            ((RectTransform)go.transform).sizeDelta = size;
            return go;
        }

        static void Background(Transform canvas, Color color)
        {
            var go = new GameObject("Fondo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        static TextMeshProUGUI UiText(Transform parent, string value, float size, Vector2 position, Vector2 area, FontStyles style)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = area;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        static Button UiButton(Transform parent, string label, Vector2 position)
        {
            var go = new GameObject("Botón " + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(360, 110);
            var image = go.GetComponent<Image>();
            image.color = new Color(.13f, .38f, .78f);
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(.62f, .86f, 1f);
            colors.pressedColor = new Color(.42f, .76f, .95f);
            button.colors = colors;
            UiText(go.transform, label, 36, Vector2.zero, new Vector2(340, 100), FontStyles.Bold);
            return button;
        }

        static float DeskTop(Transform desk)
        {
            var renderers = desk.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("El escritorio del estudiante no tiene renderers.");
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.max.y;
        }

        static Material Mat(string name, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "Acts23");
            var path = MaterialFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = name, color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void Assign(UnityEngine.Object target, params (string property, UnityEngine.Object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (property, value) in values)
            {
                var prop = so.FindProperty(property);
                if (prop == null) throw new InvalidOperationException($"{target.GetType().Name} no tiene el campo serializado «{property}».");
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
