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
        public string endReason = "En curso";
        public bool sessionCompleted;
        public bool Reintento;
        public bool act1Registered;
        public bool act1Completed;
        public string Act1Ruta;
        public List<string> eventLog = new List<string>();
    }
}
