using System;
using System.Globalization;
using UnityEngine;

namespace CrisisVR
{
    public static class SessionRuntime
    {
        public static SessionData Current { get; private set; }
        static int eventSequence;

        public static void Begin(string sessionId)
        {
            Current = new SessionData
            {
                sessionId = sessionId,
                startedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                appVersion = Application.version,
                endReason = "En curso"
            };
            eventSequence = 0;
        }

        public static void EnsureSession()
        {
            if (Current != null) return;
            if (!string.IsNullOrEmpty(SessionCode.Current))
                Begin(SessionCode.Current);
            else
                SessionCode.NewSession();
        }

        public static void LogEvent(string eventName)
        {
            EnsureSession();
            string entry = string.Format(CultureInfo.InvariantCulture, "{0:00} {1}", ++eventSequence, eventName);
            Current.eventLog.Add(entry);
            Debug.Log("SESSION_EVENT: " + entry);
        }

        public static void RecordAct1(bool reintento)
        {
            EnsureSession();
            if (Current.act1Registered) return;

            Current.Reintento = reintento;
            Current.Act1Ruta = reintento ? "Corregir" : "NoActuo";
            Current.act1Registered = true;
            LogEvent(reintento ? "ACTO1_ACCION_CORREGIR" : "ACTO1_SIN_ACCION");
        }

        public static string CompleteAct1()
        {
            EnsureSession();
            if (!Current.act1Registered)
            {
                Debug.LogError("No se puede cerrar Acto 1 sin registrar una decisión.");
                return null;
            }

            LogEvent("ACTO1_COMPLETADO");
            Current.act1Completed = true;
            // Se guarda un reporte parcial; los actos siguientes aún no se han completado.
            Current.sessionCompleted = false;
            Current.endReason = "En curso";
            return ReportWriter.Write(Current);
        }
    }
}
