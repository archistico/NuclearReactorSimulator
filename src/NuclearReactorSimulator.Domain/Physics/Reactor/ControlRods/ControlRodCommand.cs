namespace NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

/// <summary>
/// Persistent motion command applied to either one rod or an entire rod group.
/// </summary>
public sealed record ControlRodCommand
{
    public ControlRodCommand(string targetId, ControlRodCommandTargetKind targetKind, ControlRodMotion motion)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Control-rod command target cannot be empty.", nameof(targetId));
        }

        if (!Enum.IsDefined(typeof(ControlRodCommandTargetKind), targetKind))
        {
            throw new ArgumentOutOfRangeException(nameof(targetKind), targetKind, "Unknown control-rod command target kind.");
        }

        if (!Enum.IsDefined(typeof(ControlRodMotion), motion))
        {
            throw new ArgumentOutOfRangeException(nameof(motion), motion, "Unknown control-rod motion command.");
        }

        TargetId = targetId.Trim();
        TargetKind = targetKind;
        Motion = motion;
    }

    public string TargetId { get; }

    public ControlRodCommandTargetKind TargetKind { get; }

    public ControlRodMotion Motion { get; }
}
