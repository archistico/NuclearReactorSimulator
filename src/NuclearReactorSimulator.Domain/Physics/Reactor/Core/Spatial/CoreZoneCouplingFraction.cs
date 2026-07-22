namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core.Spatial;

/// <summary>
/// Dimensionless fraction of one zone's local quasi-spatial reactivity signal replaced by a coupled neighbour signal.
/// </summary>
public readonly record struct CoreZoneCouplingFraction : IComparable<CoreZoneCouplingFraction>
{
    private CoreZoneCouplingFraction(double fraction)
    {
        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Core-zone coupling fraction must be finite and within [0, 1].");
        }

        Fraction = fraction == 0d ? 0d : fraction;
    }

    public double Fraction { get; }

    public static CoreZoneCouplingFraction Zero { get; } = FromFraction(0d);

    public static CoreZoneCouplingFraction FromFraction(double value) => new(value);

    public static CoreZoneCouplingFraction FromPercent(double value) => new(value / 100d);

    public int CompareTo(CoreZoneCouplingFraction other) => Fraction.CompareTo(other.Fraction);
}
