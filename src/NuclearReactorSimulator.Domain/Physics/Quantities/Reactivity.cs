namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Signed reactor reactivity, stored canonically as delta-k over k.
/// </summary>
public readonly record struct Reactivity : IComparable<Reactivity>
{
    private const double DeltaKOverKPerPercent = 0.01d;
    private const double DeltaKOverKPerPcm = 0.00001d;

    private Reactivity(double deltaKOverK)
    {
        DeltaKOverK = QuantityGuard.Finite(deltaKOverK, nameof(deltaKOverK));
    }

    /// <summary>
    /// Canonical dimensionless value rho = delta-k / k.
    /// </summary>
    public double DeltaKOverK { get; }

    public double PercentDeltaKOverK => DeltaKOverK / DeltaKOverKPerPercent;

    public double Pcm => DeltaKOverK / DeltaKOverKPerPcm;

    public static Reactivity Zero { get; } = FromDeltaKOverK(0d);

    public static Reactivity FromDeltaKOverK(double value) => new(value);

    public static Reactivity FromPercentDeltaKOverK(double value) => new(value * DeltaKOverKPerPercent);

    public static Reactivity FromPcm(double value) => new(value * DeltaKOverKPerPcm);

    public int CompareTo(Reactivity other) => DeltaKOverK.CompareTo(other.DeltaKOverK);

    public static Reactivity operator +(Reactivity left, Reactivity right)
        => FromDeltaKOverK(left.DeltaKOverK + right.DeltaKOverK);

    public static Reactivity operator -(Reactivity left, Reactivity right)
        => FromDeltaKOverK(left.DeltaKOverK - right.DeltaKOverK);

    public static Reactivity operator -(Reactivity value)
        => FromDeltaKOverK(-value.DeltaKOverK);

    public static Reactivity operator *(Reactivity value, double scalar)
        => FromDeltaKOverK(value.DeltaKOverK * QuantityGuard.Finite(scalar, nameof(scalar)));

    public static Reactivity operator *(double scalar, Reactivity value) => value * scalar;

    public static Reactivity operator /(Reactivity value, double divisor)
        => FromDeltaKOverK(value.DeltaKOverK / QuantityGuard.PositiveFinite(divisor, nameof(divisor)));

    public static bool operator <(Reactivity left, Reactivity right) => left.DeltaKOverK < right.DeltaKOverK;

    public static bool operator >(Reactivity left, Reactivity right) => left.DeltaKOverK > right.DeltaKOverK;

    public static bool operator <=(Reactivity left, Reactivity right) => left.DeltaKOverK <= right.DeltaKOverK;

    public static bool operator >=(Reactivity left, Reactivity right) => left.DeltaKOverK >= right.DeltaKOverK;
}
