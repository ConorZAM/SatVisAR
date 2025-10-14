using System;
using UnityEngine;

public static class SigFigFormatter
{
    public static string ToSignificantFigures(this float value, int sigFigs)
    {
        if (value == 0)
        {
            return "0";
        }

        // Order of magnitude of the number
        int magnitude = Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(value))) + 1;

        // Number of decimal places to show
        int decimals = sigFigs - magnitude;
        if (decimals < 0)
        {
            decimals = 0;
        }

        return Math.Round(value, decimals, MidpointRounding.AwayFromZero)
                   .ToString($"F{decimals}");
    }
}