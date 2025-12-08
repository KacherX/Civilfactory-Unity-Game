using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalFunctions // Functions that called from many locations.
{
    private static string[] numLibrary = { "k", "M", "B", "T", "Qa", "Qn", "Sx", "Sp", "Oc", "No", "De" };

    public static string NumberMakeUp(double num, bool returnScientific = false)
    {
        if (num < 1000) return num.ToString("0"); // No formatting for numbers below 1000

        int exponent = (int)Math.Floor(Math.Log10(num)); // Get the order of magnitude
        int index = (exponent - 3) / 3; // Determine suffix index

        if (index < numLibrary.Length && !returnScientific)
        {
            double shortNum = num / Math.Pow(10, (index * 3) + 3); // Scale down to readable format
            return shortNum.ToString("0.##") + numLibrary[index]; // Format with up to 2 decimal places
        }
        else
        {
            return (num / Math.Pow(10, exponent)).ToString("0.##") + "e" + exponent; // Scientific notation for large numbers
        }
    }

    public static float ExtractNumberFromString(string name)
    {
        string value = "";
        foreach (char c in name)
        {
            if (char.IsDigit(c) || c == '.')
            {
                value += c;
            }
            else if (value.Length > 0) // Stop when first number ends
            {
                break;
            }
        }

        return float.Parse(value);
    }
}
