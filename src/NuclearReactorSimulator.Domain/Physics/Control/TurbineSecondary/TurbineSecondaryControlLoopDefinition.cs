namespace NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;

/// <summary>Semantic binding from one generic M5.2 controller/actuator pair to one M5.4 secondary-cycle loop role.</summary>
public sealed class TurbineSecondaryControlLoopDefinition
{
    public TurbineSecondaryControlLoopDefinition(
        string id,
        TurbineSecondaryControlLoopKind kind,
        string controllerId,
        string actuatorId)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(controllerId) || string.IsNullOrWhiteSpace(actuatorId))
        {
            throw new ArgumentException("Loop, controller and actuator ids must be non-empty.");
        }

        if (!Enum.IsDefined(typeof(TurbineSecondaryControlLoopKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown turbine/secondary control-loop kind.");
        }

        Id = id.Trim();
        Kind = kind;
        ControllerId = controllerId.Trim();
        ActuatorId = actuatorId.Trim();
    }

    public string Id { get; }
    public TurbineSecondaryControlLoopKind Kind { get; }
    public string ControllerId { get; }
    public string ActuatorId { get; }
}
