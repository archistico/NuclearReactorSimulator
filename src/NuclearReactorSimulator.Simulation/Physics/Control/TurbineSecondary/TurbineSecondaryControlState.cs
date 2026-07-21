using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

/// <summary>M5.4 algorithm/command state only; physical valve and pump state remains in canonical PlantState.</summary>
public sealed class TurbineSecondaryControlState
{
    public TurbineSecondaryControlState(TurbineSecondaryControlSystemDefinition definition, ControlAndActuatorState controlAndActuator)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ControlAndActuator = controlAndActuator ?? throw new ArgumentNullException(nameof(controlAndActuator));
        if (!ReferenceEquals(controlAndActuator.Definition, definition.ActuatorSystem))
        {
            throw new ArgumentException("Control/actuator state does not use the turbine/secondary system's canonical actuator definition.", nameof(controlAndActuator));
        }
    }

    public TurbineSecondaryControlSystemDefinition Definition { get; }
    public ControlAndActuatorState ControlAndActuator { get; }

    public static TurbineSecondaryControlState CreateInitial(TurbineSecondaryControlSystemDefinition definition)
        => new(
            definition,
            new ControlAndActuatorState(
                definition.ActuatorSystem,
                ControllerSystemState.CreateUninitialized(definition.ActuatorSystem.ControlSystem),
                ActuatorSystemState.CreateInitial(definition.ActuatorSystem)));
}
