namespace NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

/// <summary>
/// Non-negative normalized poison-inventory production rate at the definition's reference fission power.
/// </summary>
public readonly record struct PoisonProductionRate : IComparable<PoisonProductionRate>
{
    private PoisonProductionRate(double relativePerSecond)
    {
        if (!double.IsFinite(relativePerSecond) || relativePerSecond < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(relativePerSecond),
                relativePerSecond,
                "Poison production rate must be finite and non-negative.");
        }

        RelativePerSecond = relativePerSecond == 0d ? 0d : relativePerSecond;
    }

    public double RelativePerSecond { get; }

    public static PoisonProductionRate Zero { get; } = FromRelativePerSecond(0d);

    public static PoisonProductionRate FromRelativePerSecond(double value) => new(value);

    public int CompareTo(PoisonProductionRate other) => RelativePerSecond.CompareTo(other.RelativePerSecond);
}
