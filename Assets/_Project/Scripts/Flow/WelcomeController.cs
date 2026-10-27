using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrisisVR
{
    public class WelcomeController : MonoBehaviour
    {
        public TMP_Text sessionCodeLabel;
        public Button continueButton;
        public string nextSceneName = "Tutorial";

        private void Awake()
        {
            if (sessionCodeLabel != null)
            {
                string code = string.IsNullOrEmpty(SessionCode.Current) ? SessionCode.NewSession() : SessionCode.Current;
                sessionCodeLabel.text = "Versión 0.1  ·  Código de sesión: " + code;
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(Continue);
                continueButton.onClick.AddListener(Continue);
            }
        }

        public void Continue()
        {
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                Debug.LogError("No se encontró la escena siguiente: " + nextSceneName);
        }
    }
}