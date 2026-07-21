using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Signed linear void coefficient of reactivity, stored canonically as delta-k/k per unit void fraction.
/// </summary>
public readonly record struct VoidReactivityCoefficient : IComparable<VoidReactivityCoefficient>
{
    private const double DeltaKOverKPerPcm = 0.00001d;
    private const double FractionPerPercent = 0.01d;

    private VoidReactivityCoefficient(double deltaKOverKPerVoidFraction)
    {
        DeltaKOverKPerVoidFraction = QuantityGuard.Finite(
            deltaKOverKPerVoidFraction,
            nameof(deltaKOverKPerVoidFraction));
    }

    public double DeltaKOverKPerVoidFraction { get; }

    /// <summary>
    /// Reactivity change in pcm per one percentage-point change in void fraction.
    /// </summary>
    public double PcmPerPercentVoid
        => DeltaKOverKPerVoidFraction * FractionPerPercent / DeltaKOverKPerPcm;

    public static VoidReactivityCoefficient Zero { get; } = FromDeltaKOverKPerVoidFraction(0d);

    public static VoidReactivityCoefficient FromDeltaKOverKPerVoidFraction(double value) => new(value);

    public static VoidReactivityCoefficient FromPcmPerPercentVoid(double value)
        => new(value * DeltaKOverKPerPcm / FractionPerPercent);

    public int CompareTo(VoidReactivityCoefficient other)
        => DeltaKOverKPerVoidFraction.CompareTo(other.DeltaKOverKPerVoidFraction);

    public static Reactivity operator *(
        VoidReactivityCoefficient coefficient,
        VoidFractionDifference voidFractionDifference)
        => Reactivity.FromDeltaKOverK(
            coefficient.DeltaKOverKPerVoidFraction * voidFractionDifference.Fraction);

    public static Reactivity operator *(
        VoidFractionDifference voidFractionDifference,
        VoidReactivityCoefficient coefficient)
        => coefficient * voidFractionDifference;
}
