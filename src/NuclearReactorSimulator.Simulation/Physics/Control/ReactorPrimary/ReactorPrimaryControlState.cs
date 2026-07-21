using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

/// <summary>
/// M5.3 state envelope for control algorithms plus the already validated M2 rod and point-kinetics states.
/// Pump physical state remains inside the canonical full-plant PlantState.
/// </summary>
public sealed class ReactorPrimaryControlState
{
    public ReactorPrimaryControlState(
        ReactorPrimaryControlSystemDefinition definition,
        ControlAndActuatorState controlAndActuator,
        ControlRodSystemState controlRods,
        PointKineticsState pointKinetics)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ControlAndActuator = controlAndActuator ?? throw new ArgumentNullException(nameof(controlAndActuator));
        ControlRods = controlRods ?? throw new ArgumentNullException(nameof(controlRods));
        PointKinetics = pointKinetics ?? throw new ArgumentNullException(nameof(pointKinetics));

        if (!ReferenceEquals(controlAndActuator.Definition, definition.ActuatorSystem))
        {
            throw new ArgumentException("Control/actuator state does not use the reactor/primary system's canonical definition.", nameof(controlAndActuator));
        }

        ValidateRodCoverage(definition.ControlRods, controlRods);
        ValidateKineticsCoverage(definition.PointKineticsParameters, pointKinetics);
    }

    public ReactorPrimaryControlSystemDefinition Definition { get; }
    public ControlAndActuatorState ControlAndActuator { get; }
    public ControlRodSystemState ControlRods { get; }
    public PointKineticsState PointKinetics { get; }

    public static ReactorPrimaryControlState CreateInitial(
        ReactorPrimaryControlSystemDefinition definition,
        ControlRodSystemState controlRods,
        PointKineticsState pointKinetics)
        => new(
            definition,
            new ControlAndActuatorState(
                definition.ActuatorSystem,
                ControllerSystemState.CreateUninitialized(definition.ActuatorSystem.ControlSystem),
                ActuatorSystemState.CreateInitial(definition.ActuatorSystem)),
            controlRods,
            pointKinetics);

    private static void ValidateRodCoverage(ControlRodSystemDefinition definition, ControlRodSystemState state)
    {
        var expected = definition.Rods.Select(static rod => rod.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = state.Rods.Select(static rod => rod.RodId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Control-rod state does not cover the canonical M5.3 rod definition.", nameof(state));
        }
    }

    private static void ValidateKineticsCoverage(PointKineticsParameters definition, PointKineticsState state)
    {
        var expected = definition.DelayedNeutronGroups.Select(static group => group.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = state.DelayedNeutronGroups.Select(static group => group.GroupId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Point-kinetics state does not cover the canonical M5.3 delayed-neutron definition.", nameof(state));
        }
    }
}
