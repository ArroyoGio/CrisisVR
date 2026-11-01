// Verificación de player en Windows (Mono) para Crisis VR.
// No reemplaza el build de Android para el Quest: comprueba que los scripts de tiempo de ejecución compilan
// fuera del Editor (código de Editor filtrado a un player) cuando la PC no tiene el módulo de Android.
// El build también sirve para probar con Quest Link (forma 3 de la guía).
// Batch: -executeMethod CrisisVR.Editor.BuildWindowsCheck.Build -crisisvrOut <carpeta>

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrisisVR.Editor
{
    public static class BuildWindowsCheck
    {
        public static void Build()
        {
            string output = ArgValue("-crisisvrOut") ?? "Builds/WindowsCheck";
            var target = NamedBuildTarget.Standalone;
            var previousBackend = PlayerSettings.GetScriptingBackend(target);
            int exitCode = 1;
            try
            {
                // IL2CPP para Windows no siempre está instalado; Mono basta para verificar la compilación.
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = System.IO.Path.Combine(output, "CrisisVR.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                var summary = report.summary;
                Debug.Log($"CrisisVR.BuildWindowsCheck: {summary.result} · escenas={string.Join(", ", scenes)} · " +
                          $"tamaño={summary.totalSize / (1024 * 1024)} MB · errores={summary.totalErrors} · advertencias={summary.totalWarnings}");
                exitCode = summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(target, previousBackend);
                AssetDatabase.SaveAssets();
            }
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
        }

        static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
