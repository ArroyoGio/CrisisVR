using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Yarn.Unity;

namespace CrisisVR
{
    // Acto 2 — Aviso de cambio (adaptabilidad). Registra OpcionAdaptabilidad:
    // 1 plan nuevo (tablet), 2 plan original (carpeta), 3 sin acción tras 45 s de espera de diseño.
    public sealed class Act2Flow : MonoBehaviour
    {
        public const float DecisionDuration = 45f;

        [SerializeField] DialogueRunner dialogueRunner;
        [SerializeField] GameObject actRoot;
        [SerializeField] GameObject teacher;
        [SerializeField] GameObject noticePanel;
        [SerializeField] GameObject tabletScreen;
        [SerializeField] GameObject rodrigo;
        [SerializeField] XRSimpleInteractable tabletInteractable;
        [SerializeField] XRSimpleInteractable folderInteractable;
        [SerializeField] TMP_Text instructionText;
        [SerializeField] Act1TransitionCard transitionCard;

        readonly DecisionTimer timer = new DecisionTimer(DecisionDuration);
        bool started;
        bool awaitingChoice;
        bool finished;

        public event Action ActCompleted;
        public bool AwaitingChoice => awaitingChoice;
        public bool Finished => finished;

        void Awake()
        {
            if (actRoot != null) actRoot.SetActive(false);
        }

        void OnEnable()
        {
            if (tabletInteractable != null) tabletInteractable.selectEntered.AddListener(OnTablet);
            if (folderInteractable != null) folderInteractable.selectEntered.AddListener(OnFolder);
            GamePause.Changed += timer.SetPaused;
        }

        void OnDisable()
        {
            if (tabletInteractable != null) tabletInteractable.selectEntered.RemoveListener(OnTablet);
            if (folderInteractable != null) folderInteractable.selectEntered.RemoveListener(OnFolder);
            GamePause.Changed -= timer.SetPaused;
        }

        public void Begin()
        {
            if (started) return;
            started = true;
            if (dialogueRunner == null) dialogueRunner = FindAnyObjectByType<DialogueRunner>();
            if (transitionCard == null) transitionCard = FindAnyObjectByType<Act1TransitionCard>();
            SessionRuntime.LogEvent("ACTO2_INICIO");

            // Los objetos se cambian mientras la tarjeta cubre la vista.
            if (teacher != null) teacher.SetActive(false);
            if (actRoot != null) actRoot.SetActive(true);
            if (noticePanel != null) noticePanel.SetActive(false);
            if (tabletScreen != null) tabletScreen.SetActive(false);
            if (rodrigo != null) rodrigo.SetActive(false);
            SetChoicesActive(false);

            if (transitionCard != null) transitionCard.ShowEntry(2, () => _ = Run());
            else _ = Run();
        }

        async YarnTask Run()
        {
            await PlayNode("Act2_Intro");
            if (this == null) return;
            if (noticePanel != null) noticePanel.SetActive(true);
            if (tabletScreen != null) tabletScreen.SetActive(true);
            SessionRuntime.LogEvent("ACTO2_AVISO");
            await PlayNode("Act2_Aviso");
            if (this == null) return;
            if (noticePanel != null) noticePanel.SetActive(false);
            if (rodrigo != null) rodrigo.SetActive(true);
            await PlayNode("Act2_Rodrigo");
            if (this == null) return;
            SetInstruction("Selecciona el material con el que quieres continuar: la tablet (plan nuevo) o la carpeta (plan original).");
            SetChoicesActive(true);
            awaitingChoice = true;
            timer.Start();
            timer.SetPaused(GamePause.IsPaused);
        }

        void Update()
        {
            if (awaitingChoice && timer.Tick(Time.deltaTime))
                Choose(DecisionCodes.Adapt_SinAccion);
        }

        void OnTablet(SelectEnterEventArgs _) => Choose(DecisionCodes.Adapt_PlanNuevo);
        void OnFolder(SelectEnterEventArgs _) => Choose(DecisionCodes.Adapt_PlanOriginal);

        // Punto único de decisión: lo usan el rayo XR, la espera vencida y EditorDebugInput.
        public void Choose(int code)
        {
            if (!awaitingChoice || GamePause.IsPaused) return;
            awaitingChoice = false;
            timer.Stop();
            SetChoicesActive(false);
            SetInstruction(string.Empty);
            SessionRuntime.RecordAct2(code);
            _ = Resolve(code);
        }

        public void DebugExpire()
        {
            if (!GamePause.IsPaused && awaitingChoice) Choose(DecisionCodes.Adapt_SinAccion);
        }

        async YarnTask Resolve(int code)
        {
            string node = code == DecisionCodes.Adapt_PlanNuevo ? "Act2_Reaccion_PlanNuevo"
                : code == DecisionCodes.Adapt_PlanOriginal ? "Act2_Reaccion_PlanOriginal"
                : "Act2_Reaccion_SinAccion";
            await PlayNode(node);
            if (this == null) return;
            if (transitionCard != null) transitionCard.ShowExit(2, Finish);
            else Finish();
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            if (actRoot != null) actRoot.SetActive(false);
            SessionRuntime.CompleteAct2();
            ActCompleted?.Invoke();
        }

        void SetChoicesActive(bool value)
        {
            SetHighlight(tabletInteractable, value);
            SetHighlight(folderInteractable, value);
        }

        static void SetHighlight(XRSimpleInteractable interactable, bool value)
        {
            if (interactable == null) return;
            var highlight = interactable.GetComponent<SelectableHighlight>();
            if (highlight != null) highlight.SetActive(value);
            else interactable.enabled = value;
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
            // El cabeceo de Rodrigo lo maneja SpeakerPresenter, línea por línea.
            // La continuación de DialogueTask corre dentro del cierre del diálogo anterior: antes de iniciar
            // otro nodo se espera a que el runner quede libre y se cede un frame (BUG-009).
            while (this != null && dialogueRunner.IsDialogueRunning) await YarnTask.Yield();
            await YarnTask.Yield();
            if (this == null) return;
            // En Yarn Spinner 3, StartDialogue vuelve al arrancar; DialogueTask termina al acabar el nodo.
            await dialogueRunner.StartDialogue(title);
            await dialogueRunner.DialogueTask;
        }
    }
}
