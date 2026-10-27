using NUnit.Framework;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CrisisVREditModeTests
{
    string root => Path.GetDirectoryName(Application.dataPath);

    [Test]
    public void E01_EscenasExistenEnProyecto()
    {
        string[] expected = { "SampleScene", "Act1", "BasicScene", "Tutorial" };
        foreach (var nombre in expected)
        {
            string path = Path.Combine(root, "Assets", "Scenes", nombre + ".unity");
            Assert.IsTrue(File.Exists(path), "La escena " + nombre + " no existe en Assets/Scenes/");
        }
    }

    [Test]
    public void E02_ArchivoYarnExiste()
    {
        string path = Path.Combine(root, "Assets", "_Project", "Yarn", "Marco.yarn");
        Assert.IsTrue(File.Exists(path), "No se encuentra Marco.yarn en Assets/_Project/Yarn/");
    }

    [Test]
    public void E03_TiposCrisisVRCompilan()
    {
        // Buscar tipos con nombre CrisisVR en los assemblies cargados
        var tipos = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("CrisisVR"));
        Assert.IsTrue(tipos.Count() > 0, "No se encontraron tipos bajo el namespace CrisisVR");
    }
}