namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Signed difference between two void fractions.
/// </summary>
public readonly record struct VoidFractionDifference : IComparable<VoidFractionDifference>
{
    private VoidFractionDifference(double fraction)
    {
        if (!double.IsFinite(fraction))
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Void-fraction difference must be finite.");
        }

        if (fraction < -1d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Void-fraction difference must be between minus one and one inclusive.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double PercentagePoints => Fraction * 100d;

    public static VoidFractionDifference Zero { get; } = FromFraction(0d);

    public static VoidFractionDifference FromFraction(double value) => new(value);

    public static VoidFractionDifference FromPercentagePoints(double value) => new(value / 100d);

    public int CompareTo(VoidFractionDifference other) => Fraction.CompareTo(other.Fraction);
}
