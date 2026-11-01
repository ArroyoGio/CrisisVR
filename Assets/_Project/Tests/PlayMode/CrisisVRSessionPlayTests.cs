using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CrisisVR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Recorre la escena de los actos de principio a fin en el Editor, sin visor, por el mismo punto de decisión
// que usa el rayo XR. CONTINUAR se simula con DialogueRunner.RequestNextLine. Es evidencia de Editor:
// no reemplaza la prueba en el Meta Quest 3.
// Los asmdef de prueba no pueden referenciar Assembly-CSharp, por eso los flujos se usan por reflexión.
public class CrisisVRSessionPlayTests
{
    const float TimeoutSeconds = 240f;

    static Type RuntimeType(string name)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name == "Assembly-CSharp")
            .Select(a => a.GetType("CrisisVR." + name, false))
            .FirstOrDefault(t => t != null);
        Assert.IsNotNull(type, "Tipo no encontrado: CrisisVR." + name);
        return type;
    }

    static MonoBehaviour Find(string typeName) =>
        UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(m => m.GetType().Name == typeName);

    static object Get(object target, string property) =>
        target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)?.GetValue(target);

    static bool Flag(object target, string property) => target != null && (bool)Get(target, property);

    static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance).Invoke(target, args);

    static SessionData CurrentSession() =>
        (SessionData)RuntimeType("SessionRuntime").GetProperty("Current").GetValue(null);

    static string LastReportPath() =>
        (string)RuntimeType("SessionRuntime").GetProperty("LastReportPath").GetValue(null);

    // act1Choice: true = CORREGIR, false = dejar vencer la espera. act2Choice: 1, 2 o 3 (3 = vencer espera). act3Choice: 1..4.
    IEnumerator RunSession(bool act1Correct, int act2Choice, int act3Choice)
    {
        SceneManager.LoadScene("Act1");
        yield return null;
        yield return null;
        var act1 = Find("Act1Flow");
        var act2 = Find("Act2Flow");
        var act3 = Find("Act3Flow");
        var closing = Find("ClosingFlow");
        var runner = Find("DialogueRunner");
        Assert.IsNotNull(act1, "Falta Act1Flow");
        Assert.IsNotNull(act2, "Falta Act2Flow: ejecuta CrisisVR/Actos 2 y 3/Preparar.");
        Assert.IsNotNull(act3, "Falta Act3Flow");
        Assert.IsNotNull(closing, "Falta ClosingFlow");
        Assert.IsNotNull(runner, "Falta DialogueRunner");
        var nextLine = runner.GetType().GetMethod("RequestNextLine");

        float start = Time.realtimeSinceStartup;
        float nextAdvance = 0f;
        while (!Flag(closing, "Started"))
        {
            Assert.Less(Time.realtimeSinceStartup - start, TimeoutSeconds, "La sesión no llegó al cierre.");
            if (Flag(act1, "AwaitingDelivery")) Call(act1, "Deliver");
            else if (Flag(act1, "AwaitingDecision")) Call(act1, act1Correct ? "Correct" : "DebugExpire");
            else if (Flag(act2, "AwaitingChoice"))
            {
                if (act2Choice == 3) Call(act2, "DebugExpire");
                else Call(act2, "Choose", act2Choice);
            }
            else if (Flag(act3, "AwaitingChoice")) Call(act3, "Choose", act3Choice);
            else if (Time.realtimeSinceStartup >= nextAdvance)
            {
                // Simula que el estudiante presiona CONTINUAR al terminar de leer cada línea.
                nextLine.Invoke(runner, null);
                nextAdvance = Time.realtimeSinceStartup + .25f;
            }
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator S01_SesionCompleta_Corregir_PlanNuevo_A()
    {
        yield return RunSession(true, 1, 1);
        var session = CurrentSession();
        Assert.AreEqual("Completa", session.endReason);
        Assert.IsTrue(session.Reintento);
        Assert.AreEqual(1, session.OpcionAdaptabilidad);
        Assert.AreEqual(1, session.OpcionTrabajoEnEquipo);
        Assert.IsTrue(session.act1Completed && session.act2Completed && session.act3Completed);

        string path = LastReportPath();
        Assert.IsTrue(File.Exists(path), "No se escribió el reporte: " + path);
        string report = File.ReadAllText(path, new UTF8Encoding(false));
        StringAssert.Contains("Estado de la sesión: Completa", report);
        StringAssert.Contains("seleccionó la opción de corregir el proyecto", report);
        StringAssert.Contains("seleccionó el plan nuevo (práctica virtual)", report);
        StringAssert.Contains("«Propongo que cada uno diga qué parte puede entregar y para cuándo, y armamos un plan juntos.»", report);
        StringAssert.DoesNotContain("Sin registro", report);
        StringAssert.DoesNotContain("ACTO1_", report);
        Debug.Log("S01 reporte: " + path);
    }

    [UnityTest]
    public IEnumerator S02_SesionCompleta_NoActua_SinAccion_D()
    {
        yield return RunSession(false, 3, 4);
        var session = CurrentSession();
        Assert.AreEqual("Completa", session.endReason);
        Assert.IsFalse(session.Reintento);
        Assert.AreEqual(3, session.OpcionAdaptabilidad);
        Assert.AreEqual(4, session.OpcionTrabajoEnEquipo);
        string report = File.ReadAllText(LastReportPath(), new UTF8Encoding(false));
        StringAssert.Contains("no seleccionó ninguna acción durante el tiempo disponible", report);
        StringAssert.Contains("no seleccionó ninguna opción durante el tiempo disponible", report);
        StringAssert.Contains("«Prefiero esperar a ver qué deciden ustedes.»", report);
        Debug.Log("S02 reporte: " + LastReportPath());
    }

    // Pausa durante la espera de 45 s del Acto 1: la espera no avanza y la decisión queda pendiente.
    // Luego TERMINAR SESIÓN en el Acto 2 deja un reporte «Interrumpida» con la Situación 3 sin registro.
    [UnityTest]
    public IEnumerator S03_PausaCongelaEspera_Y_TerminarSesionEnActo2()
    {
        SceneManager.LoadScene("Act1");
        yield return null;
        yield return null;
        var act1 = Find("Act1Flow");
        var act2 = Find("Act2Flow");
        var pause = Find("PauseMenu");
        var runner = Find("DialogueRunner");
        Assert.IsNotNull(pause, "Falta PauseMenu");
        var nextLine = runner.GetType().GetMethod("RequestNextLine");

        float start = Time.realtimeSinceStartup;
        while (!Flag(act1, "AwaitingDecision"))
        {
            Assert.Less(Time.realtimeSinceStartup - start, TimeoutSeconds, "El Acto 1 no llegó a la decisión.");
            if (Flag(act1, "AwaitingDelivery")) Call(act1, "Deliver");
            else nextLine.Invoke(runner, null);
            yield return new WaitForSecondsRealtime(.25f);
        }

        var timer = act1.GetType().GetField("decisionTimer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(act1);
        Call(pause, "Pause");
        float before = (float)Get(timer, "Remaining");
        yield return new WaitForSecondsRealtime(2f);
        float after = (float)Get(timer, "Remaining");
        Assert.AreEqual(before, after, 1e-4f, "La espera avanzó durante la pausa.");
        Assert.IsTrue(Flag(act1, "AwaitingDecision"), "La decisión se resolvió durante la pausa.");
        Call(act1, "Correct"); // ignorado: está en pausa
        Assert.IsTrue(Flag(act1, "AwaitingDecision"), "Una selección en pausa no debe registrarse.");
        Call(pause, "Resume");
        Call(act1, "Correct");

        start = Time.realtimeSinceStartup;
        while (!Flag(act2, "AwaitingChoice"))
        {
            Assert.Less(Time.realtimeSinceStartup - start, TimeoutSeconds, "El Acto 2 no llegó a la decisión.");
            nextLine.Invoke(runner, null);
            yield return new WaitForSecondsRealtime(.25f);
        }

        Call(pause, "Pause");
        Call(pause, "EndSession");
        var session = CurrentSession();
        // EndSession ya creó una sesión nueva; el reporte interrumpido es el último escrito.
        string report = File.ReadAllText(LastReportPath() ?? string.Empty, new UTF8Encoding(false));
        StringAssert.Contains("Estado de la sesión: Interrumpida", report);
        StringAssert.Contains("seleccionó la opción de corregir el proyecto", report);
        StringAssert.Contains("SITUACIÓN 3 — Reunión de equipo", report);
        StringAssert.Contains("Sin registro: la sesión terminó antes de esta situación.", report);
        Assert.AreNotEqual(null, session);
        start = Time.realtimeSinceStartup;
        while (SceneManager.GetActiveScene().name != "MainMenu" || Time.timeScale == 0f)
        {
            Assert.Less(Time.realtimeSinceStartup - start, 30f, "TERMINAR SESIÓN debe volver a la pantalla inicial y quitar la pausa.");
            yield return null;
        }
    }
}
