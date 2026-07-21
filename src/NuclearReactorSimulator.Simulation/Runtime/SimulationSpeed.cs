namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Represents one of the deterministic simulation-speed multipliers supported by the runtime.
/// Multipliers are stored in quarter-units so scaling never depends on binary floating-point arithmetic.
/// </summary>
public sealed record SimulationSpeed
{
    private const int ScaleDenominator = 4;

    private SimulationSpeed(int quarterUnits, string label)
    {
        if (quarterUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quarterUnits), "Simulation speed must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        QuarterUnits = quarterUnits;
        Label = label;
    }

    public static SimulationSpeed Quarter { get; } = new(1, "0.25×");

    public static SimulationSpeed Half { get; } = new(2, "0.5×");

    public static SimulationSpeed Normal { get; } = new(4, "1×");

    public static SimulationSpeed Double { get; } = new(8, "2×");

    public static SimulationSpeed FiveTimes { get; } = new(20, "5×");

    public static SimulationSpeed TenTimes { get; } = new(40, "10×");

    public static IReadOnlyList<SimulationSpeed> Supported { get; } = Array.AsReadOnly(
        new[] { Quarter, Half, Normal, Double, FiveTimes, TenTimes });

    public int QuarterUnits { get; }

    public string Label { get; }

    public decimal Multiplier => (decimal)QuarterUnits / ScaleDenominator;

    internal static int ScalingDenominator => ScaleDenominator;

    public override string ToString() => Label;
}
