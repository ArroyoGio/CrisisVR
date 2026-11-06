using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrisisVR
{
    // Indicaciones 3D del tutorial: resalta el objeto interactivo de cada etapa y marca referencias en el suelo.
    // Solo LEE el estado de TutorialFlow. Sin luces, sin sombras y sin transparencias grandes (pensado para Quest 3).
    [DisallowMultipleComponent]
    public class TutorialWorldCues : MonoBehaviour
    {
        [System.Serializable]
        public class ObjectGuide
        {
            public TutorialVisualCue cue;
            public XRGrabInteractable grab;
            public Transform outline;        // Casco invertido hijo del cubo.
            public SpriteRenderer ring;      // Anillo sobre la mesa, fijo en la posición de reposo.
            public SpriteRenderer marker;    // Flecha flotante que mira a la cámara.
            [HideInInspector] public Vector3 markerHome;
            [HideInInspector] public Vector3 ringScale;
        }

        public TutorialFlow flow;
        public ObjectGuide[] guides = new ObjectGuide[0];
        public SpriteRenderer[] floorRings = new SpriteRenderer[0];
        public SpriteRenderer startRing;

        static readonly Color Amber = new Color(1f, 0.72f, 0.20f, 1f);
        static readonly Color Green = new Color(0.30f, 0.90f, 0.62f, 1f);
        static readonly Color Teal = new Color(0.16f, 0.78f, 0.83f, 1f);

        Transform cam;

        void Start()
        {
            foreach (var g in guides)
            {
                if (g == null) continue;
                if (g.marker != null) g.markerHome = g.marker.transform.position;
                if (g.ring != null) g.ringScale = g.ring.transform.localScale;
            }
        }

        void Update()
        {
            if (flow == null) return;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            float t = Time.unscaledTime;
            var step = flow.CurrentStep;
            bool done = flow.StepCompleted;
            bool objectStep = step == TutorialStep.Object || step == TutorialStep.Options || step == TutorialStep.Grab || step == TutorialStep.Integration;
            bool moveStep = step == TutorialStep.Movement || step == TutorialStep.Turn || step == TutorialStep.Integration;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3.0f);

            foreach (var g in guides)
            {
                if (g == null || g.cue == null) continue;
                bool visible = objectStep && g.cue.gameObject.activeInHierarchy;
                bool held = g.grab != null && g.grab.isSelected;
                if (g.ring != null)
                {
                    if (g.ring.gameObject.activeSelf != visible) g.ring.gameObject.SetActive(visible);
                    if (visible)
                    {
                        var c = done ? Green : Amber;
                        c.a = held ? 0.35f : 0.55f + 0.40f * pulse;
                        g.ring.color = c;
                        float s = held ? 1f : 0.94f + 0.10f * pulse;
                        g.ring.transform.localScale = g.ringScale * s;
                    }
                }
                if (g.marker != null)
                {
                    bool showMarker = visible && !held && !done;
                    if (g.marker.gameObject.activeSelf != showMarker) g.marker.gameObject.SetActive(showMarker);
                    if (showMarker)
                    {
                        var p = g.markerHome + Vector3.up * (0.035f * Mathf.Sin(t * 3.0f));
                        g.marker.transform.position = p;
                        if (cam != null)
                        {
                            var away = p - cam.position; away.y = 0f;
                            if (away.sqrMagnitude > 0.0001f) g.marker.transform.rotation = Quaternion.LookRotation(away);
                        }
                    }
                }
                if (g.outline != null)
                {
                    bool showOutline = visible;
                    if (g.outline.gameObject.activeSelf != showOutline) g.outline.gameObject.SetActive(showOutline);
                    if (showOutline) { float k = 1.10f + 0.04f * pulse; g.outline.localScale = new Vector3(k, k, k); }
                }
            }

            if (floorRings != null)
            {
                foreach (var r in floorRings)
                {
                    if (r == null) continue;
                    if (r.gameObject.activeSelf != moveStep) r.gameObject.SetActive(moveStep);
                    if (!moveStep) continue;
                    var c = done ? Green : Amber;
                    c.a = 0.45f + 0.35f * pulse;
                    r.color = c;
                }
            }
            if (startRing != null) { var c = Teal; c.a = 0.55f; startRing.color = c; }
        }
    }
}
