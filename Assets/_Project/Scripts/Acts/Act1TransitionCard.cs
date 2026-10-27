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

        public void ShowEntry(Action completed) => StartCoroutine(Play(ReportTexts.Act1EntryCard, true, completed));
        public void ShowExit(Action completed) => StartCoroutine(Play(ReportTexts.Act1ExitCard, false, completed));

        IEnumerator Play(string message, bool fadeOut, Action completed)
        {
            if (label != null) label.text = message;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            float start = fadeOut ? 1f : 0f;
            float end = fadeOut ? 0f : 1f;
            canvasGroup.alpha = start;

            if (fadeOut) yield return new WaitForSecondsRealtime(holdDuration);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, end, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            canvasGroup.alpha = end;

            if (!fadeOut) yield return new WaitForSecondsRealtime(holdDuration);
            completed?.Invoke();
        }
    }
}
