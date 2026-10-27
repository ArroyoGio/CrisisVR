using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR
{
    [RequireComponent(typeof(Collider))]
    public class TutorialPracticeTarget : MonoBehaviour
    {
        public TutorialFlow flow;
        public TutorialVisualCue cue;
        XRSimpleInteractable interactable;
        XRGrabInteractable grab;
        Vector3 initialPosition;
        Quaternion initialRotation;
        bool initialized;

        void Awake() => Initialize();
        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            interactable = GetComponent<XRSimpleInteractable>();
            grab = GetComponent<XRGrabInteractable>();
            initialPosition = transform.localPosition; initialRotation = transform.localRotation;
        }
        public void Configure(bool grabbing)
        {
            Initialize();
            if (grab == null) return;
            // T-23: liberar el collider antes de registrar el siguiente interactuable.
            interactable.enabled = false;
            grab.enabled = false;
            ResetPosition();
            // También libera cualquier agarre al repetir el ejercicio.
            if (grabbing) grab.enabled = true;
            else interactable.enabled = true;
        }
        void OnEnable()
        {
            Initialize();
            interactable.selectEntered.AddListener(Select);
            interactable.hoverEntered.AddListener(HoverEnter);
            interactable.hoverExited.AddListener(HoverExit);
            if (grab != null)
            {
                grab.selectEntered.AddListener(Select);
                grab.selectExited.AddListener(Release);
                grab.hoverEntered.AddListener(HoverEnter);
                grab.hoverExited.AddListener(HoverExit);
            }
        }
        void OnDisable()
        {
            interactable.selectEntered.RemoveListener(Select);
            interactable.hoverEntered.RemoveListener(HoverEnter);
            interactable.hoverExited.RemoveListener(HoverExit);
            if (grab != null)
            {
                grab.selectEntered.RemoveListener(Select);
                grab.selectExited.RemoveListener(Release);
                grab.hoverEntered.RemoveListener(HoverEnter);
                grab.hoverExited.RemoveListener(HoverExit);
            }
        }
        void Select(SelectEnterEventArgs args) { if (flow.SelectObject()) cue.Activate(); }
        void HoverEnter(HoverEnterEventArgs args) => cue.Hover(true);
        void HoverExit(HoverExitEventArgs args) => cue.Hover(false);
        void Release(SelectExitEventArgs args) => ResetPosition();
        void ResetPosition()
        {
            transform.localPosition = initialPosition; transform.localRotation = initialRotation;
            var body=GetComponent<Rigidbody>();
            if (body != null) { body.position=transform.position; body.rotation=transform.rotation; }
        }
    }
}
