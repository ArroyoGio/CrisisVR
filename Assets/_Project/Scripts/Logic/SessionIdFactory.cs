using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CrisisVR
{
    public static class SessionIdFactory
    {
        public static string Create(DateTime now, Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random), "Se requiere un generador aleatorio.");
            return "CVR-" + now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                + "-" + random.Next(0, 0x10000).ToString("X4", CultureInfo.InvariantCulture);
        }

        public static bool IsValid(string id)
        {
            return id != null && Regex.IsMatch(id, @"\ACVR-\d{8}-\d{6}-[0-9A-F]{4}\z");
        }
    }
}
