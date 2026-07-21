namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Temperature : IComparable<Temperature>
{
    private const double CelsiusOffset = 273.15d;

    private Temperature(double kelvins)
    {
        Kelvins = QuantityGuard.NonNegativeFinite(kelvins, nameof(kelvins));
    }

    public double Kelvins { get; }

    public double DegreesCelsius => Kelvins - CelsiusOffset;

    public static Temperature AbsoluteZero { get; } = FromKelvins(0d);

    public static Temperature FromKelvins(double value) => new(value);

    public static Temperature FromDegreesCelsius(double value) => new(QuantityGuard.Finite(value, nameof(value)) + CelsiusOffset);

    public int CompareTo(Temperature other) => Kelvins.CompareTo(other.Kelvins);

    public static Temperature operator +(Temperature temperature, TemperatureDifference difference) => FromKelvins(temperature.Kelvins + difference.Kelvins);

    public static Temperature operator +(TemperatureDifference difference, Temperature temperature) => temperature + difference;

    public static Temperature operator -(Temperature temperature, TemperatureDifference difference) => FromKelvins(temperature.Kelvins - difference.Kelvins);

    public static TemperatureDifference operator -(Temperature left, Temperature right) => TemperatureDifference.FromKelvins(left.Kelvins - right.Kelvins);

    public static bool operator <(Temperature left, Temperature right) => left.Kelvins < right.Kelvins;

    public static bool operator >(Temperature left, Temperature right) => left.Kelvins > right.Kelvins;

    public static bool operator <=(Temperature left, Temperature right) => left.Kelvins <= right.Kelvins;

    public static bool operator >=(Temperature left, Temperature right) => left.Kelvins >= right.Kelvins;
}
