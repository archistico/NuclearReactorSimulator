namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Power : IComparable<Power>
{
    private const double WattsPerKilowatt = 1_000d;
    private const double WattsPerMegawatt = 1_000_000d;
    private const double WattsPerGigawatt = 1_000_000_000d;

    private Power(double watts)
    {
        Watts = QuantityGuard.Finite(watts, nameof(watts));
    }

    public double Watts { get; }

    public double Kilowatts => Watts / WattsPerKilowatt;

    public double Megawatts => Watts / WattsPerMegawatt;

    public double Gigawatts => Watts / WattsPerGigawatt;

    public static Power Zero { get; } = FromWatts(0d);

    public static Power FromWatts(double value) => new(value);

    public static Power FromKilowatts(double value) => new(value * WattsPerKilowatt);

    public static Power FromMegawatts(double value) => new(value * WattsPerMegawatt);

    public static Power FromGigawatts(double value) => new(value * WattsPerGigawatt);

    public Energy Over(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration cannot be negative.");
        }

        return Energy.FromJoules(Watts * duration.TotalSeconds);
    }

    public int CompareTo(Power other) => Watts.CompareTo(other.Watts);

    public static Power operator +(Power left, Power right) => FromWatts(left.Watts + right.Watts);

    public static Power operator -(Power left, Power right) => FromWatts(left.Watts - right.Watts);

    public static Power operator -(Power value) => FromWatts(-value.Watts);

    public static Power operator *(Power value, double scalar) => FromWatts(value.Watts * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static Power operator *(double scalar, Power value) => value * scalar;

    public static Power operator /(Power value, double divisor) => FromWatts(value.Watts / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static bool operator <(Power left, Power right) => left.Watts < right.Watts;

    public static bool operator >(Power left, Power right) => left.Watts > right.Watts;

    public static bool operator <=(Power left, Power right) => left.Watts <= right.Watts;

    public static bool operator >=(Power left, Power right) => left.Watts >= right.Watts;
}
