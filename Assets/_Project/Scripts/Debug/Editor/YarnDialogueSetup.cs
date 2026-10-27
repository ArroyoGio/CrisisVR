using System;
using System.Linq;
using CrisisVR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;
using Yarn.Unity;

namespace CrisisVR.Editor
{
    public static class YarnDialogueSetup
    {
        const string ScenePath = "Assets/Scenes/Act1.unity";
        const string ProjectPath = "Assets/_Project/Yarn/CrisisVR.yarnproject";
        const string PrefabGuid = "52571f68872914e24837210513edea1d";
        const string RootName = "CrisisVR Yarn Dialogue System";

        [MenuItem("CrisisVR/Yarn/Configurar diálogo VR")]
        public static void ConfigureScene()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Sal de Play Mode antes de configurar el diálogo.");

            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Abre Assets/Scenes/Act1.unity antes de configurar el diálogo.");

            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(ProjectPath);
            if (project == null || project.Program == null)
                throw new InvalidOperationException("CrisisVR.yarnproject no se importó o tiene errores de Yarn.");

            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            var camera = origin != null ? origin.Camera : null;
            if (camera == null)
                throw new InvalidOperationException("Act1 necesita una cámara asignada al XR Origin.");

            var prefabPath = AssetDatabase.GUIDToAssetPath(PrefabGuid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("No se encontró el prefab Dialogue System de Yarn Spinner.");

            var system = GameObject.Find(RootName);
            if (system == null)
                system = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            system.name = RootName;

            var runner = system.GetComponent<DialogueRunner>();
            if (runner == null)
                throw new InvalidOperationException("El prefab de Yarn Spinner no contiene DialogueRunner.");
            runner.SetProject(project);

            system.transform.SetParent(camera.transform, false);
            system.transform.localPosition = new Vector3(0, -0.28f, 1.3f);
            system.transform.localRotation = Quaternion.identity;
            system.transform.localScale = Vector3.one;

            ConfigureCanvas(system, camera);
            ConfigureContinueButton(system, runner);

            foreach (var eventSystem in system.GetComponentsInChildren<EventSystem>(true))
                eventSystem.gameObject.SetActive(false);

            var oldSubtitlePanel = GameObject.Find("Subtítulos Act1");
            if (oldSubtitlePanel != null)
                oldSubtitlePanel.SetActive(false);

            var flow = UnityEngine.Object.FindAnyObjectByType<Act1Flow>();
            if (flow != null)
            {
                var serializedFlow = new SerializedObject(flow);
                serializedFlow.FindProperty("dialogueRunner").objectReferenceValue = runner;
                serializedFlow.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(flow);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Yarn Spinner quedó conectado al panel world-space de Act1; el avance del diálogo usa UI de XR.");
        }

        static void ConfigureCanvas(GameObject system, Camera camera)
        {
            var canvas = system.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
                throw new InvalidOperationException("El prefab de Yarn Spinner no contiene un Canvas.");

            canvas.gameObject.name = "VR Dialogue Canvas";
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.pixelPerfect = false;

            var rect = (RectTransform)canvas.transform;
            rect.sizeDelta = new Vector2(1920, 1080);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.00058f;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1;
                scaler.dynamicPixelsPerUnit = 1;
            }

            foreach (var graphicRaycaster in canvas.GetComponents<GraphicRaycaster>())
                UnityEngine.Object.DestroyImmediate(graphicRaycaster);

            var trackedRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (trackedRaycaster == null)
                trackedRaycaster = canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            var presenter = system.GetComponentInChildren<LinePresenter>(true);
            if (presenter != null)
            {
                presenter.autoAdvance = false;
                presenter.useFadeEffect = true;
                presenter.lettersPerSecond = 42;
                if (presenter.lineText != null)
                {
                    presenter.lineText.fontSize = 48;
                    presenter.lineText.alignment = TextAlignmentOptions.TopLeft;
                    presenter.lineText.enableAutoSizing = true;
                    presenter.lineText.fontSizeMin = 36;
                    presenter.lineText.fontSizeMax = 48;
                }
                if (presenter.characterNameText != null)
                {
                    presenter.characterNameText.fontSize = 34;
                    presenter.characterNameText.fontStyle = FontStyles.Bold;
                }
            }
        }

        static void ConfigureContinueButton(GameObject system, DialogueRunner runner)
        {
            var buttonObject = system.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "Continue Button")?.gameObject;
            var button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            var handler = system.GetComponentInChildren<LinePresenterButtonHandler>(true);
            if (button == null || handler == null)
                throw new InvalidOperationException("El prefab de Yarn Spinner no tiene Continue Button o LinePresenterButtonHandler.");

            button.onClick = new Button.ButtonClickedEvent();
            button.interactable = false;
            var buttonRect = (RectTransform)button.transform;
            buttonRect.sizeDelta = new Vector2(320, 92);

            var background = button.targetGraphic as Image;
            if (background != null)
                background.color = new Color(0.08f, 0.31f, 0.34f, 0.96f);

            var labelObject = button.transform.Find("Continue Label");
            if (labelObject == null)
            {
                var labelGo = new GameObject("Continue Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(button.transform, false);
                labelObject = labelGo.transform;
            }

            var labelRect = (RectTransform)labelObject;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "CONTINUAR";
            label.fontSize = 26;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var serializedHandler = new SerializedObject(handler);
            serializedHandler.FindProperty("continueButton").objectReferenceValue = button;
            serializedHandler.FindProperty("dialogueRunner").objectReferenceValue = runner;
            serializedHandler.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(handler);
        }
    }
}
