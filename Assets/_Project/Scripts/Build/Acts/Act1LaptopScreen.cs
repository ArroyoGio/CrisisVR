using TMPro;
using UnityEngine;

namespace CrisisVR
{
    // Pantalla de la laptop del escritorio del participante. Permanece apagada hasta que Act1Flow
    // acepta la entrega (tecla ENTREGAR del teclado) y entonces muestra el mensaje de confirmación.
    public sealed class Act1LaptopScreen : MonoBehaviour
    {
        [SerializeField] Act1Flow flow;
        [SerializeField] GameObject overlay;
        [SerializeField] TMP_Text label;
        [SerializeField] string deliveredMessage = "Entrega enviada";
        string messageBeforeGuidedInput;
        bool deliveredMessageShown;
        bool showingGuidedInput;

        public bool IsAwaitingDecision => flow != null && flow.AwaitingDecision;

        void Awake()
        {
            if (flow == null) flow = FindAnyObjectByType<Act1Flow>();
            if (overlay != null) overlay.SetActive(false);
        }

        void OnEnable()
        {
            if (flow != null) flow.Delivered += OnDelivered;
        }

        void OnDisable()
        {
            if (flow != null) flow.Delivered -= OnDelivered;
        }

        void OnDelivered()
        {
            deliveredMessageShown = true;
            showingGuidedInput = false;
            if (label != null) label.text = deliveredMessage;
            if (overlay != null) overlay.SetActive(true);
        }

        public void ShowGuidedKeyboardContent(string text)
        {
            if (!showingGuidedInput)
                messageBeforeGuidedInput = label != null ? label.text : string.Empty;

            showingGuidedInput = true;
            if (label != null) label.text = text;
            if (overlay != null) overlay.SetActive(true);
        }

        public void HideGuidedKeyboardContent()
        {
            if (!showingGuidedInput)
                return;

            showingGuidedInput = false;
            if (label != null)
                label.text = deliveredMessageShown ? messageBeforeGuidedInput : string.Empty;
            if (overlay != null)
                overlay.SetActive(deliveredMessageShown);
        }
    }
}
