using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;

namespace CrisisVR.Editor
{
    // Montaje explícito en Editor; nunca se ejecuta al cargar la escena.
    public static class TutorialSceneSetup
    {
        const string Base = "Assets/_Project/";
        const string Starter = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/";
        static Dictionary<string, string[]> texts;
        static TMP_FontAsset font;
        static Color navy = new Color(0.035f, 0.07f, 0.12f);
        static Color ink = new Color(0.84f, 0.91f, 0.96f);
        static Color cyan = new Color(0.16f, 0.78f, 0.83f);

        [MenuItem("CrisisVR/T-23/Actualizar textos desde Marco.yarn")]
        public static void UpdateTexts()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("T-23: salir de Play antes de actualizar textos.");
            var scene = SceneManager.GetActiveScene();
            var flow = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TutorialFlow>(true)).Single();
            texts = ReadNodes(Base + "Yarn/Marco.yarn");
            Undo.RecordObject(flow, "T-23 textos");
            foreach (var exercise in flow.exercises)
            {
                var lines = texts["Tutorial_" + exercise.step];
                exercise.title = lines[0]; exercise.instruction = lines[1]; exercise.help = lines[2];
            }
            Undo.RecordObject(flow.panel, "T-23 etiquetas");
            var labels = texts["Tutorial_Labels"];
            flow.panel.leftHandLabel = labels[4]; flow.panel.rightHandLabel = labels[5]; flow.neutralConfirmation = labels[6];
            string[] names = { "HelpButton", "RepeatButton", "ContinueButton", "PracticeButton" };
            for (int i=0; i<names.Length; i++)
            {
                var label = flow.panel.transform.Find(names[i] + "/Label").GetComponent<TMP_Text>();
                Undo.RecordObject(label, "T-23 etiqueta"); label.text = labels[i]; EditorUtility.SetDirty(label);
            }
            flow.panel.transform.Find("Brand").GetComponent<TMP_Text>().text = labels[7];
            flow.panel.Show(flow.exercises[0], false, flow.exercises[0].step == TutorialStep.Interface);
            EditorUtility.SetDirty(flow); EditorUtility.SetDirty(flow.panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("CrisisVR/T-23/Montar tutorial en escena activa")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Tutorial.unity")
                throw new InvalidOperationException("T-23: requiere Tutorial activa, fuera de Play.");
            if (scene.GetRootGameObjects().Any(g => g.GetComponentInChildren<TutorialFlow>(true) != null))
                throw new InvalidOperationException("T-23: ya existe montaje. No se sobrescribe.");
            texts = ReadNodes(Base + "Yarn/Marco.yarn");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Starter + "Prefabs/XR Origin (XR Rig).prefab");
            var actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Starter + "XRI Default Input Actions.inputactions");
            if (prefab == null || actionAsset == null) throw new InvalidOperationException("T-23: faltan Starter Assets.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("T-23: falta fuente TMP.");
            Directory.CreateDirectory("Temp/T23");
            if (!EditorSceneManager.SaveScene(scene, "Temp/T23/Tutorial.before.unity", true))
                throw new InvalidOperationException("T-23: no se pudo guardar copia previa.");
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("T-23 montar tutorial");
            var cameras = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).ToArray();
            var previous = cameras.FirstOrDefault(c => c.isActiveAndEnabled);
            Vector3 position = previous != null ? previous.transform.position : new Vector3(70.64f, 1.6f, -17.74f);
            float yaw = previous != null ? previous.transform.eulerAngles.y : 180f;
            var root = New("TutorialRoot", null);
            Undo.RegisterCreatedObjectUndo(root, "T-23 root");
            root.transform.SetPositionAndRotation(new Vector3(position.x, 0.07f, position.z), Quaternion.Euler(0, yaw, 0));
            var flow = root.AddComponent<TutorialFlow>();
            var router = root.AddComponent<TutorialInputRouter>();
            flow.input = router;
            router.flow = flow;
            router.actions = actionAsset;
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(rig, "T-23 rig");
            rig.name = "Tutorial XR Origin";
            rig.transform.SetParent(root.transform, false);
            foreach (var c in cameras)
            {
                Undo.RecordObject(c.gameObject, "T-23 cámara previa");
                c.gameObject.SetActive(false);
            }
            var managers = rig.GetComponentsInChildren<ControllerInputActionManager>(true);
            router.leftManager = managers.First(m => m.name.Contains("Left"));
            router.rightManager = managers.First(m => m.name.Contains("Right"));
            router.movement = rig.GetComponentInChildren<ContinuousMoveProvider>(true);
            router.snapTurn = rig.GetComponentInChildren<SnapTurnProvider>(true);
            router.continuousTurn = rig.GetComponentInChildren<ContinuousTurnProvider>(true);
            foreach (var manager in managers)
            {
                var serialized = new SerializedObject(manager);
                serialized.FindProperty("m_TeleportMode").objectReferenceValue = null;
                serialized.FindProperty("m_TeleportModeCancel").objectReferenceValue = null;
                serialized.FindProperty("m_TeleportInteractor").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var provider in rig.GetComponentsInChildren<LocomotionProvider>(true))
                if (!(provider is ContinuousMoveProvider) && !(provider is SnapTurnProvider) && !(provider is ContinuousTurnProvider)) provider.enabled = false;
            foreach (var interactor in rig.GetComponentsInChildren<XRRayInteractor>(true))
                if (interactor.name.Contains("Teleport")) interactor.gameObject.SetActive(false);
            var camera = rig.GetComponentInChildren<Camera>(true);
            var origin = rig.GetComponent<XROrigin>();
            router.playerOrigin = origin;
            origin.Origin = rig;
            origin.CameraYOffset = 1.6f;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            var cameraOffset = origin.CameraFloorOffsetObject;
            cameraOffset.transform.localPosition = new Vector3(0, 1.6f, 0);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            if (!scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<XRInteractionManager>(true)).Any())
                New("XR Interaction Manager", root.transform).AddComponent<XRInteractionManager>();
            var events = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EventSystem>(true)).FirstOrDefault(e => e.isActiveAndEnabled);
            if (events == null)
            {
                var eventObject = New("EventSystem", root.transform);
                events = eventObject.AddComponent<EventSystem>();
                eventObject.AddComponent<XRUIInputModule>();
            }
            else
            {
                foreach (var module in events.GetComponents<BaseInputModule>())
                    if (!(module is XRUIInputModule)) { Undo.RecordObject(module, "T-23 input module"); module.enabled = false; }
                if (events.GetComponent<XRUIInputModule>() == null) Undo.AddComponent<XRUIInputModule>(events.gameObject);
            }
            var normal = Material("CueNeutral", new Color(0.18f, 0.43f, 0.54f));
            var hover = Material("CueHover", new Color(0.45f, 0.91f, 0.96f));
            var activated = Material("CueActivated", new Color(0.57f, 0.65f, 0.86f));
            var floorMat = Material("PracticeFloor", new Color(0.14f, 0.2f, 0.25f));
            var tableMat = Material("PracticeDesk", new Color(0.1f, 0.16f, 0.21f));
            Primitive("PracticeFloor", root.transform, new Vector3(0,-0.05f,0.4f), new Vector3(6,0.08f,6), floorMat);
            for (int side = 0; side < 4; side++)
            {
                var boundary = New("PracticeBoundary" + side, root.transform);
                boundary.transform.localPosition = side < 2 ? new Vector3(side == 0 ? -3f : 3f,1f,0.4f) : new Vector3(0,1f,side == 2 ? -2.6f : 3.4f);
                boundary.AddComponent<BoxCollider>().size = side < 2 ? new Vector3(0.1f,2f,6f) : new Vector3(6f,2f,0.1f);
            }
            Primitive("Desk", root.transform, new Vector3(0,0.82f,1.9f), new Vector3(1.8f,0.1f,0.65f), tableMat);
            Primitive("DeskLeft", root.transform, new Vector3(-0.7f,0.4f,1.9f), new Vector3(0.08f,0.8f,0.4f), tableMat);
            Primitive("DeskRight", root.transform, new Vector3(0.7f,0.4f,1.9f), new Vector3(0.08f,0.8f,0.4f), tableMat);
            for (int i=0; i<3; i++)
            {
                var mark = Primitive("MovementReference" + i, root.transform, new Vector3((i-1)*0.6f,0.005f,0.4f), new Vector3(0.3f,0.01f,0.06f), hover);
                mark.GetComponent<Collider>().enabled = false;
            }
            var objects = New("PracticeObjects", root.transform);
            flow.practiceObjects = objects;
            var cues = new List<TutorialVisualCue>();
            for (int i=0; i<2; i++)
            {
                var target = Primitive("PracticeObject" + (i+1), objects.transform, new Vector3(i==0 ? -0.35f : 0.35f,1.04f,1.9f), Vector3.one*0.28f, normal);
                target.AddComponent<XRSimpleInteractable>();
                var cue = target.AddComponent<TutorialVisualCue>();
                cue.target = target.GetComponent<Renderer>(); cue.normal = normal; cue.hover = hover; cue.activated = activated;
                var practice = target.AddComponent<TutorialPracticeTarget>(); practice.flow = flow; practice.cue = cue;
                cues.Add(cue);
            }
            flow.objectCues = cues.ToArray();
            BuildPanel(root.transform, flow, camera);
            BuildControllerCue(root.transform, flow, hover);
            var steps = TutorialSequenceUpgrade.StepOrder;
            flow.exercises = steps.Select(step =>
            {
                var lines = texts["Tutorial_" + step];
                return new TutorialExercise { step = step, title = lines[0], instruction = lines[1], help = lines[2] };
            }).ToArray();
            flow.neutralConfirmation = texts["Tutorial_Labels"][6];
            flow.panel.Show(flow.exercises[0], false, false);
            flow.controllerCue.Show(TutorialStep.Orientation, false);
            objects.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(root.transform.TransformPoint(new Vector3(0,1.6f,1.3f)), root.transform.rotation, 4f);
            if (File.Exists(Base + "Art/Icons/Lucide/move.png")) TutorialVisualUpgrade.Apply();
            TutorialWorldLayout.Apply();
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        static void BuildPanel(Transform parent, TutorialFlow flow, Camera camera)
        {
            var go = New("InstructionPanel", parent, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1600,940); rect.localScale = Vector3.one*0.0017f;
            rect.localPosition = new Vector3(0,1.85f,3.1f);
            var panel = go.AddComponent<TutorialPanel>(); flow.panel = panel;
            Image("Background", rect, new Vector2(0,0), new Vector2(1600,940), navy);
            Image("TopAccent", rect, new Vector2(0,466), new Vector2(1600,8), cyan);
            Text("Brand", rect, texts["Tutorial_Labels"][7], new Vector2(-40,392), new Vector2(1400,60), 26, cyan);
            panel.heading = Text("Heading", rect, "", new Vector2(-40,284), new Vector2(1400,110), 60, Color.white);
            panel.instruction = Text("Instruction", rect, "", new Vector2(-370,100), new Vector2(670,260), 46, ink);
            panel.hint = Text("Help", rect, "", new Vector2(-370,-119), new Vector2(670,140), 25, new Color(0.65f,0.8f,0.87f));
            panel.hand = Text("Hand", rect, texts["Tutorial_Labels"][4], new Vector2(0,-281), new Vector2(1450,55), 25, cyan);
            panel.leftHandLabel = texts["Tutorial_Labels"][4]; panel.rightHandLabel = texts["Tutorial_Labels"][5];
            Button("HelpButton", rect, texts["Tutorial_Labels"][0], new Vector2(-535,-388), new Vector2(260,86), flow.Help);
            panel.repeatButton = Button("RepeatButton", rect, texts["Tutorial_Labels"][1], new Vector2(-235,-388), new Vector2(260,86), flow.Repeat);
            panel.continueButton = Button("ContinueButton", rect, texts["Tutorial_Labels"][2], new Vector2(300,-388), new Vector2(610,86), flow.Continue);
            panel.practice = Button("PracticeButton", rect, texts["Tutorial_Labels"][3], new Vector2(0,-219), new Vector2(670,72), flow.SelectInterface);
        }

        static void BuildControllerCue(Transform parent, TutorialFlow flow, Material marker)
        {
            var root = New("ControllerCue", parent); root.transform.localPosition = new Vector3(0.60f,1.9f,2.35f);
            var cue = root.AddComponent<TutorialControllerCue>(); flow.controllerCue = cue; cue.flow = flow; cue.emphasis = marker;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRTemplateAssets/Models/Controllers/UniversalController.fbx");
            if (model == null) throw new InvalidOperationException("T-23: falta modelo universal del mando.");
            var officialLeft = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "Art/MetaTouchPlus/Models/MetaQuestTouchPlus_Left.fbx");
            var officialRight = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "Art/MetaTouchPlus/Models/MetaQuestTouchPlus_Right.fbx");
            bool official = officialLeft != null && officialRight != null;
            cue.leftModel = UnityEngine.Object.Instantiate(official ? officialLeft : model, root.transform); cue.leftModel.name = "LeftControllerReference";
            cue.rightModel = UnityEngine.Object.Instantiate(official ? officialRight : model, root.transform); cue.rightModel.name = "RightControllerReference";
            foreach (var obj in new[] { cue.leftModel, cue.rightModel })
            {
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.Euler(-35,-30,0);
                var bounds = new Bounds(obj.transform.position, Vector3.zero);
                foreach (var r in obj.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                obj.transform.localScale *= 0.75f / Mathf.Max(0.001f,size);
                if (official)
                {
                    bool left = obj == cue.leftModel;
                    var material = Material(left ? "TouchPlusLeft" : "TouchPlusRight", Color.white);
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Base + "Art/MetaTouchPlus/Textures/MetaQuestTouchPlus_" + (left ? "Left" : "right") + "_BaseColor_AO.png"));
                    foreach (var renderer in obj.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
                    EditorUtility.SetDirty(material);
                }
            }
            if (!official) { var mirrored = cue.leftModel.transform.localScale; mirrored.x *= -1f; cue.leftModel.transform.localScale = mirrored; }
            cue.leftJoystick = cue.leftModel.GetComponentsInChildren<Transform>(true).First(t => t.name == "ThumbStick" || t.name.EndsWith("_b_thumbstick"));
            cue.rightJoystick = cue.rightModel.GetComponentsInChildren<Transform>(true).First(t => t.name == "ThumbStick" || t.name.EndsWith("_b_thumbstick"));
            cue.leftTrigger = cue.leftModel.GetComponentsInChildren<Transform>(true).First(t => t.name == "Trigger" || t.name.EndsWith("_b_trigger_front"));
            cue.rightTrigger = cue.rightModel.GetComponentsInChildren<Transform>(true).First(t => t.name == "Trigger" || t.name.EndsWith("_b_trigger_front"));
            cue.joystickCue = Primitive("JoystickMarker", root.transform, new Vector3(0,0.14f,-0.04f), Vector3.one*0.035f, marker, PrimitiveType.Sphere);
            cue.triggerCue = Primitive("TriggerMarker", root.transform, new Vector3(0,0.04f,0.06f), new Vector3(0.055f,0.06f,0.04f), marker, PrimitiveType.Sphere);
            cue.arrows = New("DirectionArrows", root.transform);
            Primitive("Horizontal", cue.arrows.transform, Vector3.zero, new Vector3(0.14f,0.012f,0.012f), marker);
            Primitive("Vertical", cue.arrows.transform, Vector3.zero, new Vector3(0.012f,0.012f,0.14f), marker);
            foreach (var collider in root.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }

        static GameObject New(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, components); go.transform.SetParent(parent,false); return go;
        }
        static GameObject Primitive(string name, Transform parent, Vector3 position, Vector3 scale, Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static Material Material(string name, Color color)
        {
            string path = Base + "Materials/Tutorial/" + name + ".mat";
            Directory.CreateDirectory(Base + "Materials/Tutorial");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.color = color;
            AssetDatabase.CreateAsset(mat,path); return mat;
        }
        static UnityEngine.UI.Image Image(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = New(name,parent,typeof(RectTransform),typeof(UnityEngine.UI.Image)); var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        static TMP_Text Text(string name, RectTransform parent, string content, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var go = New(name,parent,typeof(RectTransform),typeof(TextMeshProUGUI)); var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = font; text.text = content; text.fontSize = fontSize;
            text.color = color; text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            return text;
        }
        static UnityEngine.UI.Button Button(string name, RectTransform parent, string label, Vector2 position, Vector2 size, UnityAction action)
        {
            var image = Image(name,parent,position,size,new Color(0.14f,0.28f,0.36f)); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(0.5f,0.9f,0.95f); colors.pressedColor = new Color(0.7f,0.76f,0.9f); button.colors = colors;
            var text = Text("Label",image.rectTransform,label,Vector2.zero,size-new Vector2(24,12),29,Color.white);
            text.alignment = TextAlignmentOptions.Center;
            UnityEventTools.AddPersistentListener(button.onClick,action); return button;
        }
        internal static Dictionary<string,string[]> ReadNodes(string path)
        {
            // Solo contenido estático de tres líneas; no ejecuta Yarn ni sus comandos.
            var nodes = new Dictionary<string,string[]>(); string id = null; bool body = false; var lines = new List<string>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("title: ")) { id = line.Substring(7); lines.Clear(); }
                else if (line == "---") body = true;
                else if (line == "===") { nodes.Add(id,lines.ToArray()); body = false; }
                else if (body && line.Length > 0 && !line.StartsWith("//")) lines.Add(line);
            }
            return nodes;
        }
    }
}
