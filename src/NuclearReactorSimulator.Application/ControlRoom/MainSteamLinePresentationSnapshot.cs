namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record MainSteamLinePresentationSnapshot(
    string LineId,
    string SourceNodeId,
    string HeaderNodeId,
    ControlRoomValueSnapshot MassFlow,
    ControlRoomValueSnapshot PressureDifference,
    string FlowDirection)
{
    public string EndpointText => $"{SourceNodeId} → {HeaderNodeId}";
}
