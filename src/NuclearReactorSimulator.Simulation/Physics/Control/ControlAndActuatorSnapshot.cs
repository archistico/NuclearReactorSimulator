using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

/// <summary>Immutable M5.2 controller/actuator observation envelope for future loop orchestration and UI diagnostics.</summary>
public sealed class ControlAndActuatorSnapshot
{
    public ControlAndActuatorSnapshot(
        ActuatorSystemDefinition definition,
        ControllerSystemSnapshot controllers,
        ActuatorCommandFrame actuatorCommands)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
        ActuatorCommands = actuatorCommands ?? throw new ArgumentNullException(nameof(actuatorCommands));

        if (!ReferenceEquals(controllers.Definition, definition.ControlSystem)
            || !ReferenceEquals(actuatorCommands.Definition, definition))
        {
            throw new ArgumentException("Controller/actuator snapshot does not use the canonical definitions.");
        }
    }

    public ActuatorSystemDefinition Definition { get; }

    public ControllerSystemSnapshot Controllers { get; }

    public ActuatorCommandFrame ActuatorCommands { get; }
}
