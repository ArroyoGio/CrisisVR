using UnityEngine;

namespace CrisisVR
{
    public class TutorialVisualCue : MonoBehaviour
    {
        public Renderer target;
        public Material normal, hover, activated;
        bool selected;

        public void Hover(bool value)
        {
            if (target != null && !selected) target.sharedMaterial = value ? hover : normal;
        }
        public void Activate()
        {
            selected = true;
            if (target != null) target.sharedMaterial = activated;
        }
        public void ResetCue()
        {
            selected = false;
            if (target != null) target.sharedMaterial = normal;
        }
    }
}
