using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

/// <summary>Per-step M5.4 controller inputs. Protection overrides remain deferred to M5.5.</summary>
public sealed class TurbineSecondaryControlInputs
{
    public TurbineSecondaryControlInputs(TurbineSecondaryControlSystemDefinition definition, ControllerInputs controllers)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
        if (!ReferenceEquals(controllers.Definition, definition.ActuatorSystem.ControlSystem))
        {
            throw new ArgumentException("Controller inputs do not use the turbine/secondary control system's canonical controller definition.", nameof(controllers));
        }
    }

    public TurbineSecondaryControlSystemDefinition Definition { get; }
    public ControllerInputs Controllers { get; }
}
