using System.Text.Json.Serialization;

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
    [property: JsonIgnore] ControlRoomValueSnapshot SeparableLiquidInventory,
    [property: JsonIgnore] ControlRoomValueSnapshot SeparableLiquidMassFraction,
    [property: JsonIgnore] string LiquidInventoryStatus,
    string Phase)
{
    public string PhaseText => $"Thermodynamic phase: {Phase}";
}
