namespace NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

/// <summary>
/// Immutable operational state of one control rod.
/// </summary>
public sealed record ControlRodState
{
    public ControlRodState(string rodId, ControlRodPosition position, ControlRodMotion motion = ControlRodMotion.Hold)
    {
        if (string.IsNullOrWhiteSpace(rodId))
        {
            throw new ArgumentException("Control-rod state id cannot be empty.", nameof(rodId));
        }

        if (!Enum.IsDefined(typeof(ControlRodMotion), motion))
        {
            throw new ArgumentOutOfRangeException(nameof(motion), motion, "Unknown control-rod motion state.");
        }

        RodId = rodId.Trim();
        Position = position;
        Motion = motion;
    }

    public string RodId { get; }

    public ControlRodPosition Position { get; }

    public ControlRodMotion Motion { get; }
}
