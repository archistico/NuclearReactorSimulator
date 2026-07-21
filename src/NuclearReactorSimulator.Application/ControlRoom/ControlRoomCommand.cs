namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Typed operator intent crossing the Application boundary. Target metadata is supplied only for commands addressing a
/// specific canonical device/group; Avalonia never applies the command directly to simulation state.
/// </summary>
public sealed record ControlRoomCommand(
    ControlRoomCommandKind Kind,
    string? TargetId = null,
    ControlRoomCommandTargetKind? TargetKind = null);
