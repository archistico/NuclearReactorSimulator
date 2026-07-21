namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Lumped thermal conductance stored canonically in watts per kelvin.
/// </summary>
public readonly record struct ThermalConductance : IComparable<ThermalConductance>
{
    private const double WattsPerKilowatt = 1_000d;
    private const double WattsPerMegawatt = 1_000_000d;

    private ThermalConductance(double wattsPerKelvin)
    {
        WattsPerKelvin = QuantityGuard.NonNegativeFinite(wattsPerKelvin, nameof(wattsPerKelvin));
    }

    public double WattsPerKelvin { get; }

    public double KilowattsPerKelvin => WattsPerKelvin / WattsPerKilowatt;

    public double MegawattsPerKelvin => WattsPerKelvin / WattsPerMegawatt;

    public static ThermalConductance Zero { get; } = FromWattsPerKelvin(0d);

    public static ThermalConductance FromWattsPerKelvin(double value) => new(value);

    public static ThermalConductance FromKilowattsPerKelvin(double value) => new(value * WattsPerKilowatt);

    public static ThermalConductance FromMegawattsPerKelvin(double value) => new(value * WattsPerMegawatt);

    public int CompareTo(ThermalConductance other) => WattsPerKelvin.CompareTo(other.WattsPerKelvin);

    public static Power operator *(ThermalConductance conductance, TemperatureDifference difference)
    {
        return Power.FromWatts(conductance.WattsPerKelvin * difference.Kelvins);
    }

    public static Power operator *(TemperatureDifference difference, ThermalConductance conductance) => conductance * difference;

    public static bool operator <(ThermalConductance left, ThermalConductance right) => left.WattsPerKelvin < right.WattsPerKelvin;

    public static bool operator >(ThermalConductance left, ThermalConductance right) => left.WattsPerKelvin > right.WattsPerKelvin;

    public static bool operator <=(ThermalConductance left, ThermalConductance right) => left.WattsPerKelvin <= right.WattsPerKelvin;

    public static bool operator >=(ThermalConductance left, ThermalConductance right) => left.WattsPerKelvin >= right.WattsPerKelvin;
}
