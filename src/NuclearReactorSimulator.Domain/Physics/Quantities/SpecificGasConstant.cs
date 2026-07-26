namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Specific gas constant stored canonically in joules per kilogram-kelvin.
/// This is dimensionally similar to a specific heat capacity but remains a distinct semantic quantity.
/// </summary>
public readonly record struct SpecificGasConstant : IComparable<SpecificGasConstant>
{
    private const double JoulesPerKilojoule = 1_000d;

    private SpecificGasConstant(double joulesPerKilogramKelvin)
    {
        JoulesPerKilogramKelvin = QuantityGuard.NonNegativeFinite(
            joulesPerKilogramKelvin,
            nameof(joulesPerKilogramKelvin));
    }

    public double JoulesPerKilogramKelvin { get; }

    public double KilojoulesPerKilogramKelvin => JoulesPerKilogramKelvin / JoulesPerKilojoule;

    public static SpecificGasConstant Zero { get; } = FromJoulesPerKilogramKelvin(0d);

    public static SpecificGasConstant FromJoulesPerKilogramKelvin(double value) => new(value);

    public static SpecificGasConstant FromKilojoulesPerKilogramKelvin(double value) => new(value * JoulesPerKilojoule);

    public int CompareTo(SpecificGasConstant other)
        => JoulesPerKilogramKelvin.CompareTo(other.JoulesPerKilogramKelvin);

    public static bool operator <(SpecificGasConstant left, SpecificGasConstant right)
        => left.JoulesPerKilogramKelvin < right.JoulesPerKilogramKelvin;

    public static bool operator >(SpecificGasConstant left, SpecificGasConstant right)
        => left.JoulesPerKilogramKelvin > right.JoulesPerKilogramKelvin;

    public static bool operator <=(SpecificGasConstant left, SpecificGasConstant right)
        => left.JoulesPerKilogramKelvin <= right.JoulesPerKilogramKelvin;

    public static bool operator >=(SpecificGasConstant left, SpecificGasConstant right)
        => left.JoulesPerKilogramKelvin >= right.JoulesPerKilogramKelvin;
}
