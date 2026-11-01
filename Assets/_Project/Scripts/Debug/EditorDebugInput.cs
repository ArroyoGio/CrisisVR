#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

namespace CrisisVR
{
    // Teclas para recorrer los actos sin visor (guía v1.0 §4, forma 1). Usan el mismo punto de decisión
    // que el rayo XR, así que lo registrado y el reporte son idénticos. No existe en builds de producción.
    //   F1 ENTREGAR · F2 CORREGIR PROYECTO · F3 tablet (plan nuevo) · F4 carpeta (plan original)
    //   F5–F8 opciones A–D · F9 vencer la espera actual · F10 siguiente línea de diálogo · Esc pausa
    public sealed class EditorDebugInput : MonoBehaviour
    {
        [SerializeField] Act1Flow act1;
        [SerializeField] Act2Flow act2;
        [SerializeField] Act3Flow act3;
        [SerializeField] DialogueRunner dialogueRunner;

        void Awake()
        {
            if (act1 == null) act1 = FindAnyObjectByType<Act1Flow>();
            if (act2 == null) act2 = FindAnyObjectByType<Act2Flow>();
            if (act3 == null) act3 = FindAnyObjectByType<Act3Flow>();
            if (dialogueRunner == null) dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame && act1 != null) act1.Deliver();
            if (keyboard.f2Key.wasPressedThisFrame && act1 != null) act1.Correct();
            if (keyboard.f3Key.wasPressedThisFrame && act2 != null) act2.Choose(DecisionCodes.Adapt_PlanNuevo);
            if (keyboard.f4Key.wasPressedThisFrame && act2 != null) act2.Choose(DecisionCodes.Adapt_PlanOriginal);
            if (keyboard.f5Key.wasPressedThisFrame && act3 != null) act3.Choose(DecisionCodes.Team_A);
            if (keyboard.f6Key.wasPressedThisFrame && act3 != null) act3.Choose(DecisionCodes.Team_B);
            if (keyboard.f7Key.wasPressedThisFrame && act3 != null) act3.Choose(DecisionCodes.Team_C);
            if (keyboard.f8Key.wasPressedThisFrame && act3 != null) act3.Choose(DecisionCodes.Team_D);
            if (keyboard.f9Key.wasPressedThisFrame && !GamePause.IsPaused) ExpireCurrentWait();
            if (keyboard.f10Key.wasPressedThisFrame && dialogueRunner != null && !GamePause.IsPaused) dialogueRunner.RequestNextLine();
        }

        void ExpireCurrentWait()
        {
            if (act1 != null && act1.AwaitingDecision) act1.DebugExpire();
            else if (act2 != null && act2.AwaitingChoice) act2.DebugExpire();
            else if (act3 != null && act3.AwaitingChoice) act3.DebugExpireReminder();
        }
    }
}
#endif
