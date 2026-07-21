namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Non-negative normalized delayed-neutron precursor population used by point kinetics.
/// Its scale is consistent with the standard point-kinetics equations for the selected parameter set.
/// </summary>
public readonly record struct DelayedNeutronPrecursorPopulation : IComparable<DelayedNeutronPrecursorPopulation>
{
    private DelayedNeutronPrecursorPopulation(double relative)
    {
        Relative = QuantityGuard.NonNegativeFinite(relative, nameof(relative));
    }

    public double Relative { get; }

    public static DelayedNeutronPrecursorPopulation Zero { get; } = FromRelative(0d);

    public static DelayedNeutronPrecursorPopulation FromRelative(double value) => new(value);

    public int CompareTo(DelayedNeutronPrecursorPopulation other) => Relative.CompareTo(other.Relative);
}
