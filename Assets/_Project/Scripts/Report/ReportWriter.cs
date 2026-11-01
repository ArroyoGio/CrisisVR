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
            string path = Path.Combine(Application.persistentDataPath, ReportBuilder.FileName(session.sessionId));
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                // UTF-8 sin BOM para que los acentos se lean bien al copiar el archivo desde el visor.
                File.WriteAllText(path, ReportBuilder.Build(session), new UTF8Encoding(false));
                Debug.Log("Reporte guardado (" + session.endReason + ") en: " + path);
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
