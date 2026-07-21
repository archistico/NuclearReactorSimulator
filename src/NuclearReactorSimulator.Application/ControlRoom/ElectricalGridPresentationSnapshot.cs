namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record ElectricalGridPresentationSnapshot(
    string GridId,
    ControlRoomValueSnapshot Frequency,
    ControlRoomValueSnapshot LineVoltage,
    ControlRoomValueSnapshot PhaseAngle)
{
    public static ElectricalGridPresentationSnapshot Unavailable { get; } = new(
        "—",
        ControlRoomValueSnapshot.Unavailable("Hz"),
        ControlRoomValueSnapshot.Unavailable("kV"),
        ControlRoomValueSnapshot.Unavailable("°"));
}
