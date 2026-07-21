namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct Mass : IComparable<Mass>
{
    private const double GramsPerKilogram = 1_000d;
    private const double KilogramsPerTonne = 1_000d;

    private Mass(double kilograms)
    {
        Kilograms = QuantityGuard.NonNegativeFinite(kilograms, nameof(kilograms));
    }

    public double Kilograms { get; }

    public double Grams => Kilograms * GramsPerKilogram;

    public double Tonnes => Kilograms / KilogramsPerTonne;

    public static Mass Zero { get; } = FromKilograms(0d);

    public static Mass FromKilograms(double value) => new(value);

    public static Mass FromGrams(double value) => new(value / GramsPerKilogram);

    public static Mass FromTonnes(double value) => new(value * KilogramsPerTonne);

    public MassFlowRate Per(TimeSpan duration) => MassFlowRate.FromKilogramsPerSecond(
        Kilograms / QuantityGuard.PositiveSeconds(duration, nameof(duration)));

    public int CompareTo(Mass other) => Kilograms.CompareTo(other.Kilograms);

    public static Mass operator +(Mass left, Mass right) => FromKilograms(left.Kilograms + right.Kilograms);

    public static Mass operator -(Mass left, Mass right) => FromKilograms(left.Kilograms - right.Kilograms);

    public static Mass operator *(Mass value, double scalar) => FromKilograms(value.Kilograms * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static Mass operator *(double scalar, Mass value) => value * scalar;

    public static Mass operator /(Mass value, double divisor) => FromKilograms(value.Kilograms / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static Density operator /(Mass mass, Volume volume)
    {
        if (volume == Volume.Zero)
        {
            throw new DivideByZeroException("Cannot derive density from zero volume.");
        }

        return Density.FromKilogramsPerCubicMetre(mass.Kilograms / volume.CubicMetres);
    }

    public static bool operator <(Mass left, Mass right) => left.Kilograms < right.Kilograms;

    public static bool operator >(Mass left, Mass right) => left.Kilograms > right.Kilograms;

    public static bool operator <=(Mass left, Mass right) => left.Kilograms <= right.Kilograms;

    public static bool operator >=(Mass left, Mass right) => left.Kilograms >= right.Kilograms;
}
