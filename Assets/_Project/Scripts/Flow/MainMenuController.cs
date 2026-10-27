using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CrisisVR
{
    public class MainMenuController : MonoBehaviour
    {
        public TMP_Text sessionCodeLabel;
        public Button startButton;
        public string nextSceneName = "Tutorial";

void Awake()
        {
            string code = SessionCode.NewSession();
            if (sessionCodeLabel != null)
                sessionCodeLabel.text = "Versión 0.1  ·  Código de sesión: " + code;

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartSession);
                startButton.onClick.AddListener(StartSession);
            }

            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>(true);
            if (origin != null)
            {
                origin.Origin = origin.gameObject;
                origin.CameraYOffset = 1.6f;
                origin.RequestedTrackingOriginMode = Application.isEditor
                    ? Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Device
                    : Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
                if (origin.CameraFloorOffsetObject != null)
                {
                    var offset = origin.CameraFloorOffsetObject.transform;
                    var position = offset.localPosition;
                    position.y = Application.isEditor ? origin.CameraYOffset : 0f;
                    offset.localPosition = position;
                }
            }

            // En el menú, el gatillo del mando derecho activa el botón de inicio.
            var interactors = FindObjectsOfType<NearFarInteractor>(true);
            foreach (var interactor in interactors)
                if (interactor.transform.parent != null && interactor.transform.parent.name.Contains("Right Controller"))
                    interactor.selectInput = interactor.uiPressInput;

            foreach (var provider in FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>(true)) provider.enabled = false;
            foreach (var provider in FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider>(true)) provider.enabled = false;
            foreach (var provider in FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>(true)) provider.enabled = false;
        }

        public void StartSession()
        {
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                Debug.LogError("No se encontró la escena de inicio: " + nextSceneName);
        }
    }

    public static class SessionCode
    {
        public static string Current { get; private set; }

        public static string NewSession()
        {
            Current = "CVR-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            SessionRuntime.Begin(Current);
            return Current;
        }
    }
}
