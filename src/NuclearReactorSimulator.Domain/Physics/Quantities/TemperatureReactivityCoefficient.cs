namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Signed linear temperature coefficient of reactivity, stored canonically as delta-k/k per kelvin.
/// </summary>
public readonly record struct TemperatureReactivityCoefficient : IComparable<TemperatureReactivityCoefficient>
{
    private const double DeltaKOverKPerPcm = 0.00001d;

    private TemperatureReactivityCoefficient(double deltaKOverKPerKelvin)
    {
        DeltaKOverKPerKelvin = QuantityGuard.Finite(deltaKOverKPerKelvin, nameof(deltaKOverKPerKelvin));
    }

    public double DeltaKOverKPerKelvin { get; }

    public double PcmPerKelvin => DeltaKOverKPerKelvin / DeltaKOverKPerPcm;

    public static TemperatureReactivityCoefficient Zero { get; } = FromDeltaKOverKPerKelvin(0d);

    public static TemperatureReactivityCoefficient FromDeltaKOverKPerKelvin(double value) => new(value);

    public static TemperatureReactivityCoefficient FromPcmPerKelvin(double value) => new(value * DeltaKOverKPerPcm);

    public int CompareTo(TemperatureReactivityCoefficient other)
        => DeltaKOverKPerKelvin.CompareTo(other.DeltaKOverKPerKelvin);

    public static Reactivity operator *(
        TemperatureReactivityCoefficient coefficient,
        TemperatureDifference temperatureDifference)
        => Reactivity.FromDeltaKOverK(
            coefficient.DeltaKOverKPerKelvin * temperatureDifference.Kelvins);

    public static Reactivity operator *(
        TemperatureDifference temperatureDifference,
        TemperatureReactivityCoefficient coefficient)
        => coefficient * temperatureDifference;

    public static bool operator <(
        TemperatureReactivityCoefficient left,
        TemperatureReactivityCoefficient right)
        => left.DeltaKOverKPerKelvin < right.DeltaKOverKPerKelvin;

    public static bool operator >(
        TemperatureReactivityCoefficient left,
        TemperatureReactivityCoefficient right)
        => left.DeltaKOverKPerKelvin > right.DeltaKOverKPerKelvin;

    public static bool operator <=(
        TemperatureReactivityCoefficient left,
        TemperatureReactivityCoefficient right)
        => left.DeltaKOverKPerKelvin <= right.DeltaKOverKPerKelvin;

    public static bool operator >=(
        TemperatureReactivityCoefficient left,
        TemperatureReactivityCoefficient right)
        => left.DeltaKOverKPerKelvin >= right.DeltaKOverKPerKelvin;
}
