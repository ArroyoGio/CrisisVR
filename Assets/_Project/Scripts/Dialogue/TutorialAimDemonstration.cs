using UnityEngine;
using UnityEngine.UI;

namespace CrisisVR
{
    public class TutorialAimDemonstration : MonoBehaviour
    {
        public TutorialFlow flow;
        public Transform controller, emitter, target, emphasizedControl;
        public Transform buttonTarget;
        public Renderer emphasizedSurface;
        public GameObject objectExample, secondObjectExample;
        public Renderer objectRenderer;
        public float PressAmount { get; private set; }
        public LineRenderer ray;
        public Image buttonFace, buttonIcon;
        public Color resting = new Color(0.13f, 0.22f, 0.30f);
        public Color pointed = new Color(1f, 0.72f, 0.25f);
        public Color pressed = new Color(0.95f, 0.95f, 0.88f);
        Quaternion rest, aligned, controlFront;
        float phase;
        bool ready;
        bool objectMode;
        Vector3 targetScale;
        MaterialPropertyBlock objectColor;

        public void Show(TutorialStep step)
        {
            Show(false);
            bool visible=step==TutorialStep.Interface || step==TutorialStep.Object || step==TutorialStep.Options || step==TutorialStep.Integration;
            if (!visible) return;
            if (buttonTarget == null) buttonTarget=target;
            if (step!=TutorialStep.Interface && objectExample==null) return;
            objectMode=step!=TutorialStep.Interface;
            buttonTarget.gameObject.SetActive(!objectMode);
            if (objectExample!=null) objectExample.SetActive(objectMode);
            if (secondObjectExample!=null) secondObjectExample.SetActive(step==TutorialStep.Options);
            target=objectMode ? objectExample.transform : buttonTarget;
            Show(true);
        }

        public void Show(bool visible)
        {
            if (ready) { controller.localRotation = rest; target.localScale=targetScale; }
            gameObject.SetActive(visible);
            if (!visible) { ready = false; return; }
            rest = controller.localRotation;
            targetScale=target.localScale;
            aligned = Quaternion.FromToRotation(emitter.forward, (target.position - emitter.position).normalized) * controller.rotation;
            controller.rotation = aligned;
            // Orienta el lado del gatillo hacia la cámara sin desviar el rayo.
            if (emphasizedControl != null && Camera.main != null)
            {
                var axis = emitter.forward;
                var controlPosition=emphasizedSurface!=null ? emphasizedSurface.bounds.center : emphasizedControl.position;
                var controlSide = Vector3.ProjectOnPlane(controlPosition-emitter.position,axis);
                var viewerSide = Vector3.ProjectOnPlane(Camera.main.transform.position-emitter.position,axis);
                aligned = Quaternion.AngleAxis(Vector3.SignedAngle(controlSide,viewerSide,axis),axis) * aligned;
            }
            controller.rotation=aligned;
            controlFront=aligned;
            if(emphasizedSurface!=null && Camera.main!=null)
            {
                Bounds body=emphasizedSurface.bounds; float largest=0;
                foreach(var renderer in controller.GetComponentsInChildren<Renderer>())
                    if(renderer.bounds.size.sqrMagnitude>largest) {largest=renderer.bounds.size.sqrMagnitude;body=renderer.bounds;}
                var outward=emphasizedSurface.bounds.center-body.center;
                var viewer=Camera.main.transform.position-emphasizedSurface.bounds.center;
                controlFront=Quaternion.FromToRotation(outward.normalized,viewer.normalized)*aligned;
            }
            phase = 0;
            ready = true;
            Draw(0f);
        }
        void LateUpdate()
        {
            if (!ready || (flow != null && flow.IsPaused)) return;
            // Secuencia visual repetida; no selecciona UI ni registra resultados.
            phase = (phase + Time.deltaTime * 0.3f) % 1f;
            Draw(phase);
        }
        void Draw(float progress)
        {
            // Primero muestra el control de frente; después orienta el mando al objetivo.
            float alignment=progress<0.2f ? 0 : progress<0.5f ? Mathf.SmoothStep(0,1,(progress-0.2f)/0.3f)
                : progress<0.8f ? 1 : 1-Mathf.SmoothStep(0,1,(progress-0.8f)/0.2f);
            controller.rotation = Quaternion.Slerp(controlFront, aligned, alignment);
            bool onTarget = alignment > 0.98f;
            bool activating = onTarget && progress > 0.55f && progress < 0.80f;
            PressAmount = activating ? Mathf.Sin((progress-0.55f)/0.25f*Mathf.PI) : 0;
            var end = emitter.position + emitter.forward * Vector3.Distance(emitter.position, target.position);
            ray.SetPosition(0, emitter.position);
            ray.SetPosition(1, onTarget ? target.position : end);
            ray.enabled=alignment>0.1f;
            ray.startColor = ray.endColor = onTarget ? pointed : Color.white;
            if (!objectMode) buttonFace.color = activating ? pressed : onTarget ? pointed : resting;
            else if (objectRenderer != null)
            {
                if (objectColor == null) objectColor = new MaterialPropertyBlock();
                objectColor.SetColor("_BaseColor",activating ? pressed : onTarget ? pointed : resting);
                objectRenderer.SetPropertyBlock(objectColor);
            }
            if (buttonIcon != null) buttonIcon.color = onTarget ? new Color(0.1f,0.1f,0.1f) : Color.white;
            target.localScale = targetScale * (activating ? 0.93f : 1f);
        }
    }
}
