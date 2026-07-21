namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct MassFlowRate : IComparable<MassFlowRate>
{
    private const double SecondsPerHour = 3_600d;

    private MassFlowRate(double kilogramsPerSecond)
    {
        KilogramsPerSecond = QuantityGuard.Finite(kilogramsPerSecond, nameof(kilogramsPerSecond));
    }

    public double KilogramsPerSecond { get; }

    public double KilogramsPerHour => KilogramsPerSecond * SecondsPerHour;

    public static MassFlowRate Zero { get; } = FromKilogramsPerSecond(0d);

    public static MassFlowRate FromKilogramsPerSecond(double value) => new(value);

    public static MassFlowRate FromKilogramsPerHour(double value) => new(value / SecondsPerHour);

    public int CompareTo(MassFlowRate other) => KilogramsPerSecond.CompareTo(other.KilogramsPerSecond);

    public static MassFlowRate operator +(MassFlowRate left, MassFlowRate right) => FromKilogramsPerSecond(left.KilogramsPerSecond + right.KilogramsPerSecond);

    public static MassFlowRate operator -(MassFlowRate left, MassFlowRate right) => FromKilogramsPerSecond(left.KilogramsPerSecond - right.KilogramsPerSecond);

    public static MassFlowRate operator -(MassFlowRate value) => FromKilogramsPerSecond(-value.KilogramsPerSecond);

    public static MassFlowRate operator *(MassFlowRate value, double scalar) => FromKilogramsPerSecond(value.KilogramsPerSecond * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static MassFlowRate operator *(double scalar, MassFlowRate value) => value * scalar;

    public static MassFlowRate operator /(MassFlowRate value, double divisor) => FromKilogramsPerSecond(value.KilogramsPerSecond / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static VolumetricFlowRate operator /(MassFlowRate massFlowRate, Density density)
    {
        var kilogramsPerCubicMetre = QuantityGuard.PositiveFinite(density.KilogramsPerCubicMetre, nameof(density));
        return VolumetricFlowRate.FromCubicMetresPerSecond(massFlowRate.KilogramsPerSecond / kilogramsPerCubicMetre);
    }

    public static bool operator <(MassFlowRate left, MassFlowRate right) => left.KilogramsPerSecond < right.KilogramsPerSecond;

    public static bool operator >(MassFlowRate left, MassFlowRate right) => left.KilogramsPerSecond > right.KilogramsPerSecond;

    public static bool operator <=(MassFlowRate left, MassFlowRate right) => left.KilogramsPerSecond <= right.KilogramsPerSecond;

    public static bool operator >=(MassFlowRate left, MassFlowRate right) => left.KilogramsPerSecond >= right.KilogramsPerSecond;
}
