namespace NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

/// <summary>
/// Dimensionless fraction of current fission thermal power that feeds one equivalent decay-heat inventory group.
/// At long steady operation, the group's equilibrium decay-heat power equals this fraction of fission power.
/// </summary>
public readonly record struct DecayHeatGenerationFraction : IComparable<DecayHeatGenerationFraction>
{
    private DecayHeatGenerationFraction(double fraction)
    {
        if (!double.IsFinite(fraction) || fraction < 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fraction),
                fraction,
                "Decay-heat generation fraction must be finite and within [0, 1].");
        }

        Fraction = fraction == 0d ? 0d : fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static DecayHeatGenerationFraction Zero { get; } = FromFraction(0d);

    public static DecayHeatGenerationFraction Full { get; } = FromFraction(1d);

    public static DecayHeatGenerationFraction FromFraction(double value) => new(value);

    public static DecayHeatGenerationFraction FromPercent(double value) => new(value / 100d);

    public int CompareTo(DecayHeatGenerationFraction other) => Fraction.CompareTo(other.Fraction);

    public static bool operator <(DecayHeatGenerationFraction left, DecayHeatGenerationFraction right) => left.Fraction < right.Fraction;

    public static bool operator >(DecayHeatGenerationFraction left, DecayHeatGenerationFraction right) => left.Fraction > right.Fraction;

    public static bool operator <=(DecayHeatGenerationFraction left, DecayHeatGenerationFraction right) => left.Fraction <= right.Fraction;

    public static bool operator >=(DecayHeatGenerationFraction left, DecayHeatGenerationFraction right) => left.Fraction >= right.Fraction;
}
