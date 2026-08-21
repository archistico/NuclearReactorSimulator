using System.Globalization;

namespace NuclearReactorSimulator.App.Presentation;

/// <summary>Technical HMI engineering-number formatting. Localization policy may replace this only as one coherent contract.</summary>
internal static class EngineeringNumberFormatter
{
    public static string Compact(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
