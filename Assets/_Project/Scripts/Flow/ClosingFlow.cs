using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrisisVR
{
    // Cierre: agradece sin mostrar ninguna decisión (propuesta V-14). A los 10 s aparece el panel
    // de quien conduce la sesión con NUEVA SESIÓN o SALIR.
    public sealed class ClosingFlow : MonoBehaviour
    {
        public const float ConductorPanelDelay = 10f;

        [SerializeField] GameObject closingPanel;
        [SerializeField] GameObject conductorPanel;
        [SerializeField] Button newSessionButton;
        [SerializeField] Button exitButton;
        [SerializeField] string firstSceneName = "MainMenu";

        public bool Started { get; private set; }
        public bool ConductorPanelVisible => conductorPanel != null && conductorPanel.activeSelf;

        void Awake()
        {
            if (closingPanel != null) closingPanel.SetActive(false);
            if (conductorPanel != null) conductorPanel.SetActive(false);
            if (newSessionButton != null) newSessionButton.onClick.AddListener(NewSession);
            if (exitButton != null) exitButton.onClick.AddListener(Exit);
        }

        public void Begin()
        {
            if (Started) return;
            Started = true;
            SessionRuntime.LogEvent("CIERRE");
            if (closingPanel != null) closingPanel.SetActive(true);
            StartCoroutine(ShowConductorPanel());
        }

        IEnumerator ShowConductorPanel()
        {
            yield return new WaitForSecondsRealtime(ConductorPanelDelay);
            ShowConductorPanelNow();
        }

        public void ShowConductorPanelNow()
        {
            if (conductorPanel != null) conductorPanel.SetActive(true);
        }

        public void NewSession()
        {
            // MainMenu crea la sesión nueva (y su código) al cargarse.
            if (SceneFader.IsLoading) return;
            GamePause.Reset();
            if (Application.CanStreamedLevelBeLoaded(firstSceneName))
                SceneFader.LoadScene(firstSceneName);
            else
                Debug.LogError("No se encontró la escena inicial: " + firstSceneName);
        }

        public void Exit()
        {
            Debug.Log("CrisisVR: salida solicitada desde el panel de cierre.");
            Application.Quit();
        }
    }
}
