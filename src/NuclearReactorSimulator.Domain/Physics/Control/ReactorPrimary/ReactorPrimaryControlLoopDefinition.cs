namespace NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;

/// <summary>Semantic binding from one generic M5.2 controller/actuator pair to one M5.3 reactor/primary loop role.</summary>
public sealed class ReactorPrimaryControlLoopDefinition
{
    public ReactorPrimaryControlLoopDefinition(
        string id,
        ReactorPrimaryControlLoopKind kind,
        string controllerId,
        string actuatorId)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(controllerId) || string.IsNullOrWhiteSpace(actuatorId))
        {
            throw new ArgumentException("Loop, controller and actuator ids must be non-empty.");
        }

        if (!Enum.IsDefined(typeof(ReactorPrimaryControlLoopKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown reactor/primary control-loop kind.");
        }

        Id = id.Trim();
        Kind = kind;
        ControllerId = controllerId.Trim();
        ActuatorId = actuatorId.Trim();
    }

    public string Id { get; }
    public ReactorPrimaryControlLoopKind Kind { get; }
    public string ControllerId { get; }
    public string ActuatorId { get; }
}
