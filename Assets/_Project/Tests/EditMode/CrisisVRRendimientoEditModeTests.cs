using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// TI-01: guardia de regresión de las optimizaciones del Lab 12 y del orden de escenas del build.
// Solo lee archivos y assets: no abre escenas ni modifica la configuración del proyecto.
public class CrisisVRRendimientoEditModeTests
{
    static string Root => Path.GetDirectoryName(Application.dataPath);

    static int NivelCalidadAndroid()
    {
        string txt = File.ReadAllText(Path.Combine(Root, "ProjectSettings", "QualitySettings.asset"));
        var m = Regex.Match(txt, @"m_PerPlatformDefaultQuality:[\s\S]*?Android: (\d+)");
        Assert.IsTrue(m.Success, "No se encontró el nivel de calidad por defecto de Android en QualitySettings.asset.");
        return int.Parse(m.Groups[1].Value);
    }

    static SerializedObject AssetUrpAndroid(out string ruta)
    {
        int nivel = NivelCalidadAndroid();
        var asset = QualitySettings.GetRenderPipelineAssetAt(nivel);
        Assert.IsNotNull(asset, "El nivel de calidad " + nivel + " (Android) no tiene asset de render pipeline.");
        ruta = AssetDatabase.GetAssetPath(asset);
        return new SerializedObject(asset);
    }

    static bool Bool(SerializedObject so, string campo, string ruta)
    {
        var p = so.FindProperty(campo);
        Assert.IsNotNull(p, "El asset " + ruta + " no tiene la propiedad " + campo + " (¿cambió la versión de URP?).");
        return p.boolValue;
    }

    [Test]
    public void R01_AndroidUsaUrpConSrpBatcherYDynamicBatching()
    {
        var so = AssetUrpAndroid(out string ruta);
        Assert.IsTrue(Bool(so, "m_UseSRPBatcher", ruta), "SRP Batcher debe estar activo en " + ruta);
        Assert.IsTrue(Bool(so, "m_SupportsDynamicBatching", ruta), "Dynamic Batching debe estar activo en " + ruta + " (optimización O2 del Lab 12).");
    }

    [Test]
    public void R02_AndroidSinCopiasDeColorNiProfundidadNiHdr()
    {
        var so = AssetUrpAndroid(out string ruta);
        Assert.IsFalse(Bool(so, "m_RequireDepthTexture", ruta), "Depth Texture debe seguir desactivada (evita CopyDepth) en " + ruta);
        Assert.IsFalse(Bool(so, "m_RequireOpaqueTexture", ruta), "Opaque Texture debe seguir desactivada (evita CopyColor) en " + ruta);
        Assert.IsFalse(Bool(so, "m_SupportsHDR", ruta), "HDR debe seguir desactivado en " + ruta);
    }

    [Test]
    public void R03_DatosDeOclusionDeActo1Horneados()
    {
        string ruta = Path.Combine(Root, "Assets", "Scenes", "Acto1", "OcclusionCullingData.asset");
        Assert.IsTrue(File.Exists(ruta), "Faltan los datos de Occlusion Culling de Acto1 (optimización O1 del Lab 12): hay que volver a hornear.");
        Assert.Greater(new FileInfo(ruta).Length, 20000L, "Los datos de oclusión de Acto1 parecen vacíos: hay que volver a hornear.");
    }

    [Test]
    public void R04_OrdenDeEscenasDelBuild()
    {
        var escenas = EditorBuildSettings.scenes;
        Assert.Greater(escenas.Length, 0, "Build Settings no tiene escenas.");
        Assert.AreEqual("Assets/Scenes/MainMenu.unity", escenas[0].path, "La primera escena del build debe ser MainMenu.");
        int iTutorial = -1, iAct1 = -1;
        for (int i = 0; i < escenas.Length; i++)
        {
            Assert.IsTrue(escenas[i].enabled, "Escena deshabilitada en el build: " + escenas[i].path);
            Assert.IsTrue(File.Exists(Path.Combine(Root, escenas[i].path)), "La escena del build no existe: " + escenas[i].path);
            if (escenas[i].path == "Assets/Scenes/Tutorial.unity") iTutorial = i;
            if (escenas[i].path == "Assets/Scenes/Act1.unity") iAct1 = i;
        }
        Assert.GreaterOrEqual(iTutorial, 0, "Tutorial no está en el build.");
        Assert.GreaterOrEqual(iAct1, 0, "Act1 no está en el build.");
        Assert.Less(iTutorial, iAct1, "Tutorial debe ir antes que Act1.");
    }
}
