namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record SecondaryPumpPresentationSnapshot(
    string PumpId,
    bool IsRunning,
    ControlRoomValueSnapshot Speed,
    ControlRoomValueSnapshot MassFlow,
    ControlRoomValueSnapshot PressureBoost,
    ControlRoomValueSnapshot ShaftPowerDemand)
{
    public string OperatingText => IsRunning ? "RUNNING" : "STOPPED";
}
