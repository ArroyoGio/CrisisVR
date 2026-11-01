using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.XR.CoreUtils;

namespace CrisisVR.Editor
{
    // Capturas de Edit Mode desde la posición inicial del estudiante, para revisar encuadre y legibilidad
    // de los Actos 2 y 3 y del cierre. No son prueba de Play Mode ni del visor.
    // Batch (con gráficos, sin -nographics): -executeMethod CrisisVR.Editor.EvidenceCapture.CaptureBatch
    public static class EvidenceCapture
    {
        const string ScenePath = "Assets/Scenes/Act1.unity";
        const string OutputFolder = "Docs/evidence";

        public static void CaptureBatch()
        {
            try
            {
                Capture();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("CrisisVR/Evidencia/Capturar Actos 2, 3 y cierre")]
        public static void Capture()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindAnyObjectByType<XROrigin>().Camera;
            camera.transform.SetPositionAndRotation(new Vector3(15.5f, 1.25f, -66.2f),
                Quaternion.LookRotation(new Vector3(0f, -.08f, -1f)));
            Directory.CreateDirectory(OutputFolder);

            var teacher = GameObject.Find("Profesor Miguel (placeholder visual)");
            var act2 = Find("Act2Prototype");
            var act3 = Find("Act3Prototype");
            var closingPanel = Find("Panel de cierre");
            var conductor = Find("Panel de quien conduce");

            if (teacher != null) teacher.SetActive(false);
            act2.SetActive(true);
            SetChild(act2, "aviso_panel", true);
            SetChild(act2, "Rodrigo (placeholder visual)", true);
            SetChild(act2, "Pantalla tablet", true);
            Render(camera, "S1_act2_aviso_tablet_carpeta_rodrigo.png");

            act2.SetActive(false);
            act3.SetActive(true);
            SetChild(act3, "Opciones A-D", true);
            SetChild(act3, "Borde (habla)", true);
            Render(camera, "S1_act3_videollamada_opciones.png");

            act3.SetActive(false);
            closingPanel.SetActive(true);
            conductor.SetActive(true);
            Render(camera, "S1_cierre_panel_conductor.png");

            // La escena no se guarda: las capturas no alteran el estado inicial de los actos.
            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
        }

        static GameObject Find(string name)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.name == name && go.scene.IsValid()) return go;
            throw new InvalidOperationException("No se encontró " + name + ": ejecuta CrisisVR/Actos 2 y 3/Preparar.");
        }

        static void SetChild(GameObject root, string name, bool active)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) t.gameObject.SetActive(active);
        }

        static void Render(Camera camera, string fileName)
        {
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            camera.targetTexture = previous;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(OutputFolder, fileName), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            rt.Release();
            Debug.Log("CrisisVR: evidencia guardada en " + OutputFolder + "/" + fileName);
        }
    }
}
