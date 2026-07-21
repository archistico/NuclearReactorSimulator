namespace NuclearReactorSimulator.Domain.Physics.Quantities;

/// <summary>
/// Positive first-order decay constant, stored canonically in inverse seconds.
/// </summary>
public readonly record struct DecayConstant : IComparable<DecayConstant>
{
    private DecayConstant(double perSecond)
    {
        PerSecond = QuantityGuard.PositiveFinite(perSecond, nameof(perSecond));
    }

    public double PerSecond { get; }

    public double MeanLifetimeSeconds => 1d / PerSecond;

    public double HalfLifeSeconds => Math.Log(2d) / PerSecond;

    public static DecayConstant FromPerSecond(double value) => new(value);

    public static DecayConstant FromHalfLife(TimeSpan halfLife)
    {
        var seconds = QuantityGuard.PositiveSeconds(halfLife, nameof(halfLife));
        return new DecayConstant(Math.Log(2d) / seconds);
    }

    public int CompareTo(DecayConstant other) => PerSecond.CompareTo(other.PerSecond);
}
