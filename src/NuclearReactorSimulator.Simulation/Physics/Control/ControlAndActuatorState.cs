using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed class ControlAndActuatorState
{
    public ControlAndActuatorState(ActuatorSystemDefinition definition, ControllerSystemState controllers, ActuatorSystemState actuators)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
        Actuators = actuators ?? throw new ArgumentNullException(nameof(actuators));
        if (!ReferenceEquals(controllers.Definition, definition.ControlSystem) || !ReferenceEquals(actuators.Definition, definition))
        {
            throw new ArgumentException("Control/actuator state does not use the canonical definitions.");
        }
    }

    public ActuatorSystemDefinition Definition { get; }
    public ControllerSystemState Controllers { get; }
    public ActuatorSystemState Actuators { get; }
}
