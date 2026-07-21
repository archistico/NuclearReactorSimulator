namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Non-negative rotational speed magnitude. Radians are dimensionless in SI arithmetic.
/// </summary>
public readonly record struct AngularSpeed : IComparable<AngularSpeed>
{
    private const double SecondsPerMinute = 60d;
    private const double RadiansPerRevolution = 2d * Math.PI;

    private AngularSpeed(double radiansPerSecond)
    {
        RadiansPerSecond = QuantityGuard.NonNegativeFinite(radiansPerSecond, nameof(radiansPerSecond));
    }

    public double RadiansPerSecond { get; }

    public double RevolutionsPerMinute => RadiansPerSecond * SecondsPerMinute / RadiansPerRevolution;

    public static AngularSpeed Zero { get; } = FromRadiansPerSecond(0d);

    public static AngularSpeed FromRadiansPerSecond(double value) => new(value);

    public static AngularSpeed FromRevolutionsPerMinute(double value)
        => new(QuantityGuard.NonNegativeFinite(value, nameof(value)) * RadiansPerRevolution / SecondsPerMinute);

    public int CompareTo(AngularSpeed other) => RadiansPerSecond.CompareTo(other.RadiansPerSecond);

    public static bool operator <(AngularSpeed left, AngularSpeed right) => left.RadiansPerSecond < right.RadiansPerSecond;

    public static bool operator >(AngularSpeed left, AngularSpeed right) => left.RadiansPerSecond > right.RadiansPerSecond;

    public static bool operator <=(AngularSpeed left, AngularSpeed right) => left.RadiansPerSecond <= right.RadiansPerSecond;

    public static bool operator >=(AngularSpeed left, AngularSpeed right) => left.RadiansPerSecond >= right.RadiansPerSecond;
}
