using UnityEngine;
using UnityEngine.Events;

namespace CrisisVR
{
    // Tutorial secuencial: cada paso se desbloquea únicamente al completar la acción indicada.
    // Excepciones por diseño: Wait avanza solo tras su espera fija, y Finished ofrece
    // «Repetir tutorial» o «Continuar al Acto 1» (ya no carga el Acto 1 automáticamente).
    // La pausa real (botón Menú / Escape) siempre está disponible y permite terminar la sesión.
    public class TutorialFlow : MonoBehaviour
    {
        public TutorialExercise[] exercises;
        public TutorialPanel panel;
        public TutorialControllerCue controllerCue;
        public TutorialInputRouter input;
        public GameObject practiceObjects;
        public TutorialVisualCue[] objectCues;
        public PauseMenu pauseMenu;
        public UnityEvent onFinished = new UnityEvent();
        public string nextSceneName = "Act1";
        public string neutralConfirmation;
        public TutorialStep CurrentStep => exercises[index].step;
        public bool IsPaused { get; private set; }
        public bool StepCompleted { get; private set; }
        // Solo lectura para la capa visual: avance de la espera fija del paso Wait (0 a 1).
        public float WaitProgress => CurrentStep == TutorialStep.Wait ? (StepCompleted ? 1f : Mathf.Clamp01(1f - waitRemaining / 5f)) : 0f;

        int index;
        float waitRemaining;
        bool moved, selected, grabbed, pausedDuringStep, notified, loading;
        string stepFeedback = string.Empty;
        string contextualFeedback;
        float nextFeedbackCheck;
        float idleTimer;
        bool idleHelpShown;
        public float maxPanelDistance = 4.5f;
        public float maxFacingAngle = 60f;
        // Tras estos segundos sin completar la acción se muestra la ayuda del paso (sin juzgar).
        public float idleHelpSeconds = 20f;
        public string approachFeedback = "Acércate al panel para ver mejor las indicaciones. Puedes moverte libremente.";
        public string facingFeedback = "Gira hacia el panel del tutorial para ver las indicaciones. Puedes reorientarte libremente.";
        public string completedMessage = "Acción completada. Presiona Continuar cuando estés listo.";
        public string missingSelectionMessage = "Falta una acción: selecciona un objeto con el gatillo.";
        public string missingMovementMessage = "Falta una acción: muévete con el joystick y suéltalo para detenerte.";
        public string lockedHint = "Completa la acción para desbloquear Continuar.";

        float startedAt;
        public float LastDurationSeconds { get; private set; }

        void OnEnable() { GamePause.Changed += OnGamePauseChanged; }
        void OnDisable() { GamePause.Changed -= OnGamePauseChanged; }

        void Start() { index = 0; startedAt = Time.realtimeSinceStartup; Show(); }

        void Update()
        {
            if (IsPaused) return;
            UpdatePlayerFeedback(false);
            UpdateIdleHelp();
            if (CurrentStep != TutorialStep.Wait || StepCompleted) return;
            waitRemaining -= Time.deltaTime;
            if (waitRemaining <= 0f)
            {
                CompleteStep();
                Continue();
            }
        }

        void Show()
        {
            moved = selected = grabbed = pausedDuringStep = false;
            StepCompleted = false;
            idleTimer = 0f;
            idleHelpShown = false;
            stepFeedback = CurrentStep == TutorialStep.Wait || CurrentStep == TutorialStep.Finished ? string.Empty : lockedHint;
            contextualFeedback = null;
            waitRemaining = 5f; // Única espera de diseño del tutorial, según GDD.
            bool objects = CurrentStep == TutorialStep.Object || CurrentStep == TutorialStep.Options || CurrentStep == TutorialStep.Grab || CurrentStep == TutorialStep.Integration;
            for (int i = 0; i < objectCues.Length; i++)
            {
                // T-23: soltar y restaurar el cubo antes de ocultar su padre.
                objectCues[i].GetComponent<TutorialPracticeTarget>().Configure(objects);
                objectCues[i].ResetCue();
                objectCues[i].gameObject.SetActive(objects && ((CurrentStep != TutorialStep.Object && CurrentStep != TutorialStep.Grab) || i == 0));
            }
            practiceObjects.SetActive(objects);
            input.SetGrabMode(CurrentStep == TutorialStep.Grab);
            input.SetLocomotion(!IsPaused && (CurrentStep == TutorialStep.Movement || CurrentStep == TutorialStep.Turn || CurrentStep == TutorialStep.Integration));
            RefreshHand();
            UpdatePlayerFeedback(true);
            ApplyFeedback();

            if (CurrentStep == TutorialStep.PauseInfo)
            {
                if (pauseMenu == null)
                {
                    // Nunca bloquear el tutorial por una referencia faltante.
                    Debug.LogError("TutorialFlow: falta PauseMenu; el paso de pausa se completa sin acción.");
                    CompleteStep();
                }
                else pauseMenu.AllowPause = true;
            }
        }

        public void RefreshHand()
        {
            panel.Show(exercises[index], input.RightMovement, CurrentStep == TutorialStep.Interface || CurrentStep == TutorialStep.Integration, index, exercises.Length);
            // Show() vuelve a bloquear Continuar; se restituye según el avance real del paso.
            panel.SetContinueEnabled(StepCompleted || CurrentStep == TutorialStep.Finished);
            controllerCue.Show(CurrentStep, input.RightMovement);
        }

        void SetStepFeedback(string message)
        {
            stepFeedback = message ?? string.Empty;
            ApplyFeedback();
        }

        void UpdatePlayerFeedback(bool force)
        {
            if (panel == null || Camera.main == null) return;
            if (!force && Time.unscaledTime < nextFeedbackCheck) return;
            nextFeedbackCheck = Time.unscaledTime + 0.2f;

            Vector3 toPanel = panel.transform.position - Camera.main.transform.position;
            float distance = toPanel.magnitude;
            string message = distance > maxPanelDistance
                ? approachFeedback
                : Vector3.Angle(Camera.main.transform.forward, toPanel) > maxFacingAngle
                    ? facingFeedback
                    : string.Empty;

            if (message == contextualFeedback) return;
            contextualFeedback = message;
            ApplyFeedback();
        }

        void UpdateIdleHelp()
        {
            if (idleHelpShown || StepCompleted || idleHelpSeconds <= 0f) return;
            if (CurrentStep == TutorialStep.Wait || CurrentStep == TutorialStep.Finished) return;
            idleTimer += Time.unscaledDeltaTime;
            if (idleTimer < idleHelpSeconds) return;
            idleHelpShown = true;
            Help();
        }

        void ApplyFeedback()
        {
            if (panel == null) return;
            panel.ShowHelp(string.IsNullOrEmpty(contextualFeedback) ? stepFeedback : contextualFeedback);
        }

        // Desbloquea Continuar. Es el único punto donde un paso pasa a «completado».
        void CompleteStep()
        {
            if (IsPaused || StepCompleted || CurrentStep == TutorialStep.Finished) return;
            StepCompleted = true;
            panel.SetContinueEnabled(true);
            SetStepFeedback(CurrentStep == TutorialStep.Wait ? string.Empty : completedMessage);
            Debug.Log("TUTORIAL_PASO_COMPLETADO: " + CurrentStep);
        }

        public void Help() { if (!IsPaused) SetStepFeedback(exercises[index].help); }

        // Durante el tutorial repite el paso actual; en el cierre reinicia todo el tutorial.
        public void Repeat()
        {
            if (IsPaused) return;
            if (CurrentStep == TutorialStep.Finished) { RestartTutorial(); return; }
            Show();
        }

        void RestartTutorial()
        {
            Debug.Log("TUTORIAL_REINICIO");
            index = 0;
            notified = false;
            startedAt = Time.realtimeSinceStartup;
            Show();
        }

        public void Continue()
        {
            if (IsPaused) return;
            if (CurrentStep == TutorialStep.Finished) { LoadNextScene(); return; }
            if (!StepCompleted) return; // Paso bloqueado: solo avanza al completar la acción.
            index = Mathf.Min(index + 1, exercises.Length - 1);
            Show();
            if (CurrentStep == TutorialStep.Finished) FinishTutorial();
        }

        void FinishTutorial()
        {
            if (notified) return;
            notified = true;
            // Cronometraje pedido por la tesista: solo se escribe en la consola como evidencia de prueba;
            // no se guarda en la sesión ni en el reporte (no es un dato del estudiante).
            LastDurationSeconds = Time.realtimeSinceStartup - startedAt;
            Debug.Log($"TUTORIAL_DURACION: {LastDurationSeconds:0.0} s");
            onFinished.Invoke();
        }

        void LoadNextScene()
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError("No se encontró la escena siguiente al tutorial: " + nextSceneName);
                return;
            }
            loading = true;
            SceneFader.LoadScene(nextSceneName);
        }

        public void NotifyOrientation()
        {
            if (IsPaused || CurrentStep != TutorialStep.Orientation) return;
            CompleteStep();
        }

        public void NotifyMovement()
        {
            if (IsPaused) return;
            moved = true;
            if (CurrentStep == TutorialStep.Movement) CompleteStep();
            else TryCompleteIntegration();
        }

        public void NotifyTurn()
        {
            if (IsPaused || CurrentStep != TutorialStep.Turn) return;
            CompleteStep();
        }

        public void SelectInterface()
        {
            if (IsPaused) return;
            if (CurrentStep == TutorialStep.Interface) { CompleteStep(); return; }
            if (CurrentStep == TutorialStep.Integration) { selected = true; TryCompleteIntegration(); }
        }

        public bool SelectObject()
        {
            if (IsPaused || (CurrentStep != TutorialStep.Object && CurrentStep != TutorialStep.Options && CurrentStep != TutorialStep.Grab && CurrentStep != TutorialStep.Integration)) return false;
            selected = true;
            // Object/Options/Grab se completan al soltar (agarrar y soltar); Integration, al seleccionar.
            if (CurrentStep == TutorialStep.Integration) TryCompleteIntegration();
            else grabbed = true;
            return true;
        }

        public void ReleaseObject()
        {
            if (!grabbed) return;
            if (CurrentStep == TutorialStep.Object || CurrentStep == TutorialStep.Options || CurrentStep == TutorialStep.Grab)
                CompleteStep();
        }

        void TryCompleteIntegration()
        {
            if (CurrentStep != TutorialStep.Integration || StepCompleted) return;
            if (moved && selected) { CompleteStep(); return; }
            SetStepFeedback(moved ? missingSelectionMessage : missingMovementMessage);
        }

        void OnGamePauseChanged(bool paused)
        {
            SetPaused(paused);
            if (CurrentStep != TutorialStep.PauseInfo) return;
            if (paused) pausedDuringStep = true;
            else if (pausedDuringStep) CompleteStep(); // Abrir la pausa y reanudar completa el paso.
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            input.SetLocomotion(!paused && (CurrentStep == TutorialStep.Movement || CurrentStep == TutorialStep.Turn || CurrentStep == TutorialStep.Integration));
        }
    }
}
