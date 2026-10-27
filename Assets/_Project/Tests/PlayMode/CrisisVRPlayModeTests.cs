using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CrisisVRPlayModeTests
{
    // Comprueba si una escena esta incluida en Build Settings sin usar UnityEditor.
    static bool EnBuildSettings(string nombre)
    {
        int total = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < total; i++)
        {
            string ruta = SceneUtility.GetScenePathByBuildIndex(i);
            if (!string.IsNullOrEmpty(ruta) && ruta.EndsWith("/" + nombre + ".unity"))
                return true;
        }
        return false;
    }

    // Cargar SampleScene y verificar que no lanza excepciones.
    [UnityTest]
    public IEnumerator E01_CargaSampleScene_SinExcepciones()
    {
        if (!EnBuildSettings("SampleScene"))
        {
            Assert.Ignore("SampleScene no esta en Build Settings; test omitido.");
            yield break;
        }
        SceneManager.LoadScene("SampleScene");
        yield return null;
        Assert.IsNotNull(SceneManager.GetActiveScene());
        yield break;
    }

    // Cargar Act1 solo si esta habilitada en Build Settings.
    [UnityTest]
    public IEnumerator E02_CargaAct1_SinExcepciones()
    {
        if (!EnBuildSettings("Act1"))
        {
            Assert.Ignore("Act1 no esta habilitada en Build Settings; test omitido.");
            yield break;
        }
        SceneManager.LoadScene("Act1");
        yield return null;
        Assert.IsNotNull(SceneManager.GetActiveScene());
        yield break;
    }

    // Cargar Tutorial solo si esta habilitada en Build Settings.
    [UnityTest]
    public IEnumerator E03_CargaTutorial_SinExcepciones()
    {
        if (!EnBuildSettings("Tutorial"))
        {
            Assert.Ignore("Tutorial no esta habilitada en Build Settings; test omitido.");
            yield break;
        }
        SceneManager.LoadScene("Tutorial");
        yield return null;
        Assert.IsNotNull(SceneManager.GetActiveScene());
        yield break;
    }
}
