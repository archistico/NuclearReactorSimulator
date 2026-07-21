namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>UI-safe canonical rod or rod-group command target.</summary>
public sealed record ReactorRodTargetPresentationSnapshot(
    string TargetId,
    ControlRoomCommandTargetKind TargetKind)
{
    public string Label => TargetKind == ControlRoomCommandTargetKind.ControlRodGroup
        ? $"GROUP · {TargetId}"
        : $"ROD · {TargetId}";
}
