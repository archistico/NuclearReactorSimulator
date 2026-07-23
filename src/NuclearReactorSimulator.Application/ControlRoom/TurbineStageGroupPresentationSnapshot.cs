namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record TurbineStageGroupPresentationSnapshot(
    string StageGroupId,
    string RotorId,
    string InletNodeId,
    string ExhaustNodeId,
    ControlRoomValueSnapshot SteamFlow,
    ControlRoomValueSnapshot ShaftPower,
    ControlRoomValueSnapshot InletPressure,
    ControlRoomValueSnapshot InletTemperature,
    string InletPhase,
    ControlRoomValueSnapshot AvailableSpecificWork,
    ControlRoomValueSnapshot ExtractedSpecificWork,
    bool ThermodynamicWorkModelActive,
    bool ThermodynamicWorkLimited,
    bool TripBlocked)
{
    public string EndpointText => $"{InletNodeId} → {ExhaustNodeId}";
}
