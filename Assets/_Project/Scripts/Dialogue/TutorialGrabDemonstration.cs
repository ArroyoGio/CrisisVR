using UnityEngine;

namespace CrisisVR
{
    public class TutorialGrabDemonstration : MonoBehaviour
    {
        public TutorialFlow flow;
        public Transform controller, grip, sample;
        public Renderer gripSurface;
        Vector3 controllerRest, sampleRest;
        Quaternion rotationRest, gripRest;
        Vector3 reach;
        bool ready;
        float phase;

        public void Show(bool visible)
        {
            if (ready)
            {
                controller.position=controllerRest; controller.rotation=rotationRest;
                sample.position=sampleRest; grip.localRotation=gripRest;
            }
            ready=false;
            gameObject.SetActive(visible);
            if (!visible) return;
            controllerRest=controller.position; rotationRest=controller.rotation;
            sampleRest=sample.position; gripRest=grip.localRotation;
            if (Camera.main != null)
            {
                var axis=flow.transform.up;
                var side=Vector3.ProjectOnPlane((gripSurface!=null ? gripSurface.bounds.center : grip.position)-controller.position,axis);
                var viewer=Vector3.ProjectOnPlane(Camera.main.transform.position-controller.position,axis);
                controller.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(side,viewer,axis),axis)*controller.rotation;
            }
            reach=sampleRest-grip.position;
            phase=0; ready=true; Draw(0);
        }
        void LateUpdate()
        {
            if (!ready || flow.IsPaused) return;
            // Solo demostración: acercar, sujetar, levantar y soltar.
            phase=(phase+Time.deltaTime*0.23f)%1f;
            Draw(phase);
        }
        void Draw(float p)
        {
            float approach=p<0.25f ? Mathf.SmoothStep(0,1,p/0.25f) : p<0.8f ? 1 : 1-Mathf.SmoothStep(0,1,(p-0.8f)/0.2f);
            float hold=p>0.25f && p<0.7f ? Mathf.Clamp01((p-0.25f)/0.1f) : 0;
            float lift=p>0.4f && p<0.7f ? Mathf.SmoothStep(0,1,(p-0.4f)/0.15f) : p>=0.7f && p<0.8f ? 1-(p-0.7f)/0.1f : 0;
            controller.position=controllerRest+reach*approach+flow.transform.up*(lift*0.14f);
            grip.localRotation=gripRest*Quaternion.Euler(0,0,-hold*12f);
            sample.position=sampleRest+flow.transform.up*(lift*0.14f);
        }
    }
}
