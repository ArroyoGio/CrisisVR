using TMPro;
using UnityEngine;

namespace CrisisVR
{
    public class TutorialPanel : MonoBehaviour
    {
        public TMP_Text heading, instruction, hint, hand;
        public TMP_Text progress; // Opcional: si queda vacio se crea en runtime.
        public string leftHandLabel, rightHandLabel;
        public UnityEngine.UI.Button practice, continueButton, repeatButton;

        // PROPUESTA - VALIDAR CON CLIENTA
        const string ProgressFormat = "Paso {0} de {1}";
        static readonly Color ProgressColor = new Color(0.60f, 0.70f, 0.78f, 1f);
        const int LongInstruction = 160; // Umbral para activar el ajuste de legibilidad.

        public void Show(TutorialExercise exercise, bool right, bool showPractice)
        {
            Show(exercise, right, showPractice, -1, -1);
        }

        public void Show(TutorialExercise exercise, bool right, bool showPractice, int index, int total)
        {
            // Tolerante a referencias sin asignar: el tutorial nunca debe bloquearse.
            if (heading != null) heading.text = exercise.title;
            if (instruction != null) instruction.text = exercise.instruction;
            if (hint != null) hint.text = string.Empty;
            if (hand != null) hand.text = right ? rightHandLabel : leftHandLabel;
            if (practice != null) practice.gameObject.SetActive(showPractice);
            if (continueButton != null) continueButton.interactable = exercise.step != TutorialStep.Finished;
            if (repeatButton != null) repeatButton.interactable = exercise.step != TutorialStep.Finished;

            FitInstruction();
            SetProgress(index, total);
        }

        public void ShowHelp(string content)
        {
            if (hint != null) hint.text = content;
        }

        // Muestra "Paso X de Y" para orientar sin valorar el desempeño.
        public void SetProgress(int index, int total)
        {
            if (index < 0 || total <= 0) return;
            EnsureProgress();
            if (progress != null) progress.text = string.Format(ProgressFormat, index + 1, total);
        }

        // Reduce el cuerpo solo cuando la instruccion es larga, para que no se corte.
        void FitInstruction()
        {
            if (instruction == null || instruction.text == null) return;
            bool largo = instruction.text.Length > LongInstruction;
            instruction.enableAutoSizing = largo;
            if (!largo) return;
            instruction.fontSizeMin = 19f;
            instruction.fontSizeMax = 29f;
        }

        // Crea el texto de progreso una sola vez, reutilizando el estilo del panel.
        void EnsureProgress()
        {
            if (progress != null) return;
            TMP_Text source = hand != null ? hand : heading;
            if (source == null) return;

            var go = new GameObject("PasoProgreso", typeof(RectTransform));
            go.layer = source.gameObject.layer;
            go.transform.SetParent(source.transform.parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = source.font;
            tmp.fontSharedMaterial = source.fontSharedMaterial;
            tmp.fontSize = 25f;
            tmp.color = ProgressColor;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.raycastTarget = false;
            tmp.text = string.Empty;

            // Zona libre del panel (no se superpone a marca, instruccion ni botones).
            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(600f, 160f);
            rt.sizeDelta = new Vector2(280f, 44f);

            progress = tmp;
        }
    }
}
