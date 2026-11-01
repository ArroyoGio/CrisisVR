using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Yarn.Unity;

namespace CrisisVR
{
    // Acto 3 — Reunión de equipo (trabajo en equipo). Registra OpcionTrabajoEnEquipo (1 A … 4 D).
    // No hay tiempo de espera: a los 90 s sin selección solo aparece un recordatorio, que no se registra.
    public sealed class Act3Flow : MonoBehaviour
    {
        public const float ReminderDelay = 90f;
        public const string ReminderText = "Selecciona una opción para continuar.";

        [SerializeField] DialogueRunner dialogueRunner;
        [SerializeField] GameObject actRoot;
        [SerializeField] GameObject callScreen;
        [SerializeField] GameObject optionsRoot;
        [SerializeField] XRSimpleInteractable optionA;
        [SerializeField] XRSimpleInteractable optionB;
        [SerializeField] XRSimpleInteractable optionC;
        [SerializeField] XRSimpleInteractable optionD;
        [SerializeField] TMP_Text instructionText;
        [SerializeField] Act1TransitionCard transitionCard;

        readonly DecisionTimer reminder = new DecisionTimer(ReminderDelay);
        bool started;
        bool awaitingChoice;
        bool finished;

        public event Action ActCompleted;
        public bool AwaitingChoice => awaitingChoice;
        public bool ReminderShown { get; private set; }
        public bool Finished => finished;

        void Awake()
        {
            if (actRoot != null) actRoot.SetActive(false);
        }

        void OnEnable()
        {
            Subscribe(optionA, OnA, true);
            Subscribe(optionB, OnB, true);
            Subscribe(optionC, OnC, true);
            Subscribe(optionD, OnD, true);
            GamePause.Changed += reminder.SetPaused;
        }

        void OnDisable()
        {
            Subscribe(optionA, OnA, false);
            Subscribe(optionB, OnB, false);
            Subscribe(optionC, OnC, false);
            Subscribe(optionD, OnD, false);
            GamePause.Changed -= reminder.SetPaused;
        }

        static void Subscribe(XRSimpleInteractable interactable, UnityEngine.Events.UnityAction<SelectEnterEventArgs> handler, bool add)
        {
            if (interactable == null) return;
            if (add) interactable.selectEntered.AddListener(handler);
            else interactable.selectEntered.RemoveListener(handler);
        }

        public void Begin()
        {
            if (started) return;
            started = true;
            if (dialogueRunner == null) dialogueRunner = FindAnyObjectByType<DialogueRunner>();
            if (transitionCard == null) transitionCard = FindAnyObjectByType<Act1TransitionCard>();
            SessionRuntime.LogEvent("ACTO3_INICIO");

            if (actRoot != null) actRoot.SetActive(true);
            if (callScreen != null) callScreen.SetActive(true);
            if (optionsRoot != null) optionsRoot.SetActive(false);
            SetOptionsActive(false);

            if (transitionCard != null) transitionCard.ShowEntry(3, () => _ = Run());
            else _ = Run();
        }

        async YarnTask Run()
        {
            await PlayNode("Act3_Intro");
            await PlayNode("Act3_Conflicto");
            if (this == null) return;
            // Orden fijo A–D de izquierda a derecha (propuesta V-16, pendiente de validar).
            if (optionsRoot != null) optionsRoot.SetActive(true);
            SetOptionsActive(true);
            awaitingChoice = true;
            reminder.Start();
            reminder.SetPaused(GamePause.IsPaused);
        }

        void Update()
        {
            if (awaitingChoice && reminder.Tick(Time.deltaTime))
            {
                ReminderShown = true;
                SetInstruction(ReminderText);
            }
        }

        void OnA(SelectEnterEventArgs _) => Choose(DecisionCodes.Team_A);
        void OnB(SelectEnterEventArgs _) => Choose(DecisionCodes.Team_B);
        void OnC(SelectEnterEventArgs _) => Choose(DecisionCodes.Team_C);
        void OnD(SelectEnterEventArgs _) => Choose(DecisionCodes.Team_D);

        public void Choose(int code)
        {
            if (!awaitingChoice || GamePause.IsPaused) return;
            if (code < DecisionCodes.Team_A || code > DecisionCodes.Team_D) return;
            awaitingChoice = false;
            reminder.Stop();
            SetInstruction(string.Empty);
            SetOptionsActive(false);
            if (optionsRoot != null) optionsRoot.SetActive(false);
            SessionRuntime.RecordAct3(code);
            _ = Resolve(code);
        }

        // Solo para pruebas: adelanta el recordatorio de 90 s sin registrar nada.
        public void DebugExpireReminder()
        {
            if (!awaitingChoice || GamePause.IsPaused) return;
            ReminderShown = true;
            SetInstruction(ReminderText);
        }

        async YarnTask Resolve(int code)
        {
            // El texto del botón aparece como subtítulo del estudiante (hablante «Tú») dentro del nodo de reacción.
            await PlayNode("Act3_Reaccion_" + DecisionCodes.TeamLetter(code));
            await PlayNode("Act3_Cierre");
            if (this == null) return;
            if (callScreen != null) callScreen.SetActive(false);
            // Fin del Acto 3: sesión completa y reporte final. Desde aquí ya no se puede pausar,
            // para que TERMINAR SESIÓN no actúe sobre una sesión ya cerrada.
            var pauseMenu = FindAnyObjectByType<PauseMenu>();
            if (pauseMenu != null) pauseMenu.AllowPause = false;
            SessionRuntime.CompleteAct3();
            if (transitionCard != null) transitionCard.ShowExit(3, Finish);
            else Finish();
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            if (actRoot != null) actRoot.SetActive(false);
            ActCompleted?.Invoke();
        }

        void SetOptionsActive(bool value)
        {
            foreach (var option in new[] { optionA, optionB, optionC, optionD })
            {
                if (option == null) continue;
                var highlight = option.GetComponent<SelectableHighlight>();
                if (highlight != null) highlight.SetActive(value);
                else option.enabled = value;
            }
        }

        void SetInstruction(string text)
        {
            if (instructionText == null) return;
            instructionText.text = text;
            instructionText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        async YarnTask PlayNode(string title)
        {
            if (dialogueRunner == null)
            {
                Debug.LogError($"No se puede iniciar el nodo {title}: falta DialogueRunner.", this);
                return;
            }
            // En Yarn Spinner 3, StartDialogue vuelve al arrancar; DialogueTask termina al acabar el nodo.
            await dialogueRunner.StartDialogue(title);
            await dialogueRunner.DialogueTask;
        }
    }
}
