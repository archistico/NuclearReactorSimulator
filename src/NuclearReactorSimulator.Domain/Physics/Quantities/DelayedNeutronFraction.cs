namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Dimensionless delayed-neutron fraction, constrained to the inclusive range [0, 1].
/// </summary>
public readonly record struct DelayedNeutronFraction : IComparable<DelayedNeutronFraction>
{
    private DelayedNeutronFraction(double fraction)
    {
        fraction = QuantityGuard.NonNegativeFinite(fraction, nameof(fraction));
        if (fraction > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Delayed-neutron fraction cannot exceed one.");
        }

        Fraction = fraction;
    }

    public double Fraction { get; }

    public double Percent => Fraction * 100d;

    public static DelayedNeutronFraction Zero { get; } = FromFraction(0d);

    public static DelayedNeutronFraction FromFraction(double value) => new(value);

    public static DelayedNeutronFraction FromPercent(double value) => new(value / 100d);

    public int CompareTo(DelayedNeutronFraction other) => Fraction.CompareTo(other.Fraction);
}
