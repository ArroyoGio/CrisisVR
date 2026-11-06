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
        [Header("Portada")]
        public GameObject startPanel;
        public TMP_Text sessionCodeLabel;
        public Button startButton;

        [Header("Bienvenida")]
        public GameObject welcomePanel;
        public TMP_Text welcomeSessionCodeLabel;
        public Button continueButton;

        public string nextSceneName = "Tutorial";

void Awake()
        {
            string code = SessionCode.NewSession();
            string label = "Versión 0.1  ·  Código de sesión: " + code;
            if (sessionCodeLabel != null) sessionCodeLabel.text = label;
            if (welcomeSessionCodeLabel != null) welcomeSessionCodeLabel.text = label;

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartSession);
                startButton.onClick.AddListener(StartSession);
            }
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(Continue);
                continueButton.onClick.AddListener(Continue);
            }
            ShowPanel(false);

            var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include);
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
            var interactors = FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var interactor in interactors)
                if (interactor.transform.parent != null && interactor.transform.parent.name.Contains("Right Controller"))
                    interactor.selectInput = interactor.uiPressInput;

            foreach (var provider in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) provider.enabled = false;
            foreach (var provider in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) provider.enabled = false;
            foreach (var provider in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None)) provider.enabled = false;
        }

        // INICIAR: pasa de la portada a la bienvenida (solo cambia de panel, no de escena).
        public void StartSession()
        {
            // Tolerante a referencias sin asignar: el flujo nunca debe bloquearse.
            if (welcomePanel == null) { Continue(); return; }
            ShowPanel(true);
        }

        // CONTINUAR: carga el tutorial.
        public void Continue()
        {
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneFader.LoadScene(nextSceneName);
            else
                Debug.LogError("No se encontró la escena de inicio: " + nextSceneName);
        }

        void ShowPanel(bool welcome)
        {
            if (startPanel != null) startPanel.SetActive(!welcome);
            if (welcomePanel != null) welcomePanel.SetActive(welcome);
        }
    }

    public static class SessionCode
    {
        public static string Current { get; private set; }

        public static string NewSession()
        {
            // Formato de la guía v1.0: CVR-AAAAMMDD-HHMMSS-XXXX (seudónimo, sin datos personales).
            Current = SessionRuntime.NewSessionId();
            SessionRuntime.Begin(Current);
            return Current;
        }
    }
}
