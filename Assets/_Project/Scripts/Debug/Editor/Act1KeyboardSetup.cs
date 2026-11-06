using CrisisVR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR.Editor
{
    /// <summary>
    /// Convierte las teclas de Teclado_Fisico en Acto1.unity en teclas pulsables con el gatillo.
    /// Es idempotente: se puede ejecutar de nuevo sin duplicar componentes. No toca la tapa ni la bisagra.
    /// </summary>
    public static class Act1KeyboardSetup
    {
        const string ScenePath = "Assets/Scenes/Acto1.unity";
        const string PressSoundPath = "Assets/VRTemplateAssets/Audio/Button_22_click.wav";

        [MenuItem("CrisisVR/Acto 1/Preparar sonido de teclas")]
        public static void PrepareKeySound()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform keyboard = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != "Teclado_Fisico")
                        continue;
                    keyboard = child;
                    break;
                }
                if (keyboard != null)
                    break;
            }

            if (keyboard == null)
                throw new System.InvalidOperationException("No se encontró Teclado_Fisico en Acto1.");

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PressSoundPath);
            if (clip == null)
                throw new System.InvalidOperationException($"No se encontró el sonido de tecla: {PressSoundPath}.");

            AudioSource source = keyboard.GetComponent<AudioSource>();
            if (source == null)
                source = Undo.AddComponent<AudioSource>(keyboard.gameObject);
            else
                Undo.RecordObject(source, "Configure keyboard sound");

            source.clip = clip;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.volume = 0.35f;
            source.minDistance = 0.12f;
            source.maxDistance = 1.2f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;

            foreach (NearFarInteractor interactor in Object.FindObjectsByType<NearFarInteractor>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (interactor.gameObject.scene != scene)
                    continue;

                var provider = interactor.GetComponentInParent<IXRHapticImpulseProvider>();
                if (provider == null)
                    continue;

                HapticImpulsePlayer haptics = interactor.GetComponent<HapticImpulsePlayer>();
                if (haptics == null)
                    haptics = Undo.AddComponent<HapticImpulsePlayer>(interactor.gameObject);
                else
                    Undo.RecordObject(haptics, "Configure keyboard haptics");

                var output = haptics.hapticOutput;
                output.SetObjectReference(provider);
                output.inputSourceMode = XRInputHapticImpulseProvider.InputSourceMode.ObjectReference;
                haptics.hapticOutput = output;
                EditorUtility.SetDirty(haptics);
            }

            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Audio de pulsación preparado en Teclado_Fisico de Acto1.");
        }

        [MenuItem("CrisisVR/Acto 1/Preparar teclas físicas del teclado")]
        public static void Prepare()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform keyboard = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == "Teclado_Fisico") { keyboard = child; break; }
                if (keyboard != null) break;
            }
            if (keyboard == null)
                throw new System.InvalidOperationException("No se encontró Teclado_Fisico en Acto1.");

            int converted = 0;
            for (int i = 0; i < keyboard.childCount; i++)
            {
                Transform key = keyboard.GetChild(i);
                if (!key.name.StartsWith("Tecla_"))
                    continue;
                GameObject go = key.gameObject;

                // Primero se añade el interactable nuevo: otros componentes (el tinte XR) dependen de uno.
                var simple = go.GetComponent<XRSimpleInteractable>();
                if (simple == null)
                    simple = Undo.AddComponent<XRSimpleInteractable>(go);
                simple.selectMode = InteractableSelectMode.Single;

                Collider keyCollider = go.GetComponent<Collider>();
                if (keyCollider == null)
                    keyCollider = Undo.AddComponent<BoxCollider>(go);
                simple.colliders.Clear();
                simple.colliders.Add(keyCollider);

                var grab = go.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    Undo.DestroyObjectImmediate(grab);

                // La articulación física permitía mover la tecla; ahora el recorrido lo fija Act1KeyboardKey.
                var joint = go.GetComponent<ConfigurableJoint>();
                if (joint != null)
                    Undo.DestroyObjectImmediate(joint);

                var body = go.GetComponent<Rigidbody>();
                if (body == null)
                    body = Undo.AddComponent<Rigidbody>(go);
                body.isKinematic = true;
                body.useGravity = false;
                body.interpolation = RigidbodyInterpolation.None;
                body.constraints = RigidbodyConstraints.FreezeAll;

                // El tinte XR sigue marcando el hover; el color de pulsación lo pone la propia tecla.
                var tint = go.GetComponent("XRTintInteractableVisual");
                if (tint != null)
                {
                    var serializedTint = new SerializedObject(tint);
                    SerializedProperty onSelection = serializedTint.FindProperty("m_TintOnSelection");
                    if (onSelection != null)
                        onSelection.boolValue = false;
                    serializedTint.ApplyModifiedPropertiesWithoutUndo();
                }

                if (go.GetComponent<Act1KeyboardKey>() == null)
                    Undo.AddComponent<Act1KeyboardKey>(go);

                EditorUtility.SetDirty(go);
                converted++;
            }

            PrepareKeySound();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Teclado Acto 1 preparado: {converted} teclas pulsables con el gatillo (sin agarre ni articulación).");
        }
    }
}
