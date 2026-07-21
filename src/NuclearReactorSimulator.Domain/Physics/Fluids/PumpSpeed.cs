using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Normalized pump rotational speed in the stopped [0] to rated-speed [1] interval.
/// </summary>
public readonly record struct PumpSpeed : IComparable<PumpSpeed>
{
    private PumpSpeed(double fraction)
    {
        fraction = QuantityGuard.Finite(fraction, nameof(fraction));

        if (fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Pump speed must be between zero and one inclusive.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public bool IsStopped => Fraction == 0d;

    public bool IsRatedSpeed => Fraction == 1d;

    public static PumpSpeed Stopped => new(0d);

    public static PumpSpeed Rated => new(1d);

    public static PumpSpeed FromFraction(double fraction) => new(fraction);

    public static PumpSpeed FromPercent(double percent) => new(percent / 100d);

    public int CompareTo(PumpSpeed other) => Fraction.CompareTo(other.Fraction);

    public static bool operator <(PumpSpeed left, PumpSpeed right) => left.Fraction < right.Fraction;

    public static bool operator >(PumpSpeed left, PumpSpeed right) => left.Fraction > right.Fraction;

    public static bool operator <=(PumpSpeed left, PumpSpeed right) => left.Fraction <= right.Fraction;

    public static bool operator >=(PumpSpeed left, PumpSpeed right) => left.Fraction >= right.Fraction;
}
