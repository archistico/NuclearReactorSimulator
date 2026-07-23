namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

public sealed record ControlRoomSubsystemSchematicSnapshot(
    ControlRoomSubsystemSchematicKind Kind,
    string Title,
    string Subtitle,
    IReadOnlyList<ControlRoomSubsystemSchematicNodeSnapshot> Nodes,
    IReadOnlyList<ControlRoomSubsystemSchematicConnectionSnapshot> Connections,
    string OperatorContextText)
{
    public static ControlRoomSubsystemSchematicSnapshot Empty(ControlRoomSubsystemSchematicKind kind, string title) => new(
        kind,
        title,
        "NO SCHEMATIC DATA AVAILABLE",
        Array.Empty<ControlRoomSubsystemSchematicNodeSnapshot>(),
        Array.Empty<ControlRoomSubsystemSchematicConnectionSnapshot>(),
        "No integrated runtime presentation snapshot is available.");
}
