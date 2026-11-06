using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrisisVR
{
    // Capa visual del panel del tutorial. Solo LEE el estado de TutorialFlow/TutorialPanel:
    // no avanza pasos, no desbloquea botones ni cambia la lógica del tutorial.
    // Costo: un LateUpdate con comparaciones simples y a lo sumo 4 imágenes animadas (sin alloc).
    [DisallowMultipleComponent]
    public class TutorialPanelStyle : MonoBehaviour
    {
        public TutorialFlow flow;
        public TutorialPanel panel;
        public Image[] segments = new Image[0];
        public Image actionIcon, actionBadge;
        public Image continueBackground, continueGlow, continueLock, continueCheck;
        public TMP_Text continueLabel;
        public Image practiceGlow;
        public Image feedbackBar, feedbackFill, feedbackIcon;
        public Sprite infoSprite, checkSprite;

        // Paleta CrisisVR (azul noche + turquesa; ámbar = «esto es lo que debes usar»; verde = completado).
        public static readonly Color Teal = new Color(0.16f, 0.78f, 0.83f, 1f);
        public static readonly Color Amber = new Color(1f, 0.72f, 0.20f, 1f);
        public static readonly Color Green = new Color(0.30f, 0.90f, 0.62f, 1f);
        static readonly Color SegDim = new Color(0.16f, 0.23f, 0.30f, 1f);
        static readonly Color Slate = new Color(0.13f, 0.20f, 0.27f, 1f);
        static readonly Color SlateText = new Color(0.62f, 0.70f, 0.76f, 1f);
        static readonly Color Ink = new Color(0.03f, 0.07f, 0.12f, 1f);
        static readonly Color BarNeutral = new Color(0.09f, 0.16f, 0.22f, 1f);
        static readonly Color BarDone = new Color(0.06f, 0.24f, 0.21f, 1f);
        static readonly Color HintNeutral = new Color(0.86f, 0.92f, 0.96f, 1f);
        static readonly Color HintDone = new Color(0.80f, 1f, 0.92f, 1f);

        int lastIndex = -2;
        bool lastDone, lastUnlocked, lastBar, init;
        RectTransform continueRect;

        void OnEnable() { init = false; }

        int CurrentIndex()
        {
            if (flow == null || flow.exercises == null) return -1;
            var step = flow.CurrentStep;
            for (int i = 0; i < flow.exercises.Length; i++)
                if (flow.exercises[i].step == step) return i;
            return -1;
        }

        void LateUpdate()
        {
            if (flow == null || panel == null || flow.exercises == null || flow.exercises.Length == 0) return;
            float t = Time.unscaledTime;
            int index = CurrentIndex();
            bool done = flow.StepCompleted;
            bool unlocked = panel.continueButton != null && panel.continueButton.interactable;
            bool changed = !init || index != lastIndex || done != lastDone || unlocked != lastUnlocked;
            init = true;
            lastIndex = index; lastDone = done; lastUnlocked = unlocked;

            // Progreso segmentado: completados (turquesa), actual (ámbar o verde si ya se cumplió), pendientes (tenue).
            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                if (seg == null) continue;
                bool exists = i < flow.exercises.Length;
                if (seg.gameObject.activeSelf != exists) seg.gameObject.SetActive(exists);
                if (!exists) continue;
                if (i == index)
                {
                    var c = done || flow.CurrentStep == TutorialStep.Finished ? Green : Amber;
                    float pulse = done ? 1f : 0.82f + 0.18f * Mathf.Sin(t * 3.2f);
                    seg.color = new Color(c.r * pulse, c.g * pulse, c.b * pulse, 1f);
                }
                else if (changed) seg.color = i < index ? Teal : SegDim;
            }

            // Continuar: bloqueado = pizarra apagada + candado; desbloqueado = turquesa, check y pulso suave.
            if (continueBackground != null)
            {
                if (changed)
                {
                    continueBackground.color = unlocked ? Teal : Slate;
                    if (continueLabel != null) continueLabel.color = unlocked ? Ink : SlateText;
                    if (continueLock != null) continueLock.gameObject.SetActive(!unlocked);
                    if (continueCheck != null) continueCheck.gameObject.SetActive(unlocked);
                }
                if (continueRect == null) continueRect = continueBackground.rectTransform;
                float k = unlocked ? 1f + 0.025f * (0.5f + 0.5f * Mathf.Sin(t * 3.6f)) : 1f;
                continueRect.localScale = new Vector3(k, k, 1f);
                if (continueGlow != null)
                {
                    if (continueGlow.gameObject.activeSelf != unlocked) continueGlow.gameObject.SetActive(unlocked);
                    if (unlocked) { var g = Green; g.a = 0.55f + 0.40f * Mathf.Sin(t * 3.6f); continueGlow.color = g; }
                }
            }

            // Etiqueta de mano: solo tiene sentido en los pasos de joystick (evita texto fuera de contexto).
            if (panel.hand != null)
            {
                var handStep = flow.CurrentStep;
                bool showHand = handStep == TutorialStep.Orientation || handStep == TutorialStep.Movement || handStep == TutorialStep.Turn;
                if (panel.hand.gameObject.activeSelf != showHand) panel.hand.gameObject.SetActive(showHand);
            }

            
// Botón de práctica: anillo ámbar mientras la acción está pendiente.
            if (practiceGlow != null)
            {
                bool show = panel.practice != null && panel.practice.gameObject.activeSelf && !done && !flow.IsPaused;
                if (practiceGlow.gameObject.activeSelf != show) practiceGlow.gameObject.SetActive(show);
                if (show) { var g = Amber; g.a = 0.45f + 0.5f * (0.5f + 0.5f * Mathf.Sin(t * 3.2f)); practiceGlow.color = g; }
            }

            // Insignia de la acción: sigue al icono que ya gestiona TutorialControllerCue.
            if (actionBadge != null && actionIcon != null)
            {
                bool show = actionIcon.gameObject.activeSelf;
                if (actionBadge.gameObject.activeSelf != show) actionBadge.gameObject.SetActive(show);
            }

            // Barra de mensajes: ayuda/indicación, confirmación de logro y progreso de la espera fija.
            if (feedbackBar != null)
            {
                bool waiting = flow.CurrentStep == TutorialStep.Wait && !done;
                bool hasText = panel.hint != null && !string.IsNullOrEmpty(panel.hint.text);
                bool show = hasText || waiting;
                if (feedbackBar.gameObject.activeSelf != show) feedbackBar.gameObject.SetActive(show);
                if (show)
                {
                    if (changed || show != lastBar)
                    {
                        feedbackBar.color = done ? BarDone : BarNeutral;
                        if (panel.hint != null) panel.hint.color = done ? HintDone : HintNeutral;
                        if (feedbackIcon != null)
                        {
                            feedbackIcon.sprite = done ? checkSprite : infoSprite;
                            feedbackIcon.color = done ? Green : Teal;
                            feedbackIcon.gameObject.SetActive(hasText);
                        }
                    }
                    if (feedbackFill != null)
                    {
                        var holder = feedbackFill.transform.parent.gameObject;
                        if (holder.activeSelf != waiting) holder.SetActive(waiting);
                        if (waiting) feedbackFill.fillAmount = flow.WaitProgress;
                    }
                }
                lastBar = show;
            }
        }
    }

    // Respuesta visual al apuntar con el rayo: anillo + leve escala. No cambia el comportamiento del botón.
    [DisallowMultipleComponent]
    public class TutorialButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image ring;
        public float hoverScale = 1.04f;
        Button button;
        RectTransform rect;

        void Awake() { button = GetComponent<Button>(); rect = transform as RectTransform; SetHover(false); }
        void OnDisable() { SetHover(false); }
        public void OnPointerEnter(PointerEventData e) { if (button == null || button.interactable) SetHover(true); }
        public void OnPointerExit(PointerEventData e) { SetHover(false); }

        void SetHover(bool on)
        {
            if (ring != null) ring.gameObject.SetActive(on);
            if (rect != null) rect.localScale = on ? new Vector3(hoverScale, hoverScale, 1f) : Vector3.one;
        }
    }
}
