using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
#pragma warning disable CS0618
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;
#pragma warning restore CS0618

namespace CrisisVR
{
    public class TutorialInputRouter : MonoBehaviour
    {
        public TutorialFlow flow;
        public XROrigin playerOrigin;
        public ControllerInputActionManager leftManager, rightManager;
        public ContinuousMoveProvider movement;
        public SnapTurnProvider snapTurn;
        public ContinuousTurnProvider continuousTurn;
        public InputActionAsset actions;
        public NearFarInteractor[] interactors = new NearFarInteractor[0];
        public bool RightMovement { get; private set; }
        InputAction leftStick, rightStick;
        bool neutral = true, moved;
        bool allowLocomotion;
        bool grabMode;

        void Awake()
        {
            ConfigureTrackingSpace();
            // VERIFICAR: en XRI 3.5.1 no se reasigna interactors[].selectInput en Play;
            // la referencia de accion cacheada no se refresca y el near-field usa uiPressInput.
            // El agarre se controla por estado (interactable) sin tocar los lectores del interactor.
            leftStick = new InputAction(binding: "<XRController>{LeftHand}/primary2DAxis", type: InputActionType.Value);
            rightStick = new InputAction(binding: "<XRController>{RightHand}/primary2DAxis", type: InputActionType.Value);
        }

        void ConfigureTrackingSpace()
        {
            var origin = playerOrigin != null ? playerOrigin :
                (movement != null ? movement.GetComponentInParent<XROrigin>() : GetComponentInChildren<XROrigin>(true));
            if (origin == null) return;
            playerOrigin = origin;

            // La locomoción transforma únicamente el rig, nunca TutorialRoot ni el aula.
            origin.Origin = origin.gameObject;
            origin.CameraYOffset = 1.6f;
            // El simulador parte de una pose relativa al dispositivo; Quest usa su suelo real.
            origin.RequestedTrackingOriginMode = Application.isEditor
                ? XROrigin.TrackingOriginMode.Device
                : XROrigin.TrackingOriginMode.Floor;

            // Inicializar una sola vez; no seguir ni compensar la cámara cada fotograma.
            if (origin.CameraFloorOffsetObject != null)
            {
                var offset = origin.CameraFloorOffsetObject.transform;
                var position = offset.localPosition;
                position.y = Application.isEditor ? origin.CameraYOffset : 0f;
                offset.localPosition = position;
            }
        }
        void OnEnable() { leftStick.Enable(); rightStick.Enable(); }
        void Start()
        {
            ApplyScheme();
            InitializeControllerAffordances();
        }

        void InitializeControllerAffordances()
        {
            // XRI 3.5.1 deja los receptores de color de Poke/NearFar sin tema en Play;
            // copiar el tema del datum y reactivar registra cada receptor con su provider.
#pragma warning disable CS0618
            // TutorialWorld y TutorialPlayer son objetos raíz hermanos en la escena.
            // Los receptores de los mandos viven bajo XROrigin, no bajo este objeto.
            var receiverRoot = playerOrigin != null ? playerOrigin.gameObject : gameObject;
            foreach (var receiver in receiverRoot.GetComponentsInChildren<ColorAffordanceReceiver>(true))
            {
                if (receiver.affordanceTheme != null) continue;
                var theme = receiver.affordanceThemeDatum?.Value;
                if (theme == null)
                {
                    receiver.enabled = false;
                    Debug.LogWarning($"Se desactivó el feedback de color de {receiver.name}: no tiene Affordance Theme.", receiver);
                    continue;
                }

                receiver.enabled = false;
                receiver.affordanceTheme = theme;
                receiver.enabled = true;
            }
#pragma warning restore CS0618
        }
        public void SetGrabMode(bool grabbing)
        {
            // No se reasigna selectInput: solo se recuerda el estado. El gatillo (uiPressInput)
            // se desactiva durante el agarre para que el paso Grab use el boton lateral.
            grabMode = grabbing;
            for (int i=0;i<interactors.Length;i++)
            {
                var press = interactors[i].uiPressInput;
                if (press == null) continue;
                if (grabbing) press.DisableDirectActionIfModeUsed();
                else press.EnableDirectActionIfModeUsed();
            }
        }
        void OnDisable() { leftStick?.Disable(); rightStick?.Disable(); }
        void OnDestroy() { leftStick?.Dispose(); rightStick?.Dispose(); }

        public void SetRightMovement(bool right)
        {
            RightMovement = right;
            ApplyScheme();
            neutral = false;
            moved = false;
            flow.RefreshHand();
        }
        public void SetLocomotion(bool value)
                {
                    allowLocomotion = value;
                    movement.enabled = value;

                    // El giro usa la mano opuesta a la elegida para desplazarse.
                    bool smoothTurning = RightMovement
                        ? leftManager.smoothTurnEnabled
                        : rightManager.smoothTurnEnabled;
                    bool useContinuousTurn = continuousTurn != null && smoothTurning;

                    // Snap y continuo nunca leen el stick al mismo tiempo.
                    snapTurn.enabled = value && !useContinuousTurn;
                    if (continuousTurn != null)
                        continuousTurn.enabled = value && useContinuousTurn;

                    neutral = false;
                    moved = false;
                }
        void ApplyScheme()
        {
            leftManager.smoothMotionEnabled = !RightMovement;
            rightManager.smoothMotionEnabled = RightMovement;
            // No se modifica smoothTurnEnabled ni los valores del giro.
            foreach (var map in actions.actionMaps)
                foreach (var action in map.actions)
                    if (action.name.StartsWith("Teleport")) action.Disable();
        }
        void Update()
        {
            if (flow == null || flow.IsPaused) return;
            // El gatillo no debe seleccionar durante el agarre: se reafirma cada fotograma.
            if (grabMode)
                for (int i=0;i<interactors.Length;i++)
                    interactors[i].uiPressInput?.DisableDirectActionIfModeUsed();
            Vector2 left = leftStick.ReadValue<Vector2>(), right = rightStick.ReadValue<Vector2>();
            bool turning = flow.CurrentStep == TutorialStep.Turn;
            flow.controllerCue.ShowInput(turning ? (RightMovement ? left : right) : (RightMovement ? right : left));
            bool bothReleased = left.sqrMagnitude < 0.04f && right.sqrMagnitude < 0.04f;
            if (!neutral) { if (bothReleased) neutral = true; return; }
            if (flow.CurrentStep == TutorialStep.Orientation)
            {
                // Solo elegir durante este ejercicio, sin desplazar ni girar.
                if (left.sqrMagnitude > 0.25f || right.sqrMagnitude > 0.25f)
                    SetRightMovement(right.sqrMagnitude > left.sqrMagnitude);
                return;
            }
            if (!allowLocomotion) return;
            Vector2 move = RightMovement ? right : left;
            Vector2 turn = RightMovement ? left : right;
            if (flow.CurrentStep == TutorialStep.Movement || flow.CurrentStep == TutorialStep.Integration)
            {
                if (move.sqrMagnitude > 0.25f) moved = true;
                if (moved && move.sqrMagnitude < 0.04f) { moved = false; flow.NotifyMovement(); }
            }
            if (flow.CurrentStep == TutorialStep.Turn && Mathf.Abs(turn.x) > 0.5f)
            {
                flow.NotifyTurn();
                neutral = false;
            }
        }
    }
}
