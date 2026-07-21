namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct TemperatureDifference : IComparable<TemperatureDifference>
{
    private TemperatureDifference(double kelvins)
    {
        Kelvins = QuantityGuard.Finite(kelvins, nameof(kelvins));
    }

    public double Kelvins { get; }

    public double DegreesCelsius => Kelvins;

    public static TemperatureDifference Zero { get; } = FromKelvins(0d);

    public static TemperatureDifference FromKelvins(double value) => new(value);

    public static TemperatureDifference FromDegreesCelsius(double value) => new(value);

    public int CompareTo(TemperatureDifference other) => Kelvins.CompareTo(other.Kelvins);

    public static TemperatureDifference operator +(TemperatureDifference left, TemperatureDifference right) => FromKelvins(left.Kelvins + right.Kelvins);

    public static TemperatureDifference operator -(TemperatureDifference left, TemperatureDifference right) => FromKelvins(left.Kelvins - right.Kelvins);

    public static TemperatureDifference operator -(TemperatureDifference value) => FromKelvins(-value.Kelvins);

    public static TemperatureDifference operator *(TemperatureDifference value, double scalar) => FromKelvins(value.Kelvins * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static TemperatureDifference operator *(double scalar, TemperatureDifference value) => value * scalar;

    public static TemperatureDifference operator /(TemperatureDifference value, double divisor) => FromKelvins(value.Kelvins / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static bool operator <(TemperatureDifference left, TemperatureDifference right) => left.Kelvins < right.Kelvins;

    public static bool operator >(TemperatureDifference left, TemperatureDifference right) => left.Kelvins > right.Kelvins;

    public static bool operator <=(TemperatureDifference left, TemperatureDifference right) => left.Kelvins <= right.Kelvins;

    public static bool operator >=(TemperatureDifference left, TemperatureDifference right) => left.Kelvins >= right.Kelvins;
}
