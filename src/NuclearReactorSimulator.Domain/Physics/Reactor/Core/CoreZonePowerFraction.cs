namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core;

/// <summary>
/// Dimensionless fraction of global fission thermal power assigned to one aggregated core zone.
/// </summary>
public readonly record struct CoreZonePowerFraction : IComparable<CoreZonePowerFraction>
{
    private CoreZonePowerFraction(double fraction)
    {
        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Core-zone power fraction must be finite and within [0, 1].");
        }

        Fraction = fraction == 0d ? 0d : fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static CoreZonePowerFraction Zero { get; } = FromFraction(0d);

    public static CoreZonePowerFraction Full { get; } = FromFraction(1d);

    public static CoreZonePowerFraction FromFraction(double value) => new(value);

    public static CoreZonePowerFraction FromPercent(double value) => new(value / 100d);

    public int CompareTo(CoreZonePowerFraction other) => Fraction.CompareTo(other.Fraction);
}
