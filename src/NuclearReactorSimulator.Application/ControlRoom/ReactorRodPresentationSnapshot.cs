namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Presentation-only state for one canonical control rod.</summary>
public sealed record ReactorRodPresentationSnapshot(
    string RodId,
    double PercentWithdrawn,
    string Motion,
    ControlRoomVisualState State)
{
    public string PositionText => FormattableString.Invariant($"{PercentWithdrawn:0.0}% withdrawn");
}
