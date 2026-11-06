using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrisisVR
{
    // Anade las escenas propias del proyecto al Build Settings.
    // Se ejecuta solo al cargar el Editor y tambien desde el menu manual.
    public static class EditorBuildSetup
    {
        // Orden de arranque de la guia: menu (portada + bienvenida, en una sola escena), tutorial y aula de los tres actos.
        static readonly string[] EscenasPropias =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Tutorial.unity",
            "Assets/Scenes/Act1.unity",
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
                        // Se retiran del build las escenas que ya no existen (p. ej. BasicScene).
                        if (System.IO.File.Exists(escena.path)) lista.Add(escena);
                        else hayCambio = true;
                    }
                }

                // El orden importa: la primera escena es la que abre la aplicacion.
                if (!hayCambio && !lista.Select(s => s.path).SequenceEqual(actuales.Select(s => s.path)))
                    hayCambio = true;

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
