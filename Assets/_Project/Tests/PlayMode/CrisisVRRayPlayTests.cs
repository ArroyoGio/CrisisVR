using System.Collections;
using System.Linq;
using CrisisVR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Pruebas 1 y 2 de la guía por la vía real de XR Interaction Toolkit: el rayo del mando derecho apunta,
// el objeto cambia de color y el «gatillo» selecciona. El botón se simula con un lector de entrada falso
// (bypass del interactor), el mismo mecanismo que usan las pruebas de XRI; no reemplaza la prueba con mandos reales.
public class CrisisVRRayPlayTests
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    sealed class FakeButton : IXRInputButtonReader
    {
        public bool pressed;
        bool previous;
        public bool ReadIsPerformed() => pressed;
        public bool ReadWasPerformedThisFrame() { bool r = pressed && !previous; previous = pressed; return r; }
        public bool ReadWasCompletedThisFrame() => !pressed && previous;
        public float ReadValue() => pressed ? 1f : 0f;
        public bool TryReadValue(out float value) { value = ReadValue(); return true; }
    }

    static MonoBehaviour Find(string typeName) =>
        Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(m => m.GetType().Name == typeName);

    static bool Flag(object target, string property) => (bool)target.GetType().GetProperty(property).GetValue(target);
    static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);

    static SessionData Session() =>
        (SessionData)System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp")
            .GetType("CrisisVR.SessionRuntime").GetProperty("Current").GetValue(null);

    static Color ColorOf(GameObject root)
    {
        var renderer = root.transform.Find("Cuerpo").GetComponent<Renderer>();
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        return block.GetColor(BaseColorId);
    }

    static void Aim(Transform controller, Transform target)
    {
        Vector3 from = target.position + new Vector3(0f, .3f, .55f);
        controller.SetPositionAndRotation(from, Quaternion.LookRotation(target.position - from));
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator S07_RayoYGatillo_BotonActivoEInactivo()
    {
        SceneManager.LoadScene("Act1");
        yield return null;
        yield return null;
        var act1 = Find("Act1Flow");
        var act2 = Find("Act2Flow");
        var runner = Find("DialogueRunner");
        var nextLine = runner.GetType().GetMethod("RequestNextLine");

        // El gatillo es la acción de selección en los actos (BUG-011).
        // Sin visor, XRInputModalityManager apaga los mandos: se desactiva y se enciende el mando derecho.
        foreach (var manager in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(m => m.GetType().Name == "XRInputModalityManager"))
            manager.enabled = false;
        var interactor = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(i => i.transform.parent != null && i.transform.parent.name.Contains("Right"));
        Assert.IsNotNull(interactor, "No hay NearFarInteractor en el mando derecho.");
        for (var t = interactor.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
        yield return null;
        var selectAction = interactor.selectInput.inputActionReferencePerformed;
        Assert.IsNotNull(selectAction, "El interactor derecho no tiene acción de selección.");
        Assert.IsTrue(selectAction.action.bindings.Any(b => b.path.Contains("{TriggerButton}")),
            "La selección debe estar ligada al gatillo, no al grip: " + string.Join(", ", selectAction.action.bindings.Select(b => b.path)));

        // Llega al Acto 2 con la tablet visible pero todavía inactiva (antes de la pregunta).
        var tablet = ((XRSimpleInteractable)act2.GetType()
            .GetField("tabletInteractable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(act2)).gameObject;
        float start = Time.realtimeSinceStartup;
        while (tablet == null || !tablet.activeInHierarchy)
        {
            Assert.Less(Time.realtimeSinceStartup - start, 120f, "No apareció la tablet del Acto 2.");
            if (Flag(act1, "AwaitingDelivery")) Call(act1, "Deliver");
            else if (Flag(act1, "AwaitingDecision")) Call(act1, "Correct");
            else nextLine.Invoke(runner, null);
            yield return new WaitForSecondsRealtime(.25f);
        }

        // El mando se fija en una pose que apunta a la tablet (sin seguimiento del simulador).
        var controller = interactor.transform.parent;
        foreach (var driver in controller.GetComponents<MonoBehaviour>().Where(m => m.GetType().Name.Contains("TrackedPoseDriver")))
            driver.enabled = false;
        var tabletInteractable = tablet.GetComponent<XRSimpleInteractable>();
        var trigger = new FakeButton();
        interactor.selectInput.bypass = trigger;

        // Prueba 2: botón inactivo (gris). Apuntar y presionar no hace nada.
        Aim(controller, tablet.transform);
        for (int i = 0; i < 10; i++) yield return null;
        Assert.IsFalse(Flag(act2, "AwaitingChoice"), "La tablet todavía no debía estar activa.");
        Color inactive = ColorOf(tablet);
        Assert.Less(Mathf.Abs(inactive.r - inactive.b), .1f, "Inactiva debe verse gris: " + inactive);
        trigger.pressed = true;
        for (int i = 0; i < 5; i++) yield return null;
        trigger.pressed = false;
        yield return null;
        Assert.IsFalse(tabletInteractable.isSelected, "Un objeto inactivo no debe seleccionarse.");
        Assert.AreEqual(0, Session().OpcionAdaptabilidad, "Seleccionar un objeto inactivo no debe registrar nada.");

        // Avanza hasta la pregunta del Acto 2 sin tocar las opciones.
        start = Time.realtimeSinceStartup;
        while (!Flag(act2, "AwaitingChoice"))
        {
            Assert.Less(Time.realtimeSinceStartup - start, 120f, "El Acto 2 no llegó a la decisión.");
            nextLine.Invoke(runner, null);
            yield return new WaitForSecondsRealtime(.25f);
        }

        // Prueba 1: botón activo. Al apuntar se vuelve celeste; el gatillo lo selecciona y se registra la decisión.
        Aim(controller, tablet.transform);
        for (int i = 0; i < 10; i++) yield return null;
        Assert.IsTrue(tabletInteractable.isHovered, "El rayo del mando derecho debe apuntar a la tablet.");
        Color hover = ColorOf(tablet);
        Assert.Greater(hover.b, hover.r + .3f, "Al apuntar debe verse celeste: " + hover);
        trigger.pressed = true;
        start = Time.realtimeSinceStartup;
        while (Flag(act2, "AwaitingChoice"))
        {
            Assert.Less(Time.realtimeSinceStartup - start, 5f, "El gatillo no seleccionó la tablet.");
            yield return null;
        }
        trigger.pressed = false;
        Assert.AreEqual(1, Session().OpcionAdaptabilidad, "La tablet debe registrar el plan nuevo (1).");
        interactor.selectInput.bypass = null;
        // Aislamiento: no dejar un diálogo a medias para la prueba siguiente.
        runner.GetType().GetMethod("Stop").Invoke(runner, null);
        for (int i = 0; i < 5; i++) yield return null;
    }
}
