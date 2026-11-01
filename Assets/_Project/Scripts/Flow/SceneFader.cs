using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrisisVR
{
    // Fundido a negro entre escenas (respuesta de la tesista, 27/10/2026). La sesión vive en SessionRuntime
    // (estático), así que sus datos se mantienen al cambiar de escena. Se usa un Canvas world-space pegado
    // a la cámara porque un overlay de pantalla no se ve dentro del visor.
    public sealed class SceneFader : MonoBehaviour
    {
        public const float FadeDuration = 1f;

        static SceneFader instance;
        Canvas canvas;
        CanvasGroup group;
        bool loading;
        string pendingScene;
        bool fadingIn;

        public static bool IsLoading => instance != null && instance.loading;

        public static void LoadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("No se encontró la escena: " + sceneName);
                return;
            }
            Ensure().StartCoroutine(instance.FadeAndLoad(sceneName));
        }

        static SceneFader Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("CrisisVR Scene Fader", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneFader>();
            instance.canvas = go.GetComponent<Canvas>();
            instance.canvas.renderMode = RenderMode.WorldSpace;
            instance.canvas.overrideSorting = true;
            instance.canvas.sortingOrder = 1000;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(4000f, 4000f);
            instance.group = go.GetComponent<CanvasGroup>();
            instance.group.alpha = 0f;
            instance.group.interactable = false;
            instance.group.blocksRaycasts = false;

            var image = new GameObject("Negro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            image.transform.SetParent(go.transform, false);
            var imageRect = (RectTransform)image.transform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            var img = image.GetComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return instance;
        }

        void AttachToCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;
            transform.SetParent(camera.transform, false);
            transform.localPosition = new Vector3(0f, 0f, .3f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * .001f;
            canvas.worldCamera = camera;
        }

        IEnumerator FadeAndLoad(string sceneName)
        {
            if (loading)
            {
                // Solo se recuerda lo pedido con la escena nueva ya visible (fundido de entrada);
                // un doble clic durante la salida se ignora para no cargar dos veces.
                if (fadingIn) pendingScene = sceneName;
                yield break;
            }
            loading = true;
            AttachToCamera();
            yield return Fade(0f, 1f);
            // Se suelta de la cámara antes de descargar la escena para no destruirse con ella.
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (!operation.isDone) yield return null;
            GamePause.Reset();
            yield return null;
            AttachToCamera();
            fadingIn = true;
            yield return Fade(1f, 0f);
            fadingIn = false;
            loading = false;
            if (pendingScene != null)
            {
                string next = pendingScene;
                pendingScene = null;
                StartCoroutine(FadeAndLoad(next));
            }
        }

        IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / FadeDuration));
                yield return null;
            }
            group.alpha = to;
        }
    }
}
