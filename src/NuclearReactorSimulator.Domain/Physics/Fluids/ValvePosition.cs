using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Normalized mechanical valve position in the closed [0] to fully-open [1] interval.
/// </summary>
public readonly record struct ValvePosition : IComparable<ValvePosition>
{
    private ValvePosition(double fraction)
    {
        fraction = QuantityGuard.Finite(fraction, nameof(fraction));

        if (fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Valve position must be between zero and one inclusive.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public bool IsClosed => Fraction == 0d;

    public bool IsFullyOpen => Fraction == 1d;

    public static ValvePosition Closed => new(0d);

    public static ValvePosition FullyOpen => new(1d);

    public static ValvePosition FromFraction(double fraction) => new(fraction);

    public static ValvePosition FromPercent(double percent) => new(percent / 100d);

    public int CompareTo(ValvePosition other) => Fraction.CompareTo(other.Fraction);

    public static bool operator <(ValvePosition left, ValvePosition right) => left.Fraction < right.Fraction;

    public static bool operator >(ValvePosition left, ValvePosition right) => left.Fraction > right.Fraction;

    public static bool operator <=(ValvePosition left, ValvePosition right) => left.Fraction <= right.Fraction;

    public static bool operator >=(ValvePosition left, ValvePosition right) => left.Fraction >= right.Fraction;
}
