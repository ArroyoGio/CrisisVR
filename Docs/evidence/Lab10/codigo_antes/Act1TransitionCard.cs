using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrisisVR
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class Act1TransitionCard : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TMP_Text label;
        [SerializeField] Image background;
        [SerializeField, Min(.05f)] float fadeDuration = .55f;
        [SerializeField, Min(0f)] float holdDuration = .65f;

        void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (background != null) background.raycastTarget = false;
            if (label != null) label.raycastTarget = false;
        }

        public void ShowEntry(Action completed) => ShowEntry(1, completed);
        public void ShowExit(Action completed) => ShowExit(1, completed);
        // Tarjeta «Situación N de 3» de la guía; la cámara nunca se mueve durante la transición.
        public void ShowEntry(int situation, Action completed) => StartCoroutine(Play(ReportTexts.EntryCard(situation), true, completed));
        public void ShowExit(int situation, Action completed) => StartCoroutine(Play(ReportTexts.ExitCard(situation), false, completed));

        // Retira la tarjeta (de negro a transparente); la usa el cierre, que no tiene tarjeta de entrada.
        public void Clear(Action completed = null)
        {
            StopAllCoroutines();
            StartCoroutine(Fade(canvasGroup.alpha, 0f, completed));
        }

        IEnumerator Fade(float start, float end, Action completed)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (!GamePause.IsPaused) elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, end, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            canvasGroup.alpha = end;
            completed?.Invoke();
        }

        // Espera de la tarjeta que se detiene mientras el juego está en pausa.
        static IEnumerator Hold(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (!GamePause.IsPaused) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        IEnumerator Play(string message, bool fadeOut, Action completed)
        {
            if (label != null) label.text = message;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            float start = fadeOut ? 1f : 0f;
            float end = fadeOut ? 0f : 1f;
            canvasGroup.alpha = start;

            if (fadeOut) yield return Hold(holdDuration);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (!GamePause.IsPaused) elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, end, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            canvasGroup.alpha = end;

            if (!fadeOut) yield return Hold(holdDuration);
            while (GamePause.IsPaused) yield return null;
            completed?.Invoke();
        }
    }
}
