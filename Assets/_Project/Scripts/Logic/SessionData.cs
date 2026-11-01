using System;
using System.Collections.Generic;

namespace CrisisVR
{
    [Serializable]
    public sealed class SessionData
    {
        public string sessionId;
        public string startedAt;
        public string appVersion;
        public const string EndInProgress = "En curso", EndCompleted = "Completa", EndInterrupted = "Interrumpida";
        public string endReason = EndInProgress;
        public bool sessionCompleted;
        public bool Reintento;
        public bool act1Registered;
        public bool act1Completed;
        public string Act1Ruta;
        public bool act2Registered;
        public bool act2Completed;
        public int OpcionAdaptabilidad;
        public bool act3Registered;
        public bool act3Completed;
        public int OpcionTrabajoEnEquipo;
        public List<string> eventLog = new List<string>();
    }
}
