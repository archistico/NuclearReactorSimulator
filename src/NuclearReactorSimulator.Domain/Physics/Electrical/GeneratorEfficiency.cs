namespace NuclearReactorSimulator.Domain.Physics.Electrical;

/// <summary>
/// Simplified shaft-to-electrical conversion efficiency in the open-closed interval (0, 1].
/// </summary>
public readonly record struct GeneratorEfficiency : IComparable<GeneratorEfficiency>
{
    private GeneratorEfficiency(double fraction)
    {
        fraction = double.IsFinite(fraction)
            ? fraction
            : throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Generator efficiency must be finite.");

        if (fraction <= 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Generator efficiency must be greater than zero and no greater than one.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static GeneratorEfficiency Ideal => new(1d);

    public static GeneratorEfficiency FromFraction(double fraction) => new(fraction);

    public static GeneratorEfficiency FromPercent(double percent) => new(percent / 100d);

    public int CompareTo(GeneratorEfficiency other) => Fraction.CompareTo(other.Fraction);
}
