using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Control;

/// <summary>Canonical controller-to-actuator command bindings for M5.2.</summary>
public sealed class ActuatorSystemDefinition
{
    public ActuatorSystemDefinition(string id, ControlSystemDefinition controlSystem, IEnumerable<ActuatorDefinition> actuators)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Actuator-system id cannot be empty or whitespace.", nameof(id));
        }

        ControlSystem = controlSystem ?? throw new ArgumentNullException(nameof(controlSystem));
        ArgumentNullException.ThrowIfNull(actuators);

        var canonical = actuators
            .Select(item => item ?? throw new ArgumentException("Actuator definitions cannot contain null entries.", nameof(actuators)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (canonical.Length == 0)
        {
            throw new ArgumentException("An actuator system must contain at least one actuator.", nameof(actuators));
        }

        if (canonical.Select(static item => item.Id).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Actuator ids must be unique.", nameof(actuators));
        }

        var controllerIds = controlSystem.Controllers.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var actuator in canonical)
        {
            if (!controllerIds.Contains(actuator.ControllerId))
            {
                throw new ArgumentException($"Actuator '{actuator.Id}' references unknown controller '{actuator.ControllerId}'.", nameof(actuators));
            }

            var controller = controlSystem.GetController(actuator.ControllerId);
            if (actuator.InputRange != controller.OutputRange)
            {
                throw new ArgumentException(
                    $"Actuator '{actuator.Id}' input range must exactly match controller '{controller.Id}' output range.",
                    nameof(actuators));
            }

            var targetKey = $"{actuator.TargetKind}:{actuator.TargetId}";
            if (!targets.Add(targetKey))
            {
                throw new ArgumentException($"Actuator target '{targetKey}' is assigned more than once.", nameof(actuators));
            }
        }

        Id = id.Trim();
        Actuators = new ReadOnlyCollection<ActuatorDefinition>(canonical);
    }

    public string Id { get; }
    public ControlSystemDefinition ControlSystem { get; }
    public IReadOnlyList<ActuatorDefinition> Actuators { get; }

    public ActuatorDefinition GetActuator(string id)
        => Actuators.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown actuator '{id}'.");
}
