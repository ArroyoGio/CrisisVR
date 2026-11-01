using System;
using UnityEngine;

namespace CrisisVR
{
    // Estado global de pausa de los actos. Congela el tiempo de juego (y con él las esperas de decisión)
    // y el audio; los diálogos quedan detenidos porque el panel de pausa cubre el botón CONTINUAR.
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }
        public static event Action<bool> Changed;

        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            Changed?.Invoke(paused);
        }

        // Al cambiar de escena o reiniciar la sesión nunca se hereda una pausa.
        public static void Reset()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
