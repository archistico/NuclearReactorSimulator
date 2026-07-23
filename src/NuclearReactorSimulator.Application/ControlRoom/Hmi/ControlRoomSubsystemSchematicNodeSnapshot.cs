using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

public sealed record ControlRoomSubsystemSchematicNodeSnapshot(
    string NodeId,
    string DisplayName,
    ControlRoomSubsystemSchematicNodeKind Kind,
    double X,
    double Y,
    double Width,
    double Height,
    ControlRoomVisualState State,
    string StatusText,
    string PrimaryText,
    string SecondaryText,
    string InputText,
    string OutputText);
