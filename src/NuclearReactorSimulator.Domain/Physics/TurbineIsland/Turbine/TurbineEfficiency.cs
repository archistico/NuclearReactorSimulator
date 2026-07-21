namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

/// <summary>
/// Simplified constant steam-to-shaft efficiency in the open-closed interval (0, 1].
/// </summary>
public readonly record struct TurbineEfficiency : IComparable<TurbineEfficiency>
{
    private TurbineEfficiency(double fraction)
    {
        fraction = double.IsFinite(fraction)
            ? fraction
            : throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Turbine efficiency must be finite.");

        if (fraction <= 0d || fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Turbine efficiency must be greater than zero and no greater than one.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static TurbineEfficiency Ideal => new(1d);

    public static TurbineEfficiency FromFraction(double fraction) => new(fraction);

    public static TurbineEfficiency FromPercent(double percent) => new(percent / 100d);

    public int CompareTo(TurbineEfficiency other) => Fraction.CompareTo(other.Fraction);
}
