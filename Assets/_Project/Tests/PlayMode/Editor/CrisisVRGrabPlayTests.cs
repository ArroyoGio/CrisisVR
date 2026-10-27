using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

// Boton falso para simular la pulsacion del grip (mismo mecanismo que usa XRI en sus pruebas).
class FakeGrip : IXRInputButtonReader
{
    public bool pressed;
    public FakeGrip(bool p) { pressed = p; }
    public bool ReadIsPerformed() => pressed;
    public bool ReadWasPerformedThisFrame() => pressed;
    public bool ReadWasCompletedThisFrame() => !pressed;
    public float ReadValue() => pressed ? 1f : 0f;
    public bool TryReadValue(out float value) { value = ReadValue(); return true; }
}

// Ejecuta el tutorial en Play con el simulador XR montado y comprueba si el grip
// del mando agarra el cubo y si el cubo se puede mover.
public class CrisisVRGrabPlayTests
{
    const string SimPrefab =
        "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab";

    static bool EnBuildSettings(string nombre)
    {
        int total = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < total; i++)
        {
            string ruta = SceneUtility.GetScenePathByBuildIndex(i);
            if (!string.IsNullOrEmpty(ruta) && ruta.EndsWith("/" + nombre + ".unity")) return true;
        }
        return false;
    }

    static object GetField(object obj, string nombre)
    {
        var f = obj.GetType().GetField(nombre, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? f.GetValue(obj) : null;
    }

    static void Log(string s) { Debug.Log("G01 " + s); }

    [UnityTest]
    public IEnumerator G01_AgarreConSimulador()
    {
        if (!EnBuildSettings("Tutorial")) { Assert.Ignore("Tutorial no esta en Build Settings."); yield break; }
        SceneManager.LoadScene("Tutorial");
        yield return null;
        yield return null;

        // ---- Fase 1: estado inicial SIN simulador (así lo ve la desarrolladora hoy).
        var mandos0 = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tr in mandos0)
            if (tr.name == "Left Controller" || tr.name == "Right Controller")
                Log("SIN SIMULADOR mando '" + tr.name + "' activoEnJerarquia=" + tr.gameObject.activeInHierarchy);

        // ---- Montar el simulador XR.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimPrefab);
        Assert.IsNotNull(prefab, "No se encontro el prefab del simulador.");
        var sim = Object.Instantiate(prefab);
        sim.name = "XR Interaction Simulator (test)";
        yield return null;
        yield return new WaitForSeconds(1.5f);
        Log("simulador instanciado, componentes=" + sim.GetComponentsInChildren<MonoBehaviour>(true).Length);

        // ---- Fase 2: estado de mandos yractores CON simulador.
        var interactores = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Log("nearfar interactores=" + interactores.Length);
        foreach (var it in interactores)
            Log("interactor '" + it.gameObject.name + "' actInHier=" + it.gameObject.activeInHierarchy +
                " enabled=" + it.enabled + " selectRef=" +
                (it.selectInput != null && it.selectInput.inputActionReferencePerformed != null
                    ? it.selectInput.inputActionReferencePerformed.name : "sin accion"));

        // ---- Fase 3: llevar el flujo al paso Grab.
        MonoBehaviour flow = null;
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (mb.GetType().Name == "TutorialFlow") { flow = mb; break; }
        Assert.IsNotNull(flow, "No hay TutorialFlow en la escena.");

        var exercises = (System.Array)GetField(flow, "exercises");
        Assert.IsNotNull(exercises, "TutorialFlow.exercises vacio.");
        int idxGrab = -1;
        object pasoGrab = null;
        for (int i = 0; i < exercises.Length; i++)
        {
            object st = GetField(exercises.GetValue(i), "step");
            if (st != null && st.ToString() == "Grab") { idxGrab = i; pasoGrab = st; }
        }
        Log("ejercicios=" + exercises.Length + " indiceGrab=" + idxGrab);
        Assert.IsTrue(idxGrab >= 0, "No hay ningun ejercicio con paso Grab.");

        var tFlow = flow.GetType();
        tFlow.GetField("index", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(flow, idxGrab);
        var mShow = tFlow.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        mShow.Invoke(flow, null);
        yield return null;
        yield return new WaitForSeconds(0.3f);
        Log("paso actual=" + GetField(flow, "index"));

        // ---- Fase 4: estado del cubo tras activar el paso Grab.
        var grabs = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Log("grabs=" + grabs.Length);
        XRGrabInteractable cubo = null;
        foreach (var g in grabs)
        {
            Log("grab '" + g.gameObject.name + "' enabled=" + g.enabled + " actInHier=" + g.gameObject.activeInHierarchy +
                " colliders=" + g.colliders.Count);
            if (cubo == null && g.enabled) cubo = g;
        }
        Assert.IsNotNull(cubo, "No hay ningun XRGrabInteractable activo tras el paso Grab.");

        // ---- Fase 5: pulsar el GRIP por la via real del input del interactor.
        NearFarInteractor interactor = null;
        foreach (var it in interactores)
            if (it.gameObject.activeInHierarchy) { interactor = it; break; }
        if (interactor == null)
        {
            Log("ningun interactor activo: se activa el primero");
            interactor = interactores[0];
            try { interactor.gameObject.SetActive(true); }
            catch (System.Exception e) { Log("excepcion al activar el interactor: " + e.Message); }
            yield return null;
        }
        Assert.IsNotNull(interactor, "No hay NearFarInteractor.");
        Log("interactor usado '" + interactor.gameObject.name + "' actInHier=" + interactor.gameObject.activeInHierarchy);

        // Colocar el mando junto al cubo para que el hover sea valido.
        interactor.transform.position = cubo.transform.position + new Vector3(0f, 0f, 0.15f);
        interactor.transform.rotation = Quaternion.LookRotation(cubo.transform.position - interactor.transform.position);
        yield return null;
        yield return new WaitForSeconds(0.3f);
        Log("hover tras colocar mando: interactoresConHover=" + interactor.interactablesHovered.Count);

        var bypass = new FakeGrip(true);
        if (interactor.selectInput != null)
        {
            interactor.selectInput.bypass = bypass;
            Log("bypass de grip aplicado a selectInput del interactor");
        }
        else Log("el interactor no tiene selectInput");

        yield return null;
        yield return new WaitForSeconds(0.4f);

        bool agarrado = cubo.isSelected;
        Log("RESULTADO agarrado=" + agarrado + " hovered=" + interactor.interactablesHovered.Count);

        // ---- Fase 6: mover el mando y comprobar que el cubo le sigue.
        Vector3 antes = cubo.transform.position;
        interactor.transform.position += new Vector3(0.3f, 0.1f, 0f);
        yield return null;
        yield return new WaitForSeconds(0.5f);
        Vector3 despues = cubo.transform.position;
        Log("mover mando: cubo antes=" + antes + " despues=" + despues + " movido=" + (antes != despues));

        Assert.IsTrue(agarrado, "El cubo NO fue agarrado por el grip.");
    }
}
