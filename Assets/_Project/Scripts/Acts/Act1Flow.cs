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

        const float DecisionDuration = 45f;
        bool waitingForDecision;
        bool decisionMade;
        bool actStarted;
        bool actCompleted;
        float decisionStarted;
        public bool Reintento { get; private set; }
        public bool DecisionRegistrada { get; private set; }
        public event Action ActCompleted;

        void OnEnable()
        {
            if (deliverInteractable != null) deliverInteractable.selectEntered.AddListener(OnDeliver);
            if (correctInteractable != null) correctInteractable.selectEntered.AddListener(OnCorrect);
        }

        void OnDisable()
        {
            if (deliverInteractable != null) deliverInteractable.selectEntered.RemoveListener(OnDeliver);
            if (correctInteractable != null) correctInteractable.selectEntered.RemoveListener(OnCorrect);
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
            if (transitionCard != null) transitionCard.ShowEntry(() => _ = RunAct());
            else _ = RunAct();
        }

        void Update()
        {
            if (!waitingForDecision || decisionMade || Time.unscaledTime - decisionStarted < DecisionDuration) return;
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
            if (deliverButton != null) deliverButton.SetActive(true);
        }

        async void OnDeliver(SelectEnterEventArgs _)
        {
            if (deliverButton == null || !deliverButton.activeInHierarchy) return;
            deliverButton.SetActive(false);
            SessionRuntime.LogEvent("ACTO1_ENTREGA_ENVIADA");
            await DeliverAndPresentFeedback();
        }

        async YarnTask DeliverAndPresentFeedback()
        {
            await PlayNode("Act1_Retro");
            if (gradeCard != null) gradeCard.SetActive(true);
            if (correctButton != null) correctButton.SetActive(true);
            waitingForDecision = true;
            decisionStarted = Time.unscaledTime;
        }

        async void OnCorrect(SelectEnterEventArgs _)
        {
            if (!waitingForDecision || decisionMade) return;
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
            if (gradeCard != null) gradeCard.SetActive(false);
            if (actCompleted) return;
            waitingForDecision = false;
            if (transitionCard != null) transitionCard.ShowExit(FinishAct);
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
            await dialogueRunner.StartDialogue(title);
            if (professorMotion != null) professorMotion.SetSpeaking(false);
        }
    }
}
