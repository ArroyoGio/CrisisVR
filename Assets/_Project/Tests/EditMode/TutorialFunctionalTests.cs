using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Pruebas funcionales del tutorial (Arrange-Act-Assert).
// Los asmdef de test no pueden referenciar Assembly-CSharp, por eso se accede
// a los tipos de produccion por reflexion en lugar de duplicar logica.
public class TutorialFunctionalTests
{
    const string Runtime = "Assembly-CSharp";
    const string Editor = "Assembly-CSharp-Editor";
    const string Tmp = "Unity.TextMeshPro";

    static Assembly Ensamblado(string nombre)
    {
        var a = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(x => x.GetName().Name == nombre);
        Assert.IsNotNull(a, "Ensamblado no cargado: " + nombre);
        return a;
    }

    static Type Tipo(string ensamblado, string nombreCompleto)
    {
        var t = Ensamblado(ensamblado).GetType(nombreCompleto, false);
        Assert.IsNotNull(t, "Tipo no encontrado: " + nombreCompleto);
        return t;
    }

    static object Campo(object instancia, string nombre)
    {
        var f = instancia.GetType().GetField(nombre, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, "Campo no encontrado: " + nombre);
        return f.GetValue(instancia);
    }

    // T01: el enum de pasos es unico y completo (11 pasos, sin duplicados).
    [Test]
    public void T01_PasosDelTutorial_SonUnicosYCompletos()
    {
        // Arrange
        var tipoPaso = Tipo(Runtime, "CrisisVR.TutorialStep");

        // Act
        var valores = Enum.GetValues(tipoPaso).Cast<object>().Select(v => v.ToString()).ToArray();

        // Assert
        Assert.AreEqual(11, valores.Length, "El tutorial debe declarar 11 pasos.");
        CollectionAssert.AllItemsAreUnique(valores, "Hay pasos duplicados en TutorialStep.");
        Assert.IsTrue(valores.Contains("Finished"), "Debe existir el paso Finished.");
    }

    // T02: el orden de la secuencia cubre todos los pasos exactamente una vez.
    [Test]
    public void T02_OrdenDeLaSecuencia_CubreTodosLosPasos()
    {
        // Arrange
        var tipoSecuencia = Tipo(Editor, "CrisisVR.Editor.TutorialSequenceUpgrade");
        var campoOrden = tipoSecuencia.GetField("StepOrder", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(campoOrden, "No existe TutorialSequenceUpgrade.StepOrder.");
        var tipoPaso = Tipo(Runtime, "CrisisVR.TutorialStep");

        // Act
        var orden = ((Array)campoOrden.GetValue(null)).Cast<object>().Select(o => o.ToString()).ToArray();
        var declarados = Enum.GetNames(tipoPaso);

        // Assert
        CollectionAssert.AreEquivalent(declarados, orden,
            "StepOrder debe contener cada paso del enum una sola vez.");
        Assert.AreEqual("Finished", orden[orden.Length - 1], "Finished debe ser el ultimo paso.");
    }

    // T03: el ejercicio conserva titulo, instruccion y ayuda.
    [Test]
    public void T03_Ejercicio_ConservaSusTextos()
    {
        // Arrange
        var tipoEjercicio = Tipo(Runtime, "CrisisVR.TutorialExercise");
        var ejercicio = Activator.CreateInstance(tipoEjercicio);

        // Act
        tipoEjercicio.GetField("title").SetValue(ejercicio, "Titulo de prueba");
        tipoEjercicio.GetField("instruction").SetValue(ejercicio, "Instruccion de prueba");
        tipoEjercicio.GetField("help").SetValue(ejercicio, "Ayuda de prueba");

        // Assert
        Assert.AreEqual("Titulo de prueba", Campo(ejercicio, "title"));
        StringAssert.Contains("prueba", (string)Campo(ejercicio, "instruction"));
        Assert.IsNotEmpty((string)Campo(ejercicio, "help"));
    }

    // T04: el panel muestra el avance como "Paso X de Y" sin valorar al estudiante.
    [Test]
    public void T04_Panel_MuestraElPasoActual()
    {
        // Arrange
        var tipoPanel = Tipo(Runtime, "CrisisVR.TutorialPanel");
        var tipoTexto = Tipo(Tmp, "TMPro.TextMeshProUGUI");
        var raiz = new GameObject("PanelPrueba");
        var panel = raiz.AddComponent(tipoPanel);
        var texto = new GameObject("Texto").AddComponent(tipoTexto);
        texto.transform.SetParent(raiz.transform, false);
        tipoPanel.GetField("heading").SetValue(panel, texto);
        tipoPanel.GetField("hand").SetValue(panel, texto);

        // Act
        tipoPanel.GetMethod("SetProgress", new[] { typeof(int), typeof(int) })
            .Invoke(panel, new object[] { 2, 10 });

        // Assert
        var progreso = Campo(panel, "progress");
        Assert.IsNotNull(progreso, "El panel debe crear el texto de progreso.");
        Assert.AreEqual("Paso 3 de 10", progreso.GetType().GetProperty("text").GetValue(progreso));

        UnityEngine.Object.DestroyImmediate(raiz);
    }

    // T05: un panel con referencias sin asignar no lanza excepcion ni bloquea el avance.
    [Test]
    public void T05_Panel_SinReferencias_NoBloquea()
    {
        // Arrange
        var tipoPanel = Tipo(Runtime, "CrisisVR.TutorialPanel");
        var tipoEjercicio = Tipo(Runtime, "CrisisVR.TutorialExercise");
        var tipoPaso = Tipo(Runtime, "CrisisVR.TutorialStep");
        var tipoTexto = Tipo(Tmp, "TMPro.TextMeshProUGUI");
        var ejercicio = Activator.CreateInstance(tipoEjercicio);
        tipoEjercicio.GetField("step").SetValue(ejercicio,
            Enum.Parse(tipoPaso, "Movement"));

        var raiz = new GameObject("PanelIncompleto");
        var panel = raiz.AddComponent(tipoPanel);
        var pista = new GameObject("Pista").AddComponent(tipoTexto);
        pista.transform.SetParent(raiz.transform, false);
        pista.GetType().GetProperty("text").SetValue(pista, "texto anterior");
        tipoPanel.GetField("hint").SetValue(panel, pista);

        var mostrar = tipoPanel.GetMethod("Show",
            new[] { tipoEjercicio, typeof(bool), typeof(bool), typeof(int), typeof(int) });

        // Act
        mostrar.Invoke(panel, new object[] { ejercicio, true, false, 0, 3 });

        // Assert
        Assert.DoesNotThrow(() => mostrar.Invoke(panel, new object[] { ejercicio, false, true, 1, 3 }),
            "Mostrar un paso no debe fallar aunque falten referencias.");
        Assert.AreEqual(string.Empty, (string)pista.GetType().GetProperty("text").GetValue(pista),
            "La pista debe limpiarse al cambiar de paso.");

        UnityEngine.Object.DestroyImmediate(raiz);
    }
}
