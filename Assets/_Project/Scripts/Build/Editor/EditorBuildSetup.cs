using UnityEditor;
using UnityEngine;

namespace CrisisVR
{
    // Anade las escenas propias del proyecto al Build Settings.
    // Se ejecuta solo al cargar el Editor y tambien desde el menu manual.
    public static class EditorBuildSetup
    {
        // Orden de arranque: primero el tutorial, despues Act1.
        static readonly string[] EscenasPropias =
        {
            "Assets/Scenes/Tutorial.unity",
            "Assets/Scenes/Act1.unity",
            "Assets/Scenes/BasicScene.unity",
        };

        // Escena de plantilla: no se borra, solo se deshabilita del build.
        const string EscenaPlantilla = "Assets/Scenes/SampleScene.unity";

        static bool enCurso;

        [InitializeOnLoadMethod]
        static void AlCargarEditor()
        {
            Aplicar(false);
        }

        [MenuItem("CrisisVR/Build/Añadir escenas al build")]
        static void MenuManual()
        {
            Aplicar(true);
        }

        static void Aplicar(bool forzarLog)
        {
            if (enCurso) return;
            enCurso = true;
            try
            {
                var actuales = EditorBuildSettings.scenes;
                var lista = new System.Collections.Generic.List<EditorBuildSettingsScene>();
                bool hayCambio = false;

                foreach (string ruta in EscenasPropias)
                {
                    var existente = System.Array.Find(actuales, s => s.path == ruta);
                    if (existente != null)
                    {
                        lista.Add(existente);
                        continue;
                    }
                    if (!System.IO.File.Exists(ruta))
                    {
                        Debug.LogWarning("CrisisVR: la escena no existe y no se anadio: " + ruta);
                        continue;
                    }
                    lista.Add(new EditorBuildSettingsScene(ruta, true));
                    hayCambio = true;
                }

                foreach (var escena in actuales)
                {
                    if (escena.path == EscenaPlantilla && escena.enabled)
                    {
                        escena.enabled = false;
                        hayCambio = true;
                    }
                    else if (!lista.Contains(escena) && escena.path != EscenaPlantilla)
                    {
                        lista.Add(escena);
                    }
                }

                if (hayCambio)
                {
                    EditorBuildSettings.scenes = lista.ToArray();
                    Debug.Log("CrisisVR: build actualizado con " + EscenasPropias.Length + " escenas propias.");
                }
                else if (forzarLog)
                {
                    Debug.Log("CrisisVR: el build ya contiene las escenas propias.");
                }
            }
            finally
            {
                enCurso = false;
            }
        }
    }
}
