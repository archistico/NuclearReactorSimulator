using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

/// <summary>
/// M5.3 per-step control inputs. Non-rod reactivity is an explicit seam for the already validated temperature/void/xenon/manual
/// contributions; M5.3 does not hide or recompute those effects inside the controller layer.
/// </summary>
public sealed class ReactorPrimaryControlInputs
{
    public ReactorPrimaryControlInputs(
        ReactorPrimaryControlSystemDefinition definition,
        ControllerInputs controllers,
        Reactivity nonRodReactivity)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
        if (!ReferenceEquals(controllers.Definition, definition.ActuatorSystem.ControlSystem))
        {
            throw new ArgumentException("Controller inputs do not use the reactor/primary control system's canonical controller definition.", nameof(controllers));
        }

        NonRodReactivity = nonRodReactivity;
    }

    public ReactorPrimaryControlSystemDefinition Definition { get; }
    public ControllerInputs Controllers { get; }
    public Reactivity NonRodReactivity { get; }
}
