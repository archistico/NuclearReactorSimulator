namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record ControlRoomWorkspaceDescriptor(
    ControlRoomWorkspaceId Id,
    string Title,
    string ShortTitle,
    string Description);
