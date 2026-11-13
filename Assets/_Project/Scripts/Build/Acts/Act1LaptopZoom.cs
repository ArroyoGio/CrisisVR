using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrisisVR
{
    // Acto 1: con el participante sentado, X (mando izquierdo) acerca suavemente la camara a la laptop y la vuelve a alejar.
    // El desplazamiento pasa por Act1ArrivalGate.ShiftSeat para convivir con el bloqueo de locomocion y el limite de cabeza.
    // Solo actua mientras el participante esta sentado y el Acto 1 sigue en curso; al terminar el acto, el gate lo devuelve al asiento.
    public sealed class Act1LaptopZoom : MonoBehaviour
    {
        const string ZoomInMessage = "Presiona X para acercarte a la laptop";
        const string ZoomOutMessage = "Presiona X para alejarte";

        [SerializeField] Act1ArrivalGate gate;
        [SerializeField] Act1Flow flow;
        [Tooltip("Posicion (en planta) a la que se acerca la cabeza. Por defecto Zona_Interaccion_Laptop.")]
        [SerializeField] Transform zoomTarget;
        [SerializeField] CanvasGroup hintGroup;
        [SerializeField] TMP_Text hintLabel;
        [SerializeField, Min(.2f)] float travelDuration = 1f;
        [SerializeField, Min(.05f)] float hintFadeDuration = .25f;

        InputAction toggleAction;
        CanvasGroup cardGroup;
        float progress;      // 0 = vista normal del aula, 1 = cerca de la laptop
        float target;        // destino actual del movimiento (0 o 1); una nueva pulsacion invierte el sentido sin saltos
        float hintAlpha;
        bool pendingToggle;

        public bool IsZoomed => progress > .5f && Mathf.Approximately(progress, target);

        void Awake()
        {
            if (gate == null) gate = FindAnyObjectByType<Act1ArrivalGate>();
            if (flow == null) flow = FindAnyObjectByType<Act1Flow>();
            var card = FindAnyObjectByType<Act1TransitionCard>();
            if (card != null) cardGroup = card.GetComponent<CanvasGroup>();
            if (hintGroup != null)
            {
                hintGroup.alpha = 0f;
                hintGroup.interactable = false;
                hintGroup.blocksRaycasts = false;
            }

            // X del mando izquierdo de Quest = primaryButton. El teclado X solo sirve para probar en el editor sin visor.
            toggleAction = new InputAction("CrisisVR Acto1 Zoom Laptop", InputActionType.Button);
            toggleAction.AddBinding("<XRController>{LeftHand}/primaryButton");
            toggleAction.AddBinding("<Keyboard>/x");
        }

        void OnEnable()
        {
            toggleAction.performed += OnTogglePerformed;
            toggleAction.Enable();
            if (gate != null) gate.SeatReleased += OnSeatReleased;
        }

        void OnDisable()
        {
            toggleAction.performed -= OnTogglePerformed;
            toggleAction.Disable();
            if (gate != null) gate.SeatReleased -= OnSeatReleased;
        }

        void OnDestroy() => toggleAction?.Dispose();

        void OnTogglePerformed(InputAction.CallbackContext _) => pendingToggle = true;

        void OnSeatReleased()
        {
            progress = 0f;
            target = 0f;
            pendingToggle = false;
        }

        void Update()
        {
            if (gate == null || flow == null || zoomTarget == null || hintGroup == null || hintLabel == null) return;

            bool active = gate.IsSeated && !flow.Finished;
            bool paused = GamePause.IsPaused;
            bool cardClear = cardGroup == null || cardGroup.alpha < .02f;

            if (!active)
            {
                pendingToggle = false;
                progress = 0f;
                target = 0f;
                FadeHint(false);
                return;
            }

            if (pendingToggle)
            {
                pendingToggle = false;
                if (!paused && cardClear) target = target > .5f ? 0f : 1f;
            }

            if (!paused && !Mathf.Approximately(progress, target))
            {
                progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / travelDuration);
                Vector3 full = zoomTarget.position - gate.SeatBase;
                full.y = 0f;
                gate.ShiftSeat(full * Mathf.SmoothStep(0f, 1f, progress) - gate.SeatOffset);
            }

            bool atRest = Mathf.Approximately(progress, target);
            bool showHint = atRest && !paused && cardClear;
            if (showHint) hintLabel.text = target > .5f ? ZoomOutMessage : ZoomInMessage;
            FadeHint(showHint);
        }

        void FadeHint(bool visible)
        {
            hintAlpha = Mathf.MoveTowards(hintAlpha, visible ? 1f : 0f, Time.unscaledDeltaTime / hintFadeDuration);
            hintGroup.alpha = hintAlpha;
        }
    }
}
