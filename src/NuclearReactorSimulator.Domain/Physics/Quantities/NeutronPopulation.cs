namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Non-negative normalized neutron population. A value of 1 represents the caller-selected reference level.
/// </summary>
public readonly record struct NeutronPopulation : IComparable<NeutronPopulation>
{
    private NeutronPopulation(double relative)
    {
        Relative = QuantityGuard.NonNegativeFinite(relative, nameof(relative));
    }

    public double Relative { get; }

    public static NeutronPopulation Zero { get; } = FromRelative(0d);

    public static NeutronPopulation Reference { get; } = FromRelative(1d);

    public static NeutronPopulation FromRelative(double value) => new(value);

    public int CompareTo(NeutronPopulation other) => Relative.CompareTo(other.Relative);

    public static bool operator <(NeutronPopulation left, NeutronPopulation right) => left.Relative < right.Relative;

    public static bool operator >(NeutronPopulation left, NeutronPopulation right) => left.Relative > right.Relative;

    public static bool operator <=(NeutronPopulation left, NeutronPopulation right) => left.Relative <= right.Relative;

    public static bool operator >=(NeutronPopulation left, NeutronPopulation right) => left.Relative >= right.Relative;
}
