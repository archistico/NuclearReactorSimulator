namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

public sealed record ControlRoomSubsystemSchematicsSnapshot(
    ControlRoomSubsystemSchematicSnapshot ReactorCore,
    ControlRoomSubsystemSchematicSnapshot PrimarySteamDrum,
    ControlRoomSubsystemSchematicSnapshot TurbineSecondary,
    ControlRoomSubsystemSchematicSnapshot GeneratorGrid,
    ControlRoomSubsystemSchematicSnapshot InstrumentationProtection)
{
    public static ControlRoomSubsystemSchematicsSnapshot Empty { get; } = new(
        ControlRoomSubsystemSchematicSnapshot.Empty(ControlRoomSubsystemSchematicKind.ReactorCore, "REACTOR / CORE"),
        ControlRoomSubsystemSchematicSnapshot.Empty(ControlRoomSubsystemSchematicKind.PrimarySteamDrum, "PRIMARY / STEAM DRUM"),
        ControlRoomSubsystemSchematicSnapshot.Empty(ControlRoomSubsystemSchematicKind.TurbineSecondary, "TURBINE / SECONDARY"),
        ControlRoomSubsystemSchematicSnapshot.Empty(ControlRoomSubsystemSchematicKind.GeneratorGrid, "GENERATOR / GRID"),
        ControlRoomSubsystemSchematicSnapshot.Empty(ControlRoomSubsystemSchematicKind.InstrumentationProtection, "INSTRUMENTATION / CONTROL / PROTECTION"));
}
