using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Simplified constant shaft-to-hydraulic efficiency in the open-closed interval (0, 1].
/// </summary>
public readonly record struct PumpEfficiency : IComparable<PumpEfficiency>
{
    private PumpEfficiency(double fraction)
    {
        fraction = QuantityGuard.Finite(fraction, nameof(fraction));

        if (fraction <= 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Pump efficiency must be greater than zero and no greater than one.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static PumpEfficiency Ideal => new(1d);

    public static PumpEfficiency FromFraction(double fraction) => new(fraction);

    public static PumpEfficiency FromPercent(double percent) => new(percent / 100d);

    public int CompareTo(PumpEfficiency other) => Fraction.CompareTo(other.Fraction);

    public static bool operator <(PumpEfficiency left, PumpEfficiency right) => left.Fraction < right.Fraction;

    public static bool operator >(PumpEfficiency left, PumpEfficiency right) => left.Fraction > right.Fraction;

    public static bool operator <=(PumpEfficiency left, PumpEfficiency right) => left.Fraction <= right.Fraction;

    public static bool operator >=(PumpEfficiency left, PumpEfficiency right) => left.Fraction >= right.Fraction;
}
