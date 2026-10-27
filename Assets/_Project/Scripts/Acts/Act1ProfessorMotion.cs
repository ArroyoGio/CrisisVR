using UnityEngine;

namespace CrisisVR
{
    public sealed class Act1ProfessorMotion : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] Transform torso;

        Quaternion headRest;
        Quaternion torsoRest;
        float motionPhase;
        bool speaking;

        void Awake()
        {
            if (head == null) head = transform.Find("Cabeza");
            if (torso == null) torso = transform.Find("Cuerpo");
            if (head != null) headRest = head.localRotation;
            if (torso != null) torsoRest = torso.localRotation;
        }

        public void SetSpeaking(bool value) => speaking = value;

        void Update()
        {
            float delta = Time.unscaledDeltaTime;
            motionPhase = speaking ? motionPhase + delta * 3.2f : Mathf.MoveTowards(motionPhase, 0f, delta * 2f);
            float nod = Mathf.Sin(motionPhase) * (speaking ? 3f : 0f);
            if (head != null) head.localRotation = headRest * Quaternion.Euler(nod, 0f, 0f);
            if (torso != null) torso.localRotation = torsoRest * Quaternion.Euler(0f, 0f, nod * .2f);
        }
    }
}
