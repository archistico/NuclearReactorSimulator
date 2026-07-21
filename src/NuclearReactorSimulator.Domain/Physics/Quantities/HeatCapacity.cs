namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Lumped heat capacity stored canonically in joules per kelvin.
/// </summary>
public readonly record struct HeatCapacity : IComparable<HeatCapacity>
{
    private const double JoulesPerKilojoule = 1_000d;
    private const double JoulesPerMegajoule = 1_000_000d;

    private HeatCapacity(double joulesPerKelvin)
    {
        JoulesPerKelvin = QuantityGuard.NonNegativeFinite(joulesPerKelvin, nameof(joulesPerKelvin));
    }

    public double JoulesPerKelvin { get; }

    public double KilojoulesPerKelvin => JoulesPerKelvin / JoulesPerKilojoule;

    public double MegajoulesPerKelvin => JoulesPerKelvin / JoulesPerMegajoule;

    public static HeatCapacity Zero { get; } = FromJoulesPerKelvin(0d);

    public static HeatCapacity FromJoulesPerKelvin(double value) => new(value);

    public static HeatCapacity FromKilojoulesPerKelvin(double value) => new(value * JoulesPerKilojoule);

    public static HeatCapacity FromMegajoulesPerKelvin(double value) => new(value * JoulesPerMegajoule);

    public int CompareTo(HeatCapacity other) => JoulesPerKelvin.CompareTo(other.JoulesPerKelvin);

    public static Energy operator *(HeatCapacity capacity, TemperatureDifference difference)
    {
        return Energy.FromJoules(capacity.JoulesPerKelvin * difference.Kelvins);
    }

    public static Energy operator *(TemperatureDifference difference, HeatCapacity capacity) => capacity * difference;

    public static bool operator <(HeatCapacity left, HeatCapacity right) => left.JoulesPerKelvin < right.JoulesPerKelvin;

    public static bool operator >(HeatCapacity left, HeatCapacity right) => left.JoulesPerKelvin > right.JoulesPerKelvin;

    public static bool operator <=(HeatCapacity left, HeatCapacity right) => left.JoulesPerKelvin <= right.JoulesPerKelvin;

    public static bool operator >=(HeatCapacity left, HeatCapacity right) => left.JoulesPerKelvin >= right.JoulesPerKelvin;
}
