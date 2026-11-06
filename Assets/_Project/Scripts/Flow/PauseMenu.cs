using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;

namespace CrisisVR
{
    // Botón Menú del control izquierdo (o Escape en el Editor): pausa los actos.
    // CONTINUAR reanuda en el mismo punto; TERMINAR SESIÓN escribe un reporte «Interrumpida»
    // y vuelve a la pantalla inicial con un código de sesión nuevo.
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button continueButton;
        [SerializeField] Button endSessionButton;
        [SerializeField] Canvas[] hideWhilePaused;
        [SerializeField] string firstSceneName = "MainMenu";
        // En el tutorial la pausa es práctica: no escribe PAUSA/REANUDACION en la sesión (nada se guarda).
        [SerializeField] bool logPauseToSession = true;

        InputAction menuAction;
        bool ending;
        public bool AllowPause { get; set; } = true;
        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            GamePause.Reset();
            if (panel != null) panel.SetActive(false);
            if (continueButton != null) continueButton.onClick.AddListener(Resume);
            if (endSessionButton != null) endSessionButton.onClick.AddListener(EndSession);

            menuAction = new InputAction("CrisisVR Pausa", InputActionType.Button);
            menuAction.AddBinding("<XRController>{LeftHand}/{MenuButton}");
            menuAction.AddBinding("<XRController>{LeftHand}/menu");
            menuAction.AddBinding("<XRController>{LeftHand}/menuButton");
            menuAction.AddBinding("<Keyboard>/escape");
            menuAction.performed += _ => Toggle();
        }

        void OnEnable() => menuAction?.Enable();
        void OnDisable() => menuAction?.Disable();

        void OnDestroy()
        {
            menuAction?.Dispose();
            GamePause.Reset();
        }

        public void Toggle()
        {
            if (ending) return;
            if (IsOpen) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (!AllowPause || IsOpen) return;
            GamePause.SetPaused(true);
            SetHiddenCanvases(false);
            if (panel != null) panel.SetActive(true);
            if (logPauseToSession) SessionRuntime.LogEvent("PAUSA");
        }

        public void Resume()
        {
            if (ending || !IsOpen) return;
            if (panel != null) panel.SetActive(false);
            SetHiddenCanvases(true);
            GamePause.SetPaused(false);
            if (logPauseToSession) SessionRuntime.LogEvent("REANUDACION");
        }

        public void EndSession()
        {
            if (ending) return;
            ending = true;
            if (continueButton != null) continueButton.interactable = false;
            if (endSessionButton != null) endSessionButton.interactable = false;
            SessionRuntime.LogEvent("TERMINAR_SESION");
            SessionRuntime.Interrupt();
            // Se detiene el diálogo antes de salir: si no, el fundido interno de Yarn sigue sobre objetos ya destruidos.
            var runner = FindAnyObjectByType<DialogueRunner>();
            if (runner != null && runner.IsDialogueRunning) _ = runner.Stop();
            // El juego sigue en pausa durante el fundido: ningún acto avanza ni registra nada.
            // SceneFader quita la pausa al cargar MainMenu, que crea la sesión nueva con código nuevo.
            GamePause.SetPaused(true);
            if (Application.CanStreamedLevelBeLoaded(firstSceneName))
                SceneFader.LoadScene(firstSceneName);
            else
                Debug.LogError("No se encontró la escena inicial: " + firstSceneName);
        }

        void SetHiddenCanvases(bool visible)
        {
            if (hideWhilePaused == null) return;
            foreach (var canvas in hideWhilePaused)
                if (canvas != null) canvas.enabled = visible;
        }
    }
}
