namespace NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

/// <summary>
/// Normalized control-rod withdrawal position. Zero means fully inserted; one means fully withdrawn.
/// </summary>
public readonly record struct ControlRodPosition : IComparable<ControlRodPosition>
{
    private ControlRodPosition(double fractionWithdrawn)
    {
        if (!double.IsFinite(fractionWithdrawn) || fractionWithdrawn < 0d || fractionWithdrawn > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fractionWithdrawn),
                fractionWithdrawn,
                "Control-rod withdrawal fraction must be finite and between zero and one.");
        }

        FractionWithdrawn = fractionWithdrawn == 0d ? 0d : fractionWithdrawn;
    }

    public double FractionWithdrawn { get; }

    public double PercentWithdrawn => FractionWithdrawn * 100d;

    public double FractionInserted => 1d - FractionWithdrawn;

    public static ControlRodPosition FullyInserted { get; } = FromFractionWithdrawn(0d);

    public static ControlRodPosition FullyWithdrawn { get; } = FromFractionWithdrawn(1d);

    public static ControlRodPosition FromFractionWithdrawn(double value) => new(value);

    public static ControlRodPosition FromPercentWithdrawn(double value) => new(value / 100d);

    public int CompareTo(ControlRodPosition other) => FractionWithdrawn.CompareTo(other.FractionWithdrawn);
}
