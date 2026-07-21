namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Energy : IComparable<Energy>
{
    private const double JoulesPerKilojoule = 1_000d;
    private const double JoulesPerMegajoule = 1_000_000d;
    private const double JoulesPerKilowattHour = 3_600_000d;
    private const double JoulesPerMegawattHour = 3_600_000_000d;

    private Energy(double joules)
    {
        Joules = QuantityGuard.Finite(joules, nameof(joules));
    }

    public double Joules { get; }

    public double Kilojoules => Joules / JoulesPerKilojoule;

    public double Megajoules => Joules / JoulesPerMegajoule;

    public double KilowattHours => Joules / JoulesPerKilowattHour;

    public double MegawattHours => Joules / JoulesPerMegawattHour;

    public static Energy Zero { get; } = FromJoules(0d);

    public static Energy FromJoules(double value) => new(value);

    public static Energy FromKilojoules(double value) => new(value * JoulesPerKilojoule);

    public static Energy FromMegajoules(double value) => new(value * JoulesPerMegajoule);

    public static Energy FromKilowattHours(double value) => new(value * JoulesPerKilowattHour);

    public static Energy FromMegawattHours(double value) => new(value * JoulesPerMegawattHour);

    public Power Per(TimeSpan duration) => Power.FromWatts(Joules / QuantityGuard.PositiveSeconds(duration, nameof(duration)));

    public int CompareTo(Energy other) => Joules.CompareTo(other.Joules);

    public static Energy operator +(Energy left, Energy right) => FromJoules(left.Joules + right.Joules);

    public static Energy operator -(Energy left, Energy right) => FromJoules(left.Joules - right.Joules);

    public static Energy operator -(Energy value) => FromJoules(-value.Joules);

    public static Energy operator *(Energy value, double scalar) => FromJoules(value.Joules * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static Energy operator *(double scalar, Energy value) => value * scalar;

    public static Energy operator /(Energy value, double divisor) => FromJoules(value.Joules / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static SpecificEnergy operator /(Energy energy, Mass mass)
    {
        if (mass == Mass.Zero)
        {
            throw new DivideByZeroException("Cannot derive specific energy from zero mass.");
        }

        return SpecificEnergy.FromJoulesPerKilogram(energy.Joules / mass.Kilograms);
    }

    public static bool operator <(Energy left, Energy right) => left.Joules < right.Joules;

    public static bool operator >(Energy left, Energy right) => left.Joules > right.Joules;

    public static bool operator <=(Energy left, Energy right) => left.Joules <= right.Joules;

    public static bool operator >=(Energy left, Energy right) => left.Joules >= right.Joules;
}
