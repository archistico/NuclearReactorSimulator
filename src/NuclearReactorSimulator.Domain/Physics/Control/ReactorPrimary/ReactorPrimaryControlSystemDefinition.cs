using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;

/// <summary>
/// Canonical M5.3 composition of generic M5.2 controllers/actuators with reactor rods and primary-circulation pumps.
/// It validates ownership only; physical state remains in the pre-existing rod and plant domains.
/// </summary>
public sealed class ReactorPrimaryControlSystemDefinition
{
    public ReactorPrimaryControlSystemDefinition(
        string id,
        IntegratedSecondaryCycleDefinition plantDefinition,
        ControlRodSystemDefinition controlRods,
        PointKineticsParameters pointKineticsParameters,
        FissionPowerDefinition fissionPowerDefinition,
        ActuatorSystemDefinition actuatorSystem,
        IEnumerable<ReactorPrimaryControlLoopDefinition> loops)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Reactor/primary control-system id cannot be empty or whitespace.", nameof(id));
        }

        PlantDefinition = plantDefinition ?? throw new ArgumentNullException(nameof(plantDefinition));
        ControlRods = controlRods ?? throw new ArgumentNullException(nameof(controlRods));
        PointKineticsParameters = pointKineticsParameters ?? throw new ArgumentNullException(nameof(pointKineticsParameters));
        FissionPowerDefinition = fissionPowerDefinition ?? throw new ArgumentNullException(nameof(fissionPowerDefinition));
        ActuatorSystem = actuatorSystem ?? throw new ArgumentNullException(nameof(actuatorSystem));
        ArgumentNullException.ThrowIfNull(loops);

        var canonical = loops
            .Select(item => item ?? throw new ArgumentException("Reactor/primary control-loop definitions cannot contain null entries.", nameof(loops)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (canonical.Length == 0)
        {
            throw new ArgumentException("A reactor/primary control system must contain at least one loop.", nameof(loops));
        }

        EnsureUnique(canonical.Select(static item => item.Id), "loop id", nameof(loops));
        EnsureUnique(canonical.Select(static item => item.ControllerId), "controller assignment", nameof(loops));
        EnsureUnique(canonical.Select(static item => item.ActuatorId), "actuator assignment", nameof(loops));

        var expectedControllerIds = actuatorSystem.ControlSystem.Controllers.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var loopControllerIds = canonical.Select(static item => item.ControllerId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var expectedActuatorIds = actuatorSystem.Actuators.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var loopActuatorIds = canonical.Select(static item => item.ActuatorId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (!expectedControllerIds.SequenceEqual(loopControllerIds, StringComparer.Ordinal)
            || !expectedActuatorIds.SequenceEqual(loopActuatorIds, StringComparer.Ordinal))
        {
            throw new ArgumentException("M5.3 loop definitions must cover every controller and actuator exactly once.", nameof(loops));
        }

        var hasPowerLoop = false;
        foreach (var loop in canonical)
        {
            var controller = actuatorSystem.ControlSystem.GetController(loop.ControllerId);
            var actuator = actuatorSystem.GetActuator(loop.ActuatorId);
            if (!string.Equals(actuator.ControllerId, controller.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Loop '{loop.Id}' controller '{controller.Id}' does not own actuator '{actuator.Id}'.",
                    nameof(loops));
            }

            switch (loop.Kind)
            {
                case ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation:
                    ValidatePowerLoop(loop, controller, actuator);
                    hasPowerLoop = true;
                    break;
                case ReactorPrimaryControlLoopKind.MainCirculationPumpFlow:
                case ReactorPrimaryControlLoopKind.MainCirculationHeaderPressure:
                    ValidateCirculationLoop(loop, controller, actuator);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loops), loop.Kind, "Unknown reactor/primary control-loop kind.");
            }
        }

        if (!hasPowerLoop)
        {
            throw new ArgumentException("M5.3 requires at least one reactor-power/rod-regulation loop.", nameof(loops));
        }

        Id = id.Trim();
        Loops = new ReadOnlyCollection<ReactorPrimaryControlLoopDefinition>(canonical);
    }

    public string Id { get; }
    public IntegratedSecondaryCycleDefinition PlantDefinition { get; }
    public ControlRodSystemDefinition ControlRods { get; }
    public PointKineticsParameters PointKineticsParameters { get; }
    public FissionPowerDefinition FissionPowerDefinition { get; }
    public ActuatorSystemDefinition ActuatorSystem { get; }
    public IReadOnlyList<ReactorPrimaryControlLoopDefinition> Loops { get; }

    public ReactorPrimaryControlLoopDefinition GetLoop(string id)
        => Loops.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown reactor/primary control loop '{id}'.");

    private void ValidatePowerLoop(
        ReactorPrimaryControlLoopDefinition loop,
        PidControllerDefinition controller,
        ActuatorDefinition actuator)
    {
        if (actuator.TargetKind != ActuatorTargetKind.ControlRod)
        {
            throw new ArgumentException($"Power loop '{loop.Id}' must target a control rod or rod group.", nameof(loop));
        }

        var targetKind = actuator.RodTargetKind
            ?? throw new ArgumentException($"Power-loop actuator '{actuator.Id}' has no rod target kind.", nameof(loop));
        if (targetKind == ControlRodCommandTargetKind.Rod)
        {
            _ = ControlRods.GetRod(actuator.TargetId);
        }
        else
        {
            _ = ControlRods.GetGroup(actuator.TargetId);
        }

        var channel = ActuatorSystem.ControlSystem.Instrumentation.GetChannel(controller.MeasurementChannelId);
        if (!string.Equals(channel.SourceId, "plant/reactor/thermal-power", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Power loop '{loop.Id}' must consume an instrument channel sourced from 'plant/reactor/thermal-power'.",
                nameof(loop));
        }
    }

    private void ValidateCirculationLoop(
        ReactorPrimaryControlLoopDefinition loop,
        PidControllerDefinition controller,
        ActuatorDefinition actuator)
    {
        if (actuator.TargetKind != ActuatorTargetKind.Pump)
        {
            throw new ArgumentException($"Circulation loop '{loop.Id}' must target a pump.", nameof(loop));
        }

        var circulationLoop = PlantDefinition.PrimaryCircuit.MainCirculationSystem.Loops
            .SingleOrDefault(item => item.PumpIds.Contains(actuator.TargetId, StringComparer.Ordinal))
            ?? throw new ArgumentException(
                $"Circulation-loop actuator '{actuator.Id}' targets pump '{actuator.TargetId}', which is not a canonical main-circulation pump.",
                nameof(loop));

        var channel = ActuatorSystem.ControlSystem.Instrumentation.GetChannel(controller.MeasurementChannelId);
        var expectedSourceId = loop.Kind == ReactorPrimaryControlLoopKind.MainCirculationPumpFlow
            ? $"main-circulation-loop/{circulationLoop.Id}/total-pump-flow"
            : $"main-circulation-loop/{circulationLoop.Id}/header-pressure-rise";
        if (!string.Equals(channel.SourceId, expectedSourceId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Circulation loop '{loop.Id}' must consume source '{expectedSourceId}', not '{channel.SourceId}'.",
                nameof(loop));
        }
    }

    private static void EnsureUnique(IEnumerable<string> values, string label, string parameterName)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!seen.Add(value))
            {
                throw new ArgumentException($"Duplicate reactor/primary {label} '{value}'.", parameterName);
            }
        }
    }
}
