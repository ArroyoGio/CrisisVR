using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CrisisVR
{
    public class TutorialFlow : MonoBehaviour
    {
        public TutorialExercise[] exercises;
        public TutorialPanel panel;
        public TutorialControllerCue controllerCue;
        public TutorialInputRouter input;
        public GameObject practiceObjects;
        public TutorialVisualCue[] objectCues;
        public UnityEvent onFinished = new UnityEvent();
        public string nextSceneName = "Act1";
        public string neutralConfirmation;
        public TutorialStep CurrentStep => exercises[index].step;
        public bool IsPaused { get; private set; }
        int index;
        float waitRemaining;
        bool moved, selected, notified;
        string stepFeedback = string.Empty;
        string contextualFeedback;
        float nextFeedbackCheck;
        public float maxPanelDistance = 4.5f;
        public float maxFacingAngle = 60f;
        public string approachFeedback = "Acércate al panel para ver mejor las indicaciones. Puedes moverte libremente.";
        public string facingFeedback = "Gira hacia el panel del tutorial para ver las indicaciones. Puedes reorientarte libremente.";

        void Start() { index = 0; Show(); }
        void Update()
                {
                    if (IsPaused) return;
                    UpdatePlayerFeedback(false);
                    if (CurrentStep != TutorialStep.Wait) return;
                    waitRemaining -= Time.deltaTime;
                    if (waitRemaining <= 0f) Continue();
                }
        void Show()
                {
                    moved = selected = false;
                    stepFeedback = string.Empty;
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
                }
        public void RefreshHand()
        {
            panel.Show(exercises[index], input.RightMovement, CurrentStep == TutorialStep.Interface || CurrentStep == TutorialStep.Integration, index, exercises.Length);
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

        void ApplyFeedback()
        {
            if (panel == null) return;
            panel.ShowHelp(string.IsNullOrEmpty(contextualFeedback) ? stepFeedback : contextualFeedback);
        }

        public void Help() { if (!IsPaused) SetStepFeedback(exercises[index].help); }
        public void Repeat() { if (!IsPaused && CurrentStep != TutorialStep.Finished) Show(); }
        public void Continue()
        {
            if (IsPaused || CurrentStep == TutorialStep.Finished) return;
            index = Mathf.Min(index + 1, exercises.Length - 1);
            Show();
            if (CurrentStep == TutorialStep.Finished && !notified)
            {
                notified = true;
                onFinished.Invoke();
                if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                    SceneManager.LoadScene(nextSceneName);
                else
                    Debug.LogError("No se encontró la escena siguiente al tutorial: " + nextSceneName);
            }
        }
        public void NotifyMovement()
                {
                    if (IsPaused) return;
                    moved = true;
                    SetStepFeedback(neutralConfirmation);
                    TryCompleteIntegration();
                }
        public void NotifyTurn() { if (!IsPaused) SetStepFeedback(neutralConfirmation); }
        public void SelectInterface()
        {
            if (IsPaused) return;
            if (CurrentStep == TutorialStep.Interface) { Continue(); return; }
            if (CurrentStep == TutorialStep.Integration) { selected = true; TryCompleteIntegration(); }
        }
        public bool SelectObject()
                {
                    if (IsPaused || (CurrentStep != TutorialStep.Object && CurrentStep != TutorialStep.Options && CurrentStep != TutorialStep.Grab && CurrentStep != TutorialStep.Integration)) return false;
                    selected = true;
                    SetStepFeedback(neutralConfirmation);
                    TryCompleteIntegration();
                    return true;
                }
        void TryCompleteIntegration()
                {
                    // Confirmación neutral; continuar siempre está disponible.
                    if (CurrentStep == TutorialStep.Integration && moved && selected) SetStepFeedback(neutralConfirmation);
                }
        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            input.SetLocomotion(!paused && (CurrentStep == TutorialStep.Movement || CurrentStep == TutorialStep.Turn || CurrentStep == TutorialStep.Integration));
        }
    }
}
