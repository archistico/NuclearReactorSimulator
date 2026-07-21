namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record PrimaryCircuitValvePresentationSnapshot(
    string ValveId,
    string FromNodeId,
    string ToNodeId,
    ControlRoomValueSnapshot Position,
    bool IsFailSafeActive)
{
    public string StateText => IsFailSafeActive ? "FAIL-SAFE ACTIVE" : "NORMAL AUTHORITY";

    public string PositionText => $"{Position.ValueText} {Position.Unit}".TrimEnd();

    public string EndpointText => $"{FromNodeId} → {ToNodeId}";
}
