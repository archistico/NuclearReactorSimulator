namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct SpecificEnergy : IComparable<SpecificEnergy>
{
    private const double JoulesPerKilojoule = 1_000d;

    private SpecificEnergy(double joulesPerKilogram)
    {
        JoulesPerKilogram = QuantityGuard.Finite(joulesPerKilogram, nameof(joulesPerKilogram));
    }

    public double JoulesPerKilogram { get; }

    public double KilojoulesPerKilogram => JoulesPerKilogram / JoulesPerKilojoule;

    public static SpecificEnergy Zero { get; } = FromJoulesPerKilogram(0d);

    public static SpecificEnergy FromJoulesPerKilogram(double value) => new(value);

    public static SpecificEnergy FromKilojoulesPerKilogram(double value) => new(value * JoulesPerKilojoule);

    public int CompareTo(SpecificEnergy other) => JoulesPerKilogram.CompareTo(other.JoulesPerKilogram);

    public static Energy operator *(SpecificEnergy specificEnergy, Mass mass) => Energy.FromJoules(specificEnergy.JoulesPerKilogram * mass.Kilograms);

    public static Energy operator *(Mass mass, SpecificEnergy specificEnergy) => specificEnergy * mass;

    public static Power operator *(SpecificEnergy specificEnergy, MassFlowRate massFlowRate) => Power.FromWatts(specificEnergy.JoulesPerKilogram * massFlowRate.KilogramsPerSecond);

    public static Power operator *(MassFlowRate massFlowRate, SpecificEnergy specificEnergy) => specificEnergy * massFlowRate;

    public static bool operator <(SpecificEnergy left, SpecificEnergy right) => left.JoulesPerKilogram < right.JoulesPerKilogram;

    public static bool operator >(SpecificEnergy left, SpecificEnergy right) => left.JoulesPerKilogram > right.JoulesPerKilogram;

    public static bool operator <=(SpecificEnergy left, SpecificEnergy right) => left.JoulesPerKilogram <= right.JoulesPerKilogram;

    public static bool operator >=(SpecificEnergy left, SpecificEnergy right) => left.JoulesPerKilogram >= right.JoulesPerKilogram;
}
