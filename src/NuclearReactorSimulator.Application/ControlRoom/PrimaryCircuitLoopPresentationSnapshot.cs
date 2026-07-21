namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record PrimaryCircuitLoopPresentationSnapshot(
    string LoopId,
    ControlRoomValueSnapshot TotalPumpFlow,
    ControlRoomValueSnapshot HeaderPressureRise,
    ControlRoomValueSnapshot SuctionHeaderPressure,
    ControlRoomValueSnapshot PressureHeaderPressure,
    string FlowDirection,
    IReadOnlyList<PrimaryCircuitPumpPresentationSnapshot> Pumps,
    IReadOnlyList<PrimaryCircuitBranchPresentationSnapshot> Branches);
