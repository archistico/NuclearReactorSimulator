namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Fraction of a control volume occupied by vapor, from zero (all liquid) to one (all vapor).
/// This is a volumetric quantity and is intentionally distinct from saturated-mixture vapor quality.
/// </summary>
public readonly record struct VoidFraction : IComparable<VoidFraction>
{
    private VoidFraction(double fraction)
    {
        if (!double.IsFinite(fraction))
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Void fraction must be finite.");
        }

        if (fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Void fraction must be between zero and one inclusive.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static VoidFraction NoVoid { get; } = FromFraction(0d);

    public static VoidFraction AllVapor { get; } = FromFraction(1d);

    public static VoidFraction FromFraction(double value) => new(value);

    public static VoidFraction FromPercent(double value) => new(value / 100d);

    public int CompareTo(VoidFraction other) => Fraction.CompareTo(other.Fraction);

    public static VoidFractionDifference operator -(VoidFraction left, VoidFraction right)
        => VoidFractionDifference.FromFraction(left.Fraction - right.Fraction);

    public static bool operator <(VoidFraction left, VoidFraction right) => left.Fraction < right.Fraction;

    public static bool operator >(VoidFraction left, VoidFraction right) => left.Fraction > right.Fraction;

    public static bool operator <=(VoidFraction left, VoidFraction right) => left.Fraction <= right.Fraction;

    public static bool operator >=(VoidFraction left, VoidFraction right) => left.Fraction >= right.Fraction;
}
