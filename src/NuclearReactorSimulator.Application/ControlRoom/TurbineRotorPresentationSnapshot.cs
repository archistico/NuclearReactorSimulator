namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record TurbineRotorPresentationSnapshot(
    string RotorId,
    ControlRoomValueSnapshot Speed,
    ControlRoomValueSnapshot ShaftPower,
    ControlRoomValueSnapshot NetTorque,
    bool TripCommandActive,
    bool OverspeedDetected)
{
    public ControlRoomVisualState State => TripCommandActive || OverspeedDetected
        ? ControlRoomVisualState.Trip
        : Speed.State;

    public string ProtectionText => TripCommandActive
        ? "TRIP COMMAND ACTIVE"
        : OverspeedDetected ? "OVERSPEED DETECTED" : "NORMAL";
}
