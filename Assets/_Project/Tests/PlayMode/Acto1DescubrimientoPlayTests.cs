using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CrisisVR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Acto 1 (Acto1.unity): descubrimiento de la corrección por iniciativa propia. Recorre la escena en el Editor, sin visor,
// por los mismos puntos de entrada que usa el rayo XR (Act1ProjectReview.SelectSection/SelectObservation y los manejadores
// de Act1ProjectCorrection). CONTINUAR se simula con DialogueRunner.RequestNextLine. Es evidencia de Editor:
// no reemplaza la prueba en el Meta Quest 3.
// Los asmdef de prueba no pueden referenciar Assembly-CSharp, por eso los componentes se usan por reflexión.
public class Acto1DescubrimientoPlayTests
{
    const float TimeoutSeconds = 150f;

    sealed class Ctx
    {
        public MonoBehaviour flow, runner, review, correction, miguel;
        public Transform lid;
        public int logStart;
    }

    static MonoBehaviour Find(string typeName) =>
        UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(m => m.GetType().Name == typeName);

    static object Get(object target, string property) =>
        target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)?.GetValue(target);

    static bool Flag(object target, string property) => target != null && (bool)Get(target, property);

    static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance).Invoke(target, args);

    static void CallPrivate(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);

    static SessionData CurrentSession() =>
        (SessionData)AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name == "Assembly-CSharp")
            .Select(a => a.GetType("CrisisVR.SessionRuntime", false))
            .First(t => t != null)
            .GetProperty("Current").GetValue(null);

    static string[] Log()
    {
        var session = CurrentSession();
        if (session == null || session.eventLog == null) return new string[0];
        return ((IEnumerable)session.eventLog).Cast<object>().Select(o => o.ToString()).ToArray();
    }

    // Desde el último ACTO1_INICIO: así no se mezclan entradas de pruebas anteriores aunque la sesión se conserve o se reinicie.
    static string[] NewLog(Ctx c)
    {
        var all = Log();
        int start = Array.FindLastIndex(all, l => l.Contains("ACTO1_INICIO"));
        return start < 0 ? all : all.Skip(start).ToArray();
    }

    static bool Has(Ctx c, string eventName) => NewLog(c).Any(l => l.Contains(eventName));

    static IEnumerator Aislar()
    {
        foreach (var flowName in new[] { "Act1Flow", "Act2Flow", "Act3Flow", "ClosingFlow" })
        {
            var f = Find(flowName);
            if (f != null) UnityEngine.Object.Destroy(f);
        }
        var prevRunner = Find("DialogueRunner");
        if (prevRunner != null)
        {
            var running = prevRunner.GetType().GetProperty("IsDialogueRunning");
            if (running != null && (bool)running.GetValue(prevRunner)) prevRunner.GetType().GetMethod("Stop")?.Invoke(prevRunner, null);
            for (int i = 0; i < 120 && running != null && (bool)running.GetValue(prevRunner); i++) yield return null;
            yield return new WaitForSecondsRealtime(.6f);
        }
        yield return null;
        yield return null;
    }

    static IEnumerator CargarActo1(Ctx c)
    {
        yield return Aislar();
        SceneManager.LoadScene("Acto1");
        yield return null;
        yield return null;
        c.flow = Find("Act1Flow");
        c.runner = Find("DialogueRunner");
        c.review = Find("Act1ProjectReview");
        c.correction = Find("Act1ProjectCorrection");
        c.miguel = Find("Act1MiguelReturnToDesk");
        var hinge = Find("Act1LaptopHingeGrabTransformer");
        Assert.IsNotNull(c.flow, "Falta Act1Flow");
        Assert.IsNotNull(c.runner, "Falta DialogueRunner");
        Assert.IsNotNull(c.review, "Falta Act1ProjectReview en Acto1.unity");
        Assert.IsNotNull(c.correction, "Falta Act1ProjectCorrection");
        Assert.IsNotNull(c.miguel, "Falta Act1MiguelReturnToDesk en Miguel");
        Assert.IsNotNull(hinge, "Falta la tapa de la laptop");
        c.lid = hinge.transform;
        c.logStart = Log().Length;
    }

    // Entrega el proyecto y avanza el diálogo de Miguel hasta que el acto espera la decisión.
    static IEnumerator LlegarADecision(Ctx c)
    {
        var nextLine = c.runner.GetType().GetMethod("RequestNextLine");
        float start = Time.realtimeSinceStartup, nextAdvance = 0f;
        while (!Flag(c.flow, "AwaitingDecision"))
        {
            Assert.Less(Time.realtimeSinceStartup - start, TimeoutSeconds, "El acto no llegó a la espera de decisión.");
            if (Flag(c.flow, "AwaitingDelivery")) Call(c.flow, "Deliver");
            else if (Time.realtimeSinceStartup >= nextAdvance)
            {
                nextLine.Invoke(c.runner, null);
                nextAdvance = Time.realtimeSinceStartup + .25f;
            }
            yield return null;
        }
    }

    static IEnumerator HastaTerminar(Ctx c)
    {
        var nextLine = c.runner.GetType().GetMethod("RequestNextLine");
        float start = Time.realtimeSinceStartup, nextAdvance = 0f;
        while (!Flag(c.flow, "Finished"))
        {
            Assert.Less(Time.realtimeSinceStartup - start, TimeoutSeconds, "El acto no terminó.");
            if (Time.realtimeSinceStartup >= nextAdvance)
            {
                nextLine.Invoke(c.runner, null);
                nextAdvance = Time.realtimeSinceStartup + .25f;
            }
            yield return null;
        }
    }

    static IEnumerator EsperarFrames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static void AbrirTapa(Ctx c) => c.lid.localRotation = Quaternion.Euler(-100f, 0f, 0f);

    static bool ReviewVisible(Ctx c) => Flag(c.review, "PanelVisible");

    static bool CorrectionVisible(Ctx c)
    {
        var panel = c.correction.transform.Find("Panel");
        return panel != null && panel.gameObject.activeInHierarchy;
    }

    static string ReviewTexts(Ctx c) =>
        string.Join(" | ", c.review.GetComponentsInChildren<Component>(false)
            .Where(x => x != null && x.GetType().Name == "TextMeshPro")
            .Select(x => (string)x.GetType().GetProperty("text").GetValue(x)));

    // Comprobaciones comunes al llegar a la decisión: sin botón ni mensaje de corregir, Miguel se retira, la laptop muestra la revisión.
    static IEnumerator VerificarEstadoInicial(Ctx c)
    {
        var correctButton = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == "CORREGIR PROYECTO" && g.scene.IsValid());
        if (correctButton != null) Assert.IsFalse(correctButton.activeInHierarchy, "El botón CORREGIR PROYECTO no debe aparecer.");

        AbrirTapa(c);
        yield return EsperarFrames(3);
        Assert.IsTrue(ReviewVisible(c), "La laptop abierta debe mostrar el proyecto y su informe de evaluación.");
        Assert.IsFalse(CorrectionVisible(c), "La interfaz de corrección no debe verse antes de elegir un apartado observado.");
        Assert.AreEqual(-1, (int)Get(c.review, "OpenedSection"), "Debe verse el informe al inicio.");

        string texts = ReviewTexts(c);
        StringAssert.Contains("08 / 20", texts);
        foreach (var banned in new[] { "corregir", "reintentar", "mejorar" })
            StringAssert.DoesNotContain(banned, texts.ToLowerInvariant(), "La pantalla de revisión no debe inducir a corregir.");

        // Miguel termina de retirarse a su escritorio.
        float start = Time.realtimeSinceStartup;
        while (!Flag(c.miguel, "Arrived"))
        {
            Assert.Less(Time.realtimeSinceStartup - start, 20f, "Miguel no llegó a su escritorio.");
            yield return null;
        }
        Assert.IsTrue(Flag(c.miguel, "Started"));
        var deskPoint = GameObject.Find("Posicion_Escritorio_Miguel").transform.position;
        var miguelPos = c.miguel.transform.position;
        Assert.Less(Vector2.Distance(new Vector2(deskPoint.x, deskPoint.z), new Vector2(miguelPos.x, miguelPos.z)), .06f, "Miguel debe quedar en su escritorio.");
        Assert.IsTrue(Has(c, "ACTO1_MIGUEL_RETIRO_INICIO"));
        Assert.IsTrue(Has(c, "ACTO1_MIGUEL_EN_ESCRITORIO"));
        Assert.IsTrue(Has(c, "ACTO1_REVISION_ABIERTA"));
        Assert.IsFalse(Has(c, "ACTO1_CORRECCION_ABIERTA"), "La corrección no debe haberse abierto sola.");
    }

    // Recorrido 1: explora, no descubre la corrección y deja el proyecto como está.
    [UnityTest, Timeout(300000)]
    public IEnumerator R1_ExploraSinDescubrir_DejaElProyectoComoEsta()
    {
        var c = new Ctx();
        yield return CargarActo1(c);
        yield return LlegarADecision(c);
        yield return VerificarEstadoInicial(c);

        // Explora solo apartados sin observación; el estudiante nunca elige Diseño ni Pruebas.
        Call(c.review, "SelectSection", 0);
        yield return EsperarFrames(2);
        Assert.AreEqual(0, (int)Get(c.review, "OpenedSection"));
        Assert.IsFalse(Flag(c.review, "Discovered"));
        Assert.IsFalse(CorrectionVisible(c));
        Call(c.review, "ShowReport");
        Call(c.review, "SelectSection", 2);
        yield return EsperarFrames(2);
        Assert.IsFalse(Flag(c.review, "Discovered"));
        Assert.IsFalse(CorrectionVisible(c));
        Assert.AreEqual(3, (int)Get(c.review, "InteractionCount"));

        // Vence la espera sin que haya descubierto la corrección.
        Call(c.flow, "DebugExpire");
        yield return HastaTerminar(c);

        Assert.IsFalse(CurrentSession().Reintento, "Sin descubrir la corrección no hay reintento.");
        Assert.IsTrue(Has(c, "ACTO1_SIN_ACCION"));
        Assert.IsTrue(Has(c, "ACTO1_REVISION_APARTADO_EXPLORADO"));
        Assert.IsTrue(Has(c, "ACTO1_REVISION_SIN_DESCUBRIR"));
        Assert.IsTrue(Has(c, "ACTO1_REVISION_CIERRE"));
        Assert.IsFalse(Has(c, "ACTO1_REVISION_APARTADO_OBSERVADO_SELECCIONADO"));
        Assert.IsFalse(Has(c, "ACTO1_CORRECCION_ABIERTA"));
        Debug.Log("R1 registro:\n" + string.Join("\n", NewLog(c)));
    }

    // Recorrido 2: explora, descubre el apartado observado (Diseño) y presenta la corrección.
    [UnityTest, Timeout(300000)]
    public IEnumerator R2_DescubreDiseno_YPresentaLaCorreccion()
    {
        var c = new Ctx();
        yield return CargarActo1(c);
        yield return LlegarADecision(c);
        yield return VerificarEstadoInicial(c);

        Call(c.review, "SelectSection", 0);
        yield return EsperarFrames(2);
        Assert.IsFalse(CorrectionVisible(c));

        Call(c.review, "SelectSection", 1); // 2. Diseño: apartado observado
        yield return EsperarFrames(3);
        Assert.IsTrue(Flag(c.review, "Discovered"));
        Assert.IsFalse(ReviewVisible(c), "La revisión cede su lugar a la corrección.");
        Assert.IsTrue(CorrectionVisible(c), "Debe abrirse la interfaz de corrección existente.");

        // Mismo flujo que antes: elegir una respuesta por apartado y presentar.
        CallPrivate(c.correction, "OnOption", 0, 0);
        CallPrivate(c.correction, "OnOption", 1, 1);
        yield return EsperarFrames(2);
        CallPrivate(c.correction, "OnPresent");
        yield return HastaTerminar(c);

        Assert.IsTrue(CurrentSession().Reintento, "Presentar la corrección debe registrar el reintento.");
        Assert.IsTrue(Has(c, "ACTO1_ACCION_CORREGIR"));
        Assert.IsTrue(Has(c, "ACTO1_REVISION_APARTADO_OBSERVADO_SELECCIONADO"));
        Assert.IsTrue(Has(c, "ACTO1_CORRECCION_ABIERTA"));
        Assert.IsTrue(Has(c, "ACTO1_CORRECCION_COMPLETA"));
        Assert.IsTrue(Has(c, "ACTO1_CORRECCION_PRESENTADA"));
        Assert.IsFalse(Has(c, "ACTO1_REVISION_SIN_DESCUBRIR"));
        Debug.Log("R2 registro:\n" + string.Join("\n", NewLog(c)));
    }

    // Recorrido 3: descubre la corrección desde el informe (observación de Pruebas) pero no la presenta.
    [UnityTest, Timeout(300000)]
    public IEnumerator R3_DescubreDesdeElInforme_PeroNoPresenta()
    {
        var c = new Ctx();
        yield return CargarActo1(c);
        yield return LlegarADecision(c);
        yield return VerificarEstadoInicial(c);

        Call(c.review, "SelectObservation", 1); // observación de Pruebas
        yield return EsperarFrames(3);
        Assert.IsTrue(Flag(c.review, "Discovered"));
        Assert.IsTrue(CorrectionVisible(c));

        Call(c.flow, "DebugExpire");
        yield return HastaTerminar(c);

        Assert.IsFalse(CurrentSession().Reintento, "Sin presentar no hay reintento.");
        Assert.IsTrue(Has(c, "ACTO1_SIN_ACCION"));
        Assert.IsTrue(Has(c, "ACTO1_REVISION_OBSERVACION_ABIERTA"));
        Assert.IsTrue(Has(c, "ACTO1_CORRECCION_SIN_PRESENTAR"));
        Assert.IsFalse(Has(c, "ACTO1_CORRECCION_PRESENTADA"));
        Debug.Log("R3 registro:\n" + string.Join("\n", NewLog(c)));
    }
}
