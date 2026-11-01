using System;

namespace CrisisVR
{
    // Tiempo de espera del diseño; no mide el tiempo de respuesta del estudiante.
    public sealed class DecisionTimer
    {
        public DecisionTimer(float durationSeconds)
        {
            if (!(durationSeconds > 0) || float.IsInfinity(durationSeconds))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "La duración debe ser positiva y finita.");
            Duration = durationSeconds;
            Remaining = durationSeconds;
        }

        public float Duration { get; }
        public float Remaining { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public bool Expired { get; private set; }

        public void Start()
        {
            Remaining = Duration;
            Running = true;
            Expired = false;
        }

        public void Stop() { Running = false; }
        public void SetPaused(bool paused) { Paused = paused; }

        public bool Tick(float deltaSeconds)
        {
            if (!Running || Paused || !(deltaSeconds >= 0)) return false;
            Remaining = Math.Max(0f, Remaining - deltaSeconds);
            if (Remaining > 0) return false;
            Running = false;
            Expired = true;
            return true;
        }
    }
}
