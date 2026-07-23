namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Specific heat capacity stored canonically in joules per kilogram-kelvin.
/// </summary>
public readonly record struct SpecificHeatCapacity : IComparable<SpecificHeatCapacity>
{
    private const double JoulesPerKilojoule = 1_000d;

    private SpecificHeatCapacity(double joulesPerKilogramKelvin)
    {
        JoulesPerKilogramKelvin = QuantityGuard.NonNegativeFinite(
            joulesPerKilogramKelvin,
            nameof(joulesPerKilogramKelvin));
    }

    public double JoulesPerKilogramKelvin { get; }

    public double KilojoulesPerKilogramKelvin => JoulesPerKilogramKelvin / JoulesPerKilojoule;

    public static SpecificHeatCapacity Zero { get; } = FromJoulesPerKilogramKelvin(0d);

    public static SpecificHeatCapacity FromJoulesPerKilogramKelvin(double value) => new(value);

    public static SpecificHeatCapacity FromKilojoulesPerKilogramKelvin(double value) => new(value * JoulesPerKilojoule);

    public int CompareTo(SpecificHeatCapacity other)
        => JoulesPerKilogramKelvin.CompareTo(other.JoulesPerKilogramKelvin);

    public static SpecificEnergy operator *(SpecificHeatCapacity capacity, Temperature temperature)
        => SpecificEnergy.FromJoulesPerKilogram(
            capacity.JoulesPerKilogramKelvin * temperature.Kelvins);

    public static SpecificEnergy operator *(Temperature temperature, SpecificHeatCapacity capacity)
        => capacity * temperature;

    public static bool operator <(SpecificHeatCapacity left, SpecificHeatCapacity right)
        => left.JoulesPerKilogramKelvin < right.JoulesPerKilogramKelvin;

    public static bool operator >(SpecificHeatCapacity left, SpecificHeatCapacity right)
        => left.JoulesPerKilogramKelvin > right.JoulesPerKilogramKelvin;

    public static bool operator <=(SpecificHeatCapacity left, SpecificHeatCapacity right)
        => left.JoulesPerKilogramKelvin <= right.JoulesPerKilogramKelvin;

    public static bool operator >=(SpecificHeatCapacity left, SpecificHeatCapacity right)
        => left.JoulesPerKilogramKelvin >= right.JoulesPerKilogramKelvin;
}
