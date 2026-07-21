namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record PrimaryCircuitPumpPresentationSnapshot(
    string PumpId,
    string LoopId,
    bool IsRunning,
    ControlRoomValueSnapshot Speed,
    ControlRoomValueSnapshot MassFlow,
    ControlRoomValueSnapshot PressureBoost,
    bool IsOperatorCommandable)
{
    public string OperatingText => IsRunning ? "RUNNING" : "STOPPED";

    public string SpeedText => $"Speed {Speed.ValueText} {Speed.Unit}".TrimEnd();

    public string MassFlowText => $"Flow {MassFlow.ValueText} {MassFlow.Unit}".TrimEnd();

    public string PressureBoostText => $"Boost {PressureBoost.ValueText} {PressureBoost.Unit}".TrimEnd();
}
