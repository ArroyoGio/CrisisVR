using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Yarn.Unity;

namespace CrisisVR
{
    public sealed class Act1Flow : MonoBehaviour
    {
        [SerializeField] DialogueRunner dialogueRunner;
        [SerializeField] GameObject gradeCard;
        [SerializeField] GameObject deliverButton;
        [SerializeField] GameObject correctButton;
        [SerializeField] XRSimpleInteractable deliverInteractable;
        [SerializeField] XRSimpleInteractable correctInteractable;
        Act1ProfessorMotion professorMotion;
        Act1TransitionCard transitionCard;

        public const float DecisionDuration = 45f;
        // Espera de diseño que se congela en pausa (guía v1.0 §6); no mide tiempos de respuesta.
        readonly DecisionTimer decisionTimer = new DecisionTimer(DecisionDuration);
        bool waitingForDecision;
        bool decisionMade;
        bool actStarted;
        bool actCompleted;
        public bool Reintento { get; private set; }
        public bool DecisionRegistrada { get; private set; }
        public bool AwaitingDelivery => deliverButton != null && deliverButton.activeInHierarchy;
        public bool AwaitingDecision => waitingForDecision && !decisionMade;
        public bool Finished => actCompleted;
        public event Action ActCompleted;

        void OnEnable()
        {
            if (deliverInteractable != null) deliverInteractable.selectEntered.AddListener(OnDeliver);
            if (correctInteractable != null) correctInteractable.selectEntered.AddListener(OnCorrect);
            GamePause.Changed += decisionTimer.SetPaused;
        }

        void OnDisable()
        {
            if (deliverInteractable != null) deliverInteractable.selectEntered.RemoveListener(OnDeliver);
            if (correctInteractable != null) correctInteractable.selectEntered.RemoveListener(OnCorrect);
            GamePause.Changed -= decisionTimer.SetPaused;
        }

        void Start()
        {
            if (dialogueRunner == null) dialogueRunner = FindAnyObjectByType<DialogueRunner>();
            if (gradeCard != null) gradeCard.SetActive(false);
            if (correctButton != null) correctButton.SetActive(false);
            if (deliverButton != null) deliverButton.SetActive(false);
            if (dialogueRunner == null)
            {
                Debug.LogError("Act1Flow necesita un DialogueRunner de Yarn Spinner en la escena.", this);
                return;
            }
            SessionRuntime.EnsureSession();
            professorMotion = FindAnyObjectByType<Act1ProfessorMotion>();
            transitionCard = FindAnyObjectByType<Act1TransitionCard>();
            if (actStarted) return;
            actStarted = true;
            SessionRuntime.LogEvent("ACTO1_INICIO");
            if (transitionCard != null) transitionCard.ShowEntry(1, () => _ = RunAct());
            else _ = RunAct();
        }

        void Update()
        {
            if (!waitingForDecision || decisionMade || !decisionTimer.Tick(Time.deltaTime)) return;
            ExpireDecision();
        }

        // Ruta «no actúa»: vence la espera de 45 s sin selección.
        void ExpireDecision()
        {
            waitingForDecision = false;
            decisionMade = true;
            if (correctButton != null) correctButton.SetActive(false);
            Reintento = false;
            DecisionRegistrada = true;
            SessionRuntime.RecordAct1(false);
            _ = ResolveRoute("Act1_Reaccion_NoActuo", false);
        }

        async YarnTask RunAct()
        {
            await PlayNode("Act1_Intro");
            if (this == null) return;
            if (deliverButton != null) deliverButton.SetActive(true);
        }

        void OnDeliver(SelectEnterEventArgs _) => Deliver();

        public async void Deliver()
        {
            if (GamePause.IsPaused || deliverButton == null || !deliverButton.activeInHierarchy) return;
            deliverButton.SetActive(false);
            SessionRuntime.LogEvent("ACTO1_ENTREGA_ENVIADA");
            await DeliverAndPresentFeedback();
        }

        async YarnTask DeliverAndPresentFeedback()
        {
            await PlayNode("Act1_Retro");
            if (this == null) return;
            if (gradeCard != null) gradeCard.SetActive(true);
            if (correctButton != null) correctButton.SetActive(true);
            waitingForDecision = true;
            decisionTimer.Start();
            decisionTimer.SetPaused(GamePause.IsPaused);
        }

        void OnCorrect(SelectEnterEventArgs _) => Correct();

        // Solo para pruebas sin visor: vence la espera de inmediato.
        public void DebugExpire()
        {
            if (!GamePause.IsPaused && AwaitingDecision) ExpireDecision();
        }

        public async void Correct()
        {
            if (GamePause.IsPaused || !waitingForDecision || decisionMade) return;
            decisionTimer.Stop();
            waitingForDecision = false;
            decisionMade = true;
            Reintento = true;
            DecisionRegistrada = true;
            if (correctButton != null) correctButton.SetActive(false);
            SessionRuntime.RecordAct1(true);
            await ResolveRoute("Act1_Reaccion_Corregir", true);
        }

        async YarnTask ResolveRoute(string reactionNode, bool retry)
        {
            if (!retry) Debug.Log("ACT1_DECISION: Reintento=false");
            await PlayNode(reactionNode);
            if (this == null) return;
            if (gradeCard != null) gradeCard.SetActive(false);
            if (actCompleted) return;
            waitingForDecision = false;
            if (transitionCard != null) transitionCard.ShowExit(1, FinishAct);
            else FinishAct();
        }

        void FinishAct()
        {
            if (actCompleted) return;
            actCompleted = true;
            SessionRuntime.CompleteAct1();
            ActCompleted?.Invoke();
        }

        async YarnTask PlayNode(string title)
        {
            if (dialogueRunner == null)
            {
                Debug.LogError($"No se puede iniciar el nodo {title}: falta DialogueRunner.", this);
                return;
            }
            if (professorMotion != null) professorMotion.SetSpeaking(true);
            // La continuación de DialogueTask corre dentro del cierre del diálogo anterior: antes de iniciar
            // otro nodo se espera a que el runner quede libre y se cede un frame (BUG-009).
            while (this != null && dialogueRunner.IsDialogueRunning) await YarnTask.Yield();
            await YarnTask.Yield();
            if (this == null) return;
            // En Yarn Spinner 3, StartDialogue vuelve al arrancar; DialogueTask termina al acabar el nodo.
            await dialogueRunner.StartDialogue(title);
            await dialogueRunner.DialogueTask;
            if (this != null && professorMotion != null) professorMotion.SetSpeaking(false);
        }
    }
}
