using System;
using System.Globalization;

namespace CrisisVR
{
    public sealed class SessionRecorder
    {
        private int eventSequence;

        public SessionRecorder(string sessionId, DateTime startedAt, string appVersion)
        {
            Data = new SessionData
            {
                sessionId = sessionId,
                startedAt = startedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                appVersion = appVersion
            };
        }

        public SessionData Data { get; }
        public bool IsFinished => Data.sessionCompleted || Data.endReason == SessionData.EndInterrupted;

        public string LogEvent(string name)
        {
            string entry = (++eventSequence).ToString("D2", CultureInfo.InvariantCulture) + " " + name;
            Data.eventLog.Add(entry);
            return entry;
        }

        public bool RecordAct1(bool reintento)
        {
            if (IsFinished || Data.act1Registered) return false;
            Data.act1Registered = true;
            Data.Reintento = reintento;
            Data.Act1Ruta = reintento ? "Corregir" : "NoActuo";
            LogEvent(reintento ? "ACTO1_ACCION_CORREGIR" : "ACTO1_SIN_ACCION");
            return true;
        }

        public bool RecordAct2(int opcion)
        {
            if (IsFinished) return false;
            if (opcion < 1 || opcion > 3)
                throw new ArgumentOutOfRangeException(nameof(opcion), "La opción debe estar entre 1 y 3.");
            if (Data.act2Registered) return false;
            Data.act2Registered = true;
            Data.OpcionAdaptabilidad = opcion;
            LogEvent(opcion == 1 ? "ACTO2_PLAN_NUEVO" : opcion == 2 ? "ACTO2_PLAN_ORIGINAL" : "ACTO2_SIN_ACCION");
            return true;
        }

        public bool RecordAct3(int opcion)
        {
            if (IsFinished) return false;
            string letter = DecisionCodes.TeamLetter(opcion);
            if (Data.act3Registered) return false;
            Data.act3Registered = true;
            Data.OpcionTrabajoEnEquipo = opcion;
            LogEvent("ACTO3_OPCION_" + letter);
            return true;
        }

        public void CompleteAct1()
        {
            if (!Data.act1Registered) throw new InvalidOperationException("No se puede completar la situación 1 sin registro.");
            if (IsFinished || Data.act1Completed) return;
            Data.act1Completed = true;
            LogEvent("ACTO1_COMPLETADO");
        }

        public void CompleteAct2()
        {
            if (!Data.act2Registered) throw new InvalidOperationException("No se puede completar la situación 2 sin registro.");
            if (IsFinished || Data.act2Completed) return;
            Data.act2Completed = true;
            LogEvent("ACTO2_COMPLETADO");
        }

        public void CompleteAct3()
        {
            if (!Data.act3Registered) throw new InvalidOperationException("No se puede completar la situación 3 sin registro.");
            if (IsFinished || Data.act3Completed) return;
            Data.act3Completed = true;
            LogEvent("ACTO3_COMPLETADO");
            Data.sessionCompleted = true;
            Data.endReason = SessionData.EndCompleted;
            LogEvent("SESION_COMPLETA");
        }

        public void Interrupt()
        {
            if (IsFinished) return;
            Data.endReason = SessionData.EndInterrupted;
            LogEvent("SESION_INTERRUMPIDA");
        }
    }
}
