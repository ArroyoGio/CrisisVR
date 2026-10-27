// Build Android APK para Crisis VR
// Ubicacion: Assets/_Project/Scripts/Build/Editor/ (compila en Assembly-CSharp-Editor)
// Ejecutar desde Unity CLI: -executeMethod CrisisVR.Editor.BuildAndroid.Build
// o desde menu Unity: CrisisVR/Build Android APK

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrisisVR.Editor
{
    public static class BuildAndroid
    {
        const string ApkPath = "Builds/CrisisVR.apk";

        // Intenta construir el APK con las escenas habilitadas en File > Build Settings.
        public static void Build()
        {
            string[] escenas = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (escenas.Length == 0)
            {
                Debug.LogError("CrisisVR.BuildAndroid: no hay escenas habilitadas en Build Settings.");
                SalirConCodigo(1);
                return;
            }

            var opciones = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            // Requiere el modulo Android (SDK/NDK) instalado desde Unity Hub.
            var reporte = BuildPipeline.BuildPlayer(opciones);

            if (reporte.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("CrisisVR.BuildAndroid: fallo al compilar el APK. Revisar la consola.");
                SalirConCodigo(1);
            }
            else
            {
                Debug.Log("CrisisVR.BuildAndroid: APK generado en " + ApkPath);
                SalirConCodigo(0);
            }
        }

        // Solo cierra el Editor en modo batch; desde el menu deja Unity abierto.
        static void SalirConCodigo(int codigo)
        {
            if (Application.isBatchMode) EditorApplication.Exit(codigo);
        }

        [MenuItem("CrisisVR/Build Android APK")]
        public static void BuildAndroidMenu()
        {
            Build();
        }
    }
}
