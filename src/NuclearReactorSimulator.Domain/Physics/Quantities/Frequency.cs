namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Non-negative cyclic frequency in hertz.
/// </summary>
public readonly record struct Frequency : IComparable<Frequency>
{
    private Frequency(double hertz)
    {
        Hertz = QuantityGuard.NonNegativeFinite(hertz, nameof(hertz));
    }

    public double Hertz { get; }

    public static Frequency Zero { get; } = FromHertz(0d);

    public static Frequency FromHertz(double value) => new(value);

    public int CompareTo(Frequency other) => Hertz.CompareTo(other.Hertz);

    public static Frequency operator +(Frequency left, Frequency right) => FromHertz(left.Hertz + right.Hertz);

    public static bool operator <(Frequency left, Frequency right) => left.Hertz < right.Hertz;

    public static bool operator >(Frequency left, Frequency right) => left.Hertz > right.Hertz;

    public static bool operator <=(Frequency left, Frequency right) => left.Hertz <= right.Hertz;

    public static bool operator >=(Frequency left, Frequency right) => left.Hertz >= right.Hertz;
}
