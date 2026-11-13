using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace CrisisVR
{
    // Equivale al GameManager de la guía v1.0 para la escena de los actos: encadena Acto 1 → 2 → 3 → cierre,
    // siempre en ese orden y sin que ninguna decisión bloquee el avance.
    public sealed class ActsDirector : MonoBehaviour
    {
        const float WalkSpeed = 4f;
        [SerializeField] Act1Flow act1;
        [SerializeField] Act2Flow act2;
        [SerializeField] Act3Flow act3;
        [SerializeField] ClosingFlow closing;
        [SerializeField] PauseMenu pauseMenu;
        // Respuesta de la tesista (27/10/2026): se mantiene el desplazamiento con joystick y el giro por pasos (T-23).
        // Ningún acto obliga a moverse; esta opción solo existe por si la prueba de confort aconseja desactivarlo.
        [Tooltip("Desactiva desplazamiento y giro en los actos (estudiante sentado).")]
        [SerializeField] bool seated = false;

        void Awake()
        {
            GamePause.Reset();
            foreach (var provider in FindObjectsByType<ContinuousMoveProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                provider.moveSpeed = WalkSpeed;
            if (act1 == null) act1 = FindAnyObjectByType<Act1Flow>();
            if (act2 == null) act2 = FindAnyObjectByType<Act2Flow>();
            if (act3 == null) act3 = FindAnyObjectByType<Act3Flow>();
            if (closing == null) closing = FindAnyObjectByType<ClosingFlow>();
            if (pauseMenu == null) pauseMenu = FindAnyObjectByType<PauseMenu>();
            if (seated) DisableLocomotion();
            UseTriggerToSelect();
        }

        // Guía §6: el gatillo selecciona. En el rig de XRI la acción «Select» es el grip y el gatillo es «UI Press»,
        // así que los botones 3D de los actos no respondían al gatillo (BUG-011). Igual que en MainMenuController.
        static void UseTriggerToSelect()
        {
            foreach (var interactor in FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (interactor.uiPressInput != null) interactor.selectInput = interactor.uiPressInput;
        }

        void OnEnable()
        {
            if (act1 != null) act1.ActCompleted += StartAct2;
            if (act2 != null) act2.ActCompleted += StartAct3;
            if (act3 != null) act3.ActCompleted += StartClosing;
        }

        void OnDisable()
        {
            if (act1 != null) act1.ActCompleted -= StartAct2;
            if (act2 != null) act2.ActCompleted -= StartAct3;
            if (act3 != null) act3.ActCompleted -= StartClosing;
        }

        void StartAct2()
        {
            if (act2 != null) act2.Begin();
        }

        void StartAct3()
        {
            if (act3 != null) act3.Begin();
        }

        void StartClosing()
        {
            // En el cierre ya no hay nada que pausar: la sesión quedó completa y el reporte escrito.
            if (pauseMenu != null) pauseMenu.AllowPause = false;
            // La tarjeta de salida del Acto 3 queda en negro: se retira para mostrar el cierre.
            var card = FindAnyObjectByType<Act1TransitionCard>();
            if (card != null) card.Clear();
            if (closing != null) closing.Begin();
        }

        static void DisableLocomotion()
        {
            foreach (var provider in FindObjectsByType<ContinuousMoveProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) provider.enabled = false;
            foreach (var provider in FindObjectsByType<ContinuousTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) provider.enabled = false;
            foreach (var provider in FindObjectsByType<SnapTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) provider.enabled = false;
        }
    }
}
