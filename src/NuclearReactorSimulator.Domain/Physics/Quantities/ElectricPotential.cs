namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Non-negative RMS electric potential magnitude in volts.
/// </summary>
public readonly record struct ElectricPotential : IComparable<ElectricPotential>
{
    private const double VoltsPerKilovolt = 1_000d;

    private ElectricPotential(double volts)
    {
        Volts = QuantityGuard.NonNegativeFinite(volts, nameof(volts));
    }

    public double Volts { get; }

    public double Kilovolts => Volts / VoltsPerKilovolt;

    public static ElectricPotential Zero { get; } = FromVolts(0d);

    public static ElectricPotential FromVolts(double value) => new(value);

    public static ElectricPotential FromKilovolts(double value) => new(value * VoltsPerKilovolt);

    public int CompareTo(ElectricPotential other) => Volts.CompareTo(other.Volts);

    public static bool operator <(ElectricPotential left, ElectricPotential right) => left.Volts < right.Volts;

    public static bool operator >(ElectricPotential left, ElectricPotential right) => left.Volts > right.Volts;

    public static bool operator <=(ElectricPotential left, ElectricPotential right) => left.Volts <= right.Volts;

    public static bool operator >=(ElectricPotential left, ElectricPotential right) => left.Volts >= right.Volts;
}
