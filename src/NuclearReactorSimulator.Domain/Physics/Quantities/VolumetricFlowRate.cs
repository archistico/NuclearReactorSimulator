namespace NuclearReactorSimulator.Domain.Physics.Quantities;

public readonly record struct VolumetricFlowRate : IComparable<VolumetricFlowRate>
{
    private const double LitresPerCubicMetre = 1_000d;
    private const double SecondsPerHour = 3_600d;

    private VolumetricFlowRate(double cubicMetresPerSecond)
    {
        CubicMetresPerSecond = QuantityGuard.Finite(cubicMetresPerSecond, nameof(cubicMetresPerSecond));
    }

    public double CubicMetresPerSecond { get; }

    public double LitresPerSecond => CubicMetresPerSecond * LitresPerCubicMetre;

    public double CubicMetresPerHour => CubicMetresPerSecond * SecondsPerHour;

    public static VolumetricFlowRate Zero { get; } = FromCubicMetresPerSecond(0d);

    public static VolumetricFlowRate FromCubicMetresPerSecond(double value) => new(value);

    public static VolumetricFlowRate FromLitresPerSecond(double value) => new(value / LitresPerCubicMetre);

    public static VolumetricFlowRate FromCubicMetresPerHour(double value) => new(value / SecondsPerHour);

    public int CompareTo(VolumetricFlowRate other) => CubicMetresPerSecond.CompareTo(other.CubicMetresPerSecond);

    public static VolumetricFlowRate operator +(VolumetricFlowRate left, VolumetricFlowRate right) => FromCubicMetresPerSecond(left.CubicMetresPerSecond + right.CubicMetresPerSecond);

    public static VolumetricFlowRate operator -(VolumetricFlowRate left, VolumetricFlowRate right) => FromCubicMetresPerSecond(left.CubicMetresPerSecond - right.CubicMetresPerSecond);

    public static VolumetricFlowRate operator -(VolumetricFlowRate value) => FromCubicMetresPerSecond(-value.CubicMetresPerSecond);

    public static VolumetricFlowRate operator *(VolumetricFlowRate value, double scalar) => FromCubicMetresPerSecond(value.CubicMetresPerSecond * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static VolumetricFlowRate operator *(double scalar, VolumetricFlowRate value) => value * scalar;

    public static VolumetricFlowRate operator /(VolumetricFlowRate value, double divisor) => FromCubicMetresPerSecond(value.CubicMetresPerSecond / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static bool operator <(VolumetricFlowRate left, VolumetricFlowRate right) => left.CubicMetresPerSecond < right.CubicMetresPerSecond;

    public static bool operator >(VolumetricFlowRate left, VolumetricFlowRate right) => left.CubicMetresPerSecond > right.CubicMetresPerSecond;

    public static bool operator <=(VolumetricFlowRate left, VolumetricFlowRate right) => left.CubicMetresPerSecond <= right.CubicMetresPerSecond;

    public static bool operator >=(VolumetricFlowRate left, VolumetricFlowRate right) => left.CubicMetresPerSecond >= right.CubicMetresPerSecond;
}
