using System;

namespace CrisisVR
{
    public static class ReportBuilder
    {
        public static string FileName(string sessionId) => "CrisisVR_" + sessionId + ".txt";

        public static string Build(SessionData s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s), "Se requieren los datos de la sesión.");
            string block1 = s.act1Registered
                ? string.Join("\n", ReportTexts.Act1Event, s.Reintento ? ReportTexts.RetryDecision : ReportTexts.NoActionDecision, ReportTexts.Act1Question)
                : ReportTexts.NotRegistered;
            string block2 = s.OpcionAdaptabilidad != 0
                ? string.Join("\n", ReportTexts.Act2Event, ReportTexts.Act2Decision(s.OpcionAdaptabilidad), ReportTexts.Act2Question)
                : ReportTexts.NotRegistered;
            string block3 = s.OpcionTrabajoEnEquipo != 0
                ? string.Join("\n", ReportTexts.Act3Event,
                    "Decisión registrada: ante el conflicto en el equipo, el estudiante eligió la opción: «" + ReportTexts.Act3OptionText(s.OpcionTrabajoEnEquipo) + "»", ReportTexts.Act3Question)
                : ReportTexts.NotRegistered;
            return string.Join("\n",
                ReportTexts.Header,
                "Código de sesión: " + s.sessionId,
                "Fecha y hora de inicio: " + s.startedAt,
                "Versión de la aplicación: " + s.appVersion,
                "Estado de la sesión: " + s.endReason,
                "",
                "AVISO DE LÍMITES", ReportTexts.Notice,
                "",
                "SITUACIÓN 1 — Retroalimentación negativa del docente",
                "Situación diseñada en relación con: autorregulación emocional (marco del proyecto; no se mide).",
                block1,
                "",
                "SITUACIÓN 2 — Aviso de cambio de modalidad y de grupo",
                "Situación diseñada en relación con: adaptabilidad (marco del proyecto; no se mide).",
                block2,
                "",
                "SITUACIÓN 3 — Reunión de equipo",
                "Situación diseñada en relación con: trabajo en equipo (marco del proyecto; no se mide).",
                block3,
                "",
                "CALIDAD DEL REGISTRO",
                "Situación 1 completada: " + (s.act1Completed ? "sí" : "no"),
                "Situación 2 completada: " + (s.act2Completed ? "sí" : "no"),
                "Situación 3 completada: " + (s.act3Completed ? "sí" : "no"),
                ReportTexts.Quality,
                "",
                "LIMITACIONES DE INTERPRETACIÓN", ReportTexts.Limits) + "\n";
        }
    }
}