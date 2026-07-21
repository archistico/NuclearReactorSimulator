namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Vapor mass fraction for a saturated liquid-vapor mixture.
/// </summary>
public readonly record struct VaporQuality : IComparable<VaporQuality>
{
    private VaporQuality(double fraction)
    {
        if (!double.IsFinite(fraction))
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Vapor quality must be finite.");
        }

        if (fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Vapor quality must be between zero and one inclusive.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static VaporQuality SaturatedLiquid { get; } = FromFraction(0d);

    public static VaporQuality DrySaturatedVapor { get; } = FromFraction(1d);

    public static VaporQuality FromFraction(double value) => new(value);

    public static VaporQuality FromPercent(double value) => new(value / 100d);

    public int CompareTo(VaporQuality other) => Fraction.CompareTo(other.Fraction);

    public static bool operator <(VaporQuality left, VaporQuality right) => left.Fraction < right.Fraction;

    public static bool operator >(VaporQuality left, VaporQuality right) => left.Fraction > right.Fraction;

    public static bool operator <=(VaporQuality left, VaporQuality right) => left.Fraction <= right.Fraction;

    public static bool operator >=(VaporQuality left, VaporQuality right) => left.Fraction >= right.Fraction;
}
