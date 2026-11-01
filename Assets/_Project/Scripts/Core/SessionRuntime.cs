using System;
using UnityEngine;

namespace CrisisVR
{
    // Puente entre los MonoBehaviour de los actos y la lógica pura de CrisisVR.Logic.
    // Guarda la sesión activa entre escenas y escribe el reporte en los momentos que fija la guía.
    public static class SessionRuntime
    {
        static SessionRecorder recorder;
        static readonly System.Random Random = new System.Random();

        public static SessionData Current => recorder?.Data;
        public static bool IsFinished => recorder != null && recorder.IsFinished;
        // Ruta del último reporte escrito (se conserva al empezar otra sesión, para quien conduce y las pruebas).
        public static string LastReportPath { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void LogReportFolder()
        {
            // La guía pide escribir la ruta en la consola al iniciar, para copiar el reporte con adb o MQDH.
            Debug.Log("CrisisVR: los reportes se guardan en " + Application.persistentDataPath);
        }

        public static string NewSessionId() => SessionIdFactory.Create(DateTime.Now, Random);

        public static void Begin(string sessionId)
        {
            recorder = new SessionRecorder(sessionId, DateTime.Now, Application.version);
        }

        public static void EnsureSession()
        {
            // Una sesión completa o interrumpida nunca se reutiliza: cada recorrido tiene su propio código y archivo.
            if (recorder != null && !recorder.IsFinished) return;
            if (recorder == null && !string.IsNullOrEmpty(SessionCode.Current))
                Begin(SessionCode.Current);
            else
                SessionCode.NewSession();
        }

        public static void LogEvent(string eventName)
        {
            if (recorder == null) EnsureSession();
            // Tras completar o interrumpir, el registro queda cerrado (p. ej. el evento CIERRE no abre otra sesión).
            if (recorder.IsFinished)
            {
                Debug.Log("SESSION_EVENT (sesión cerrada, no se registra): " + eventName);
                return;
            }
            Debug.Log("SESSION_EVENT: " + recorder.LogEvent(eventName));
        }

        public static void RecordAct1(bool reintento)
        {
            EnsureSession();
            recorder.RecordAct1(reintento);
        }

        public static string CompleteAct1()
        {
            EnsureSession();
            if (!Current.act1Registered)
            {
                Debug.LogError("No se puede cerrar el Acto 1 sin registrar una decisión.");
                return null;
            }
            recorder.CompleteAct1();
            // Reporte de respaldo «En curso»: si la aplicación se cierra a la fuerza, queda lo registrado.
            return WriteReport();
        }

        public static void RecordAct2(int opcion)
        {
            EnsureSession();
            recorder.RecordAct2(opcion);
        }

        public static string CompleteAct2()
        {
            EnsureSession();
            if (!Current.act2Registered)
            {
                Debug.LogError("No se puede cerrar el Acto 2 sin registrar una decisión.");
                return null;
            }
            recorder.CompleteAct2();
            return WriteReport();
        }

        public static void RecordAct3(int opcion)
        {
            EnsureSession();
            recorder.RecordAct3(opcion);
        }

        public static string CompleteAct3()
        {
            EnsureSession();
            if (!Current.act3Registered)
            {
                Debug.LogError("No se puede cerrar el Acto 3 sin registrar una decisión.");
                return null;
            }
            recorder.CompleteAct3();
            return WriteReport();
        }

        // TERMINAR SESIÓN desde la pausa: reporte parcial con estado «Interrumpida».
        public static string Interrupt()
        {
            if (recorder == null || recorder.IsFinished) return LastReportPath;
            recorder.Interrupt();
            return WriteReport();
        }

        public static string WriteReport()
        {
            if (recorder == null) return null;
            LastReportPath = ReportWriter.Write(recorder.Data);
            return LastReportPath;
        }
    }
}
