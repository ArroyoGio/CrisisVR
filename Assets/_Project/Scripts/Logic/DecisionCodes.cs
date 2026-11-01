using System;

namespace CrisisVR
{
    public static class DecisionCodes
    {
        public const int Adapt_None = 0, Adapt_PlanNuevo = 1, Adapt_PlanOriginal = 2, Adapt_SinAccion = 3;
        public const int Team_None = 0, Team_A = 1, Team_B = 2, Team_C = 3, Team_D = 4;

        public static string TeamLetter(int code)
        {
            if (code < Team_A || code > Team_D)
                throw new ArgumentOutOfRangeException(nameof(code), "La opción debe estar entre 1 y 4.");
            return ((char)('A' + code - 1)).ToString();
        }
    }
}
