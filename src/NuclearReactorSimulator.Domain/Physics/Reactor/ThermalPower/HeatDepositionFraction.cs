namespace NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;

/// <summary>
/// Dimensionless fraction of instantaneous fission thermal power assigned to one heat-deposition destination.
/// </summary>
public readonly record struct HeatDepositionFraction : IComparable<HeatDepositionFraction>
{
    private HeatDepositionFraction(double fraction)
    {
        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Heat-deposition fraction must be finite and within [0, 1].");
        }

        Fraction = fraction == 0d ? 0d : fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static HeatDepositionFraction Zero { get; } = FromFraction(0d);

    public static HeatDepositionFraction Full { get; } = FromFraction(1d);

    public static HeatDepositionFraction FromFraction(double value) => new(value);

    public static HeatDepositionFraction FromPercent(double value) => new(value / 100d);

    public int CompareTo(HeatDepositionFraction other) => Fraction.CompareTo(other.Fraction);

    public static bool operator <(HeatDepositionFraction left, HeatDepositionFraction right) => left.Fraction < right.Fraction;

    public static bool operator >(HeatDepositionFraction left, HeatDepositionFraction right) => left.Fraction > right.Fraction;

    public static bool operator <=(HeatDepositionFraction left, HeatDepositionFraction right) => left.Fraction <= right.Fraction;

    public static bool operator >=(HeatDepositionFraction left, HeatDepositionFraction right) => left.Fraction >= right.Fraction;
}
