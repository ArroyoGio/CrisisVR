using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace CrisisVR
{
    public static class ReportWriter
    {
        public static string Write(SessionData session)
        {
            string path = Path.Combine(Application.persistentDataPath, "CrisisVR_" + session.sessionId + ".txt");
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(path, ReportBuilder.Build(session), new UTF8Encoding(false));
                Debug.Log("Reporte parcial guardado en: " + path);
                return path;
            }
            catch (Exception exception)
            {
                Debug.LogError("No se pudo guardar el reporte de la sesión: " + exception.Message);
                return null;
            }
        }
    }
}
