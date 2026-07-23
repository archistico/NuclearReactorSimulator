using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

public sealed record ControlRoomSubsystemSchematicConnectionSnapshot(
    string ConnectionId,
    string FromNodeId,
    string ToNodeId,
    ControlRoomSubsystemSchematicConnectionKind Kind,
    string Label,
    string PrimaryText,
    string SecondaryText,
    ControlRoomVisualState State,
    double LabelX,
    double LabelY,
    IReadOnlyList<ControlRoomSubsystemSchematicPointSnapshot> Route);
