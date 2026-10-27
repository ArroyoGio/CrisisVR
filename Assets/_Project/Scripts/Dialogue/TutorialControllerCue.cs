using UnityEngine;

namespace CrisisVR
{
    public class TutorialControllerCue : MonoBehaviour
    {
        public GameObject leftModel, rightModel, joystickCue, triggerCue, arrows;
        public Transform leftJoystick, rightJoystick, leftTrigger, rightTrigger;
        public Material emphasis;
        public TutorialFlow flow;
        public GameObject leftJoystickHighlight, rightJoystickHighlight, leftTriggerHighlight, rightTriggerHighlight;
        public UnityEngine.UI.Image actionIcon;
        public Sprite moveIcon, turnIcon, selectIcon, pauseIcon;
        public TutorialAimDemonstration aimDemonstration;
        public TutorialGrabDemonstration grabDemonstration;
        public GameObject rightGripHighlight;
        public Sprite grabIcon;
        Quaternion leftRest, rightRest, leftTriggerRest, rightTriggerRest;
        Vector2 inputValue;
        TutorialStep currentStep;
        bool joystickDemo, triggerDemo, initialized;
        float animationPhase;
        Material[] originalMaterials;
        Renderer[] buttonRenderers;

        void Awake() => Initialize();
        public void Rebind() { initialized = false; Initialize(); }
        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (leftJoystick != null) leftRest = leftJoystick.localRotation;
            if (rightJoystick != null) rightRest = rightJoystick.localRotation;
            if (leftTrigger != null) leftTriggerRest = leftTrigger.localRotation;
            if (rightTrigger != null) rightTriggerRest = rightTrigger.localRotation;
            var parts = new[] { leftJoystick, rightJoystick, leftTrigger, rightTrigger };
            buttonRenderers = new Renderer[4];
            originalMaterials = new Material[4];
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                buttonRenderers[i] = parts[i].GetComponentInChildren<Renderer>();
                if (buttonRenderers[i] != null) originalMaterials[i] = buttonRenderers[i].sharedMaterial;
            }
        }
        public void ShowInput(Vector2 value) => inputValue = value;

        void LateUpdate()
        {
            if ((!joystickDemo && !triggerDemo) || (flow != null && flow.IsPaused)) return;
            Initialize();
            // Animación demostrativa: nunca mide respuesta ni hace avanzar el ejercicio.
            animationPhase += Time.deltaTime * 2.3f;
            float demonstration = Mathf.Sin(animationPhase);
            Vector2 value = inputValue.sqrMagnitude > 0.04f ? inputValue : currentStep == TutorialStep.Turn
                ? new Vector2(demonstration, 0) : new Vector2(0, demonstration);
            if (!joystickDemo) value = Vector2.zero;
            if (leftJoystick != null) leftJoystick.localRotation = leftRest * Quaternion.Euler(value.y * 15f, 0, -value.x * 15f);
            if (rightJoystick != null) rightJoystick.localRotation = rightRest * Quaternion.Euler(value.y * 15f, 0, -value.x * 15f);
            float press = triggerDemo ? aimDemonstration != null ? aimDemonstration.PressAmount : Mathf.Max(0, demonstration) : 0;
            if (leftTrigger != null) leftTrigger.localRotation = leftTriggerRest * Quaternion.Euler(-press * 18f, 0, 0);
            if (rightTrigger != null) rightTrigger.localRotation = rightTriggerRest * Quaternion.Euler(-press * 18f, 0, 0);
        }
        public void Show(TutorialStep step, bool right)
        {
            Initialize();
            if (aimDemonstration != null) aimDemonstration.Show(false);
            if (grabDemonstration != null) grabDemonstration.Show(false);
            currentStep = step;
            animationPhase = 0;
            inputValue = Vector2.zero;
            bool joystick = step == TutorialStep.Orientation || step == TutorialStep.Movement || step == TutorialStep.Turn;
            bool trigger = step == TutorialStep.Interface || step == TutorialStep.Object || step == TutorialStep.Options || step == TutorialStep.Integration;
            bool grab = step == TutorialStep.Grab;
            joystickDemo = joystick;
            triggerDemo = trigger;
            bool modelRight = step == TutorialStep.Turn ? !right : joystick ? right : true;
            leftModel.SetActive((joystick || trigger) && !modelRight);
            rightModel.SetActive((joystick || trigger || grab) && modelRight);
            var activeModel = modelRight ? rightModel : leftModel;
            activeModel.transform.localRotation = trigger ? Quaternion.Euler(-20, 80, 0) : Quaternion.Euler(-35, -30, 0);
            if (joystick && Camera.main != null)
            {
                var control=modelRight ? rightJoystick : leftJoystick;
                var facing=Quaternion.FromToRotation(control.up,(Camera.main.transform.position-control.position).normalized)*activeModel.transform.rotation;
                activeModel.transform.rotation=Quaternion.Slerp(activeModel.transform.rotation,facing,0.45f);
            }
            // Se conservan inactivos los marcadores anteriores, sin puntos flotantes.
            joystickCue.SetActive(false);
            triggerCue.SetActive(false);
            arrows.SetActive(false);
            if (leftJoystickHighlight != null) leftJoystickHighlight.SetActive(joystick);
            if (rightJoystickHighlight != null) rightJoystickHighlight.SetActive(joystick);
            if (leftTriggerHighlight != null) leftTriggerHighlight.SetActive(trigger);
            if (rightTriggerHighlight != null) rightTriggerHighlight.SetActive(trigger);
            if (rightGripHighlight != null) rightGripHighlight.SetActive(grab);
            if (actionIcon != null)
            {
                actionIcon.sprite = joystick ? (step == TutorialStep.Turn ? turnIcon : moveIcon) : grab ? grabIcon : trigger ? selectIcon : pauseIcon;
                actionIcon.gameObject.SetActive(step != TutorialStep.Finished && step != TutorialStep.Wait);
            }
            if (aimDemonstration != null) aimDemonstration.Show(step);
            if (grabDemonstration != null) grabDemonstration.Show(grab);
            for (int i = 0; i < buttonRenderers.Length; i++)
                if (buttonRenderers[i] != null)
                    buttonRenderers[i].sharedMaterial = emphasis != null && ((joystick && i < 2) || (trigger && i >= 2)) ? emphasis : originalMaterials[i];
        }
    }
}
