namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record PrimaryCircuitSteamDrumPresentationSnapshot(
    string DrumId,
    string LoopId,
    ControlRoomValueSnapshot Pressure,
    ControlRoomValueSnapshot Level,
    ControlRoomValueSnapshot Temperature,
    ControlRoomValueSnapshot IncomingReturnFlow,
    ControlRoomValueSnapshot SteamFlow,
    ControlRoomValueSnapshot RecirculationFlow,
    string Phase)
{
    public string PhaseText => $"Thermodynamic phase: {Phase}";
}
