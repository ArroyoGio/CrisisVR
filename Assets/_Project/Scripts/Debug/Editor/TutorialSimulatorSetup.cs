using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Unity.XR.CoreUtils;

namespace CrisisVR.Editor
{
    // Monta el XR Interaction Simulator en la escena abierta para probar con teclado y raton.
    // No se ejecuta solo: la desarrolladora elige el menu.
    public static class TutorialSimulatorSetup
    {
        const string Prefab =
            "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab";

        [MenuItem("CrisisVR/T-23/Montar simulador XR en la escena abierta")]
        public static void Mount()
        {
            if (EditorApplication.isPlaying)
                throw new System.InvalidOperationException("T-23: salir de Play antes de montar el simulador.");

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                throw new System.InvalidOperationException("T-23: no hay una escena abierta.");

            // Evitar duplicados: reutilizar el simulador si ya existe.
            var existing = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<XRInteractionSimulator>(true))
                .FirstOrDefault();
            if (existing != null)
            {
                Debug.Log("T-23: la escena ya tiene un XR Interaction Simulator; no se duplica.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if (prefab == null)
                throw new System.InvalidOperationException("T-23: no se encontro el prefab del simulador: " + Prefab);

            // Colocar el simulador junto al rig XR (origi de la camara) para que siga la pose del jugador.
            var origin = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<XROrigin>(true))
                .FirstOrDefault();
            var parent = origin != null ? origin.transform.parent : null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (parent != null) instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            var sim = instance.GetComponentInChildren<XRInteractionSimulator>(true);
            if (sim != null) sim.gameObject.SetActive(true);

            Undo.RegisterCreatedObjectUndo(instance, "T-23 simulador XR");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = instance;
            Debug.Log("T-23: XR Interaction Simulator montado. Guardar la escena (Ctrl+S) para conservarlo.");
        }

        // Reactiva los mandos (Left/Right Controller) del XR Origin, que llegan apagados en la escena.
        // Sin los mandos activos, el Near-Far Interactor no lee el grip y los cubos no se agarran.
        [MenuItem("CrisisVR/T-23/Activar mandos XR (arreglar agarre)")]
        public static void EnableControllers()
        {
            if (EditorApplication.isPlaying)
                throw new System.InvalidOperationException("T-23: salir de Play antes de activar los mandos.");

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                throw new System.InvalidOperationException("T-23: no hay una escena abierta.");

            var origin = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<XROrigin>(true))
                .FirstOrDefault();
            if (origin == null)
                throw new System.InvalidOperationException("T-23: no se encontro XROrigin en la escena.");

            int activados = 0;
            foreach (var tr in origin.GetComponentsInChildren<Transform>(true))
            {
                if (tr.name == "Left Controller" || tr.name == "Right Controller")
                {
                    if (!tr.gameObject.activeSelf)
                    {
                        Undo.RecordObject(tr.gameObject, "T-23 activar mando");
                        tr.gameObject.SetActive(true);
                        activados++;
                    }
                }
            }

            if (activados == 0)
            {
                Debug.Log("T-23: los mandos ya estaban activos; nada que cambiar.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("T-23: mandos activados (" + activados + "). Escena guardada.");
        }

        // Entrada por linea de comandos: abre Tutorial.unity, activa los mandos y guarda.
        // Uso: Unity -batchmode -executeMethod CrisisVR.Editor.TutorialSimulatorSetup.ArreglarAgarreBatch
        public static void ArreglarAgarreBatch()
        {
            const string ruta = "Assets/Scenes/Tutorial.unity";
            var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            EnableControllers();
            EditorSceneManager.SaveScene(escena);
            Debug.Log("T-23: arreglo del agarre aplicado y guardado en " + ruta);
        }
    }
}
