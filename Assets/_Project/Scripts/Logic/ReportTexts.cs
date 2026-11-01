using System;
using System.Globalization;

namespace CrisisVR
{
    public static class ReportTexts
    {
        public const string Act1EntryCard = "Situación 1 de 3";
        public const string Act1ExitCard = "Fin de la situación 1 de 3";
        public const string Header = "CRISIS VR — REPORTE DESCRIPTIVO DE SESIÓN";
        public const string Notice = "Este reporte describe decisiones y acciones del estudiante en una simulación breve. No constituye un diagnóstico ni una evaluación clínica, no asigna niveles ni puntajes y no reemplaza el criterio del profesional de Psicopedagogía.";
        public const string NotRegistered = "Sin registro: la sesión terminó antes de esta situación.";
        public const string Act1Event = "Lo que ocurrió: el estudiante entregó su proyecto y el docente le comunicó una calificación de 08 sobre 20 y la retroalimentación correspondiente.";
        public const string RetryDecision = "Decisión registrada: tras recibir la retroalimentación, el estudiante seleccionó la opción de corregir el proyecto.";
        public const string NoActionDecision = "Decisión registrada: tras recibir la retroalimentación, el estudiante no seleccionó ninguna acción durante el tiempo disponible, y la sesión continuó.";
        public const string Act1Question = "Pregunta orientadora: ¿Qué pensabas mientras escuchabas la retroalimentación del profesor?";
        public const string Act2Event = "Lo que ocurrió: llegó un aviso de cambio de la práctica a modalidad virtual y de reorganización de los grupos; apareció un compañero nuevo.";
        public const string Act2Question = "Pregunta orientadora: ¿Qué te llevó a elegir esa opción (o a no elegir)?";
        public const string Act3Event = "Lo que ocurrió: en una videollamada, dos compañeros discreparon sobre el reparto de tareas y un integrante informó que no tenía lista su parte.";
        public const string Act3Question = "Pregunta orientadora: ¿Qué pensaste mientras tus compañeros discutían?";
        public const string Quality = "En las situaciones 1 y 2, la ausencia de selección se determina porque vence un tiempo de espera definido en el diseño; no se mide el tiempo de respuesta del estudiante.";
        public const string Limits = "Este reporte refleja una decisión por situación en una simulación acotada. Una decisión puede obedecer a razones muy distintas. Solo cobra sentido dentro de la conversación del profesional con el estudiante y junto con otras fuentes de información.";

        public static string EntryCard(int n) => "Situación " + n.ToString(CultureInfo.InvariantCulture) + " de 3";
        public static string ExitCard(int n) => "Fin de la situación " + n.ToString(CultureInfo.InvariantCulture) + " de 3";

        public static string Act2Decision(int code)
        {
            switch (code)
            {
                case 1: return "Decisión registrada: ante el aviso, el estudiante seleccionó el plan nuevo (práctica virtual).";
                case 2: return "Decisión registrada: ante el aviso, el estudiante seleccionó el plan original (práctica presencial).";
                case 3: return "Decisión registrada: ante el aviso, el estudiante no seleccionó ninguna opción durante el tiempo disponible, y la sesión continuó.";
                default: throw new ArgumentOutOfRangeException(nameof(code), "La opción debe estar entre 1 y 3.");
            }
        }

        public static string Act3OptionText(int code)
        {
            switch (code)
            {
                case 1: return "Propongo que cada uno diga qué parte puede entregar y para cuándo, y armamos un plan juntos.";
                case 2: return "Lucía, yo puedo ayudarte con la presentación para avanzar más rápido.";
                case 3: return "Mateo no ha avanzado nada; sigamos sin él y repartamos su parte entre nosotros.";
                case 4: return "Prefiero esperar a ver qué deciden ustedes.";
                default: throw new ArgumentOutOfRangeException(nameof(code), "La opción debe estar entre 1 y 4.");
            }
        }
    }
}