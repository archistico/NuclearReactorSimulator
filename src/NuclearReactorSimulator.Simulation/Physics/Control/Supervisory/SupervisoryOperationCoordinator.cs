using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Supervisory;

/// <summary>
/// M10.6 deterministic M5 supervisor. It coordinates existing local controllers only: no physical result, actuator state,
/// protection latch or alarm state is ever assigned here.
/// </summary>
public sealed class SupervisoryOperationCoordinator
{
    public SupervisoryOperationStepResult Step(
        SupervisoryOperationState committedState,
        PlantControlAuthorityMode requestedAuthority,
        SupervisoryOperatingObjective? requestedObjective,
        MeasuredSignalFrame measuredSignals,
        ProtectionSystemSnapshot protection,
        ReactorPrimaryControlState reactorControlState,
        TurbineSecondaryControlState secondaryControlState,
        ReactorPrimaryControlInputs reactorInputs,
        TurbineSecondaryControlInputs secondaryInputs)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(measuredSignals);
        ArgumentNullException.ThrowIfNull(protection);
        ArgumentNullException.ThrowIfNull(reactorControlState);
        ArgumentNullException.ThrowIfNull(secondaryControlState);
        ArgumentNullException.ThrowIfNull(reactorInputs);
        ArgumentNullException.ThrowIfNull(secondaryInputs);
        if (!Enum.IsDefined(requestedAuthority))
        {
            throw new ArgumentOutOfRangeException(nameof(requestedAuthority));
        }

        ValidateCanonicalBindings(measuredSignals, reactorControlState, secondaryControlState, reactorInputs, secondaryInputs);

        if (requestedAuthority == PlantControlAuthorityMode.Manual)
        {
            var manualReactor = ToBumplessManual(
                reactorInputs,
                reactorControlState.ControlAndActuator.Controllers);
            var manualSecondary = ToBumplessManual(
                secondaryInputs,
                secondaryControlState.ControlAndActuator.Controllers);
            var candidate = NextState(
                committedState,
                requestedAuthority,
                PlantControlAuthorityMode.Manual,
                PlantControlAuthorityHealth.Normal,
                null,
                requestedObjective);
            return new SupervisoryOperationStepResult(candidate, manualReactor, manualSecondary, true);
        }

        if (requestedAuthority == PlantControlAuthorityMode.Assisted)
        {
            var candidate = NextState(
                committedState,
                requestedAuthority,
                PlantControlAuthorityMode.Assisted,
                PlantControlAuthorityHealth.Normal,
                null,
                requestedObjective);
            return new SupervisoryOperationStepResult(candidate, reactorInputs, secondaryInputs, false);
        }

        if (protection.ReactorScramActive || protection.TurbineTripActive || protection.GeneratorTripActive)
        {
            var reason = BuildProtectionSuspensionReason(protection);
            var candidate = NextState(
                committedState,
                requestedAuthority,
                PlantControlAuthorityMode.Assisted,
                PlantControlAuthorityHealth.SuspendedByProtection,
                reason,
                requestedObjective);
            return new SupervisoryOperationStepResult(candidate, reactorInputs, secondaryInputs, false);
        }

        if (requestedObjective is null)
        {
            var candidate = NextState(
                committedState,
                requestedAuthority,
                PlantControlAuthorityMode.Assisted,
                PlantControlAuthorityHealth.Degraded,
                "No supervisory operating objective is configured.",
                null);
            return new SupervisoryOperationStepResult(candidate, reactorInputs, secondaryInputs, false);
        }

        if (!TryValidateRequiredMeasurements(
                requestedObjective,
                measuredSignals,
                reactorInputs.Definition,
                secondaryInputs.Definition,
                out var measurementFailure))
        {
            var candidate = NextState(
                committedState,
                requestedAuthority,
                PlantControlAuthorityMode.Assisted,
                PlantControlAuthorityHealth.Degraded,
                measurementFailure,
                requestedObjective);
            return new SupervisoryOperationStepResult(candidate, reactorInputs, secondaryInputs, false);
        }

        var supervisedReactor = reactorInputs;
        var supervisedSecondary = secondaryInputs;
        switch (requestedObjective.Kind)
        {
            case SupervisoryOperatingObjectiveKind.HoldReactorPower:
                supervisedReactor = SetReactorPowerAutomatic(
                    reactorInputs,
                    reactorControlState.ControlAndActuator.Controllers,
                    requestedObjective.ReactorPowerSetpointWatts!.Value);
                break;
            case SupervisoryOperatingObjectiveKind.HoldTurbineSpeed:
                supervisedSecondary = SetTurbineSpeedAutomatic(
                    secondaryInputs,
                    secondaryControlState.ControlAndActuator.Controllers,
                    requestedObjective.TurbineSpeedSetpointRpm!.Value);
                break;
            case SupervisoryOperatingObjectiveKind.HoldOperatingPoint:
                supervisedReactor = SetReactorPowerAutomatic(
                    reactorInputs,
                    reactorControlState.ControlAndActuator.Controllers,
                    requestedObjective.ReactorPowerSetpointWatts!.Value);
                supervisedSecondary = SetTurbineSpeedAutomatic(
                    secondaryInputs,
                    secondaryControlState.ControlAndActuator.Controllers,
                    requestedObjective.TurbineSpeedSetpointRpm!.Value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(requestedObjective), requestedObjective.Kind, "Unsupported supervisory objective kind.");
        }

        var activeState = NextState(
            committedState,
            requestedAuthority,
            PlantControlAuthorityMode.SupervisoryAutomatic,
            PlantControlAuthorityHealth.Normal,
            null,
            requestedObjective);
        return new SupervisoryOperationStepResult(activeState, supervisedReactor, supervisedSecondary, true);
    }

    private static ReactorPrimaryControlInputs ToBumplessManual(
        ReactorPrimaryControlInputs inputs,
        ControllerSystemState committedControllers)
        => new(
            inputs.Definition,
            ToBumplessManual(inputs.Controllers, committedControllers),
            inputs.NonRodReactivity);

    private static TurbineSecondaryControlInputs ToBumplessManual(
        TurbineSecondaryControlInputs inputs,
        ControllerSystemState committedControllers)
        => new(
            inputs.Definition,
            ToBumplessManual(inputs.Controllers, committedControllers));

    private static ControllerInputs ToBumplessManual(
        ControllerInputs inputs,
        ControllerSystemState committedControllers)
        => new(
            inputs.Definition,
            inputs.Controllers.Select(input =>
            {
                var state = committedControllers.GetController(input.ControllerId);
                return new ControllerInput(
                    input.ControllerId,
                    ControllerMode.Manual,
                    input.Setpoint,
                    state.LastOutput);
            }));

    private static ReactorPrimaryControlInputs SetReactorPowerAutomatic(
        ReactorPrimaryControlInputs inputs,
        ControllerSystemState committedControllers,
        double setpointWatts)
    {
        var loop = inputs.Definition.Loops.Single(static item => item.Kind == ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation);
        return new ReactorPrimaryControlInputs(
            inputs.Definition,
            ReplaceControllerWithAutomatic(inputs.Controllers, committedControllers, loop.ControllerId, setpointWatts),
            inputs.NonRodReactivity);
    }

    private static TurbineSecondaryControlInputs SetTurbineSpeedAutomatic(
        TurbineSecondaryControlInputs inputs,
        ControllerSystemState committedControllers,
        double setpointRpm)
    {
        var loop = inputs.Definition.Loops.Single(static item => item.Kind == TurbineSecondaryControlLoopKind.TurbineSpeedAdmission);
        return new TurbineSecondaryControlInputs(
            inputs.Definition,
            ReplaceControllerWithAutomatic(inputs.Controllers, committedControllers, loop.ControllerId, setpointRpm));
    }

    private static ControllerInputs ReplaceControllerWithAutomatic(
        ControllerInputs inputs,
        ControllerSystemState committedControllers,
        string controllerId,
        double setpoint)
        => new(
            inputs.Definition,
            inputs.Controllers.Select(input =>
            {
                if (!string.Equals(input.ControllerId, controllerId, StringComparison.Ordinal))
                {
                    return input;
                }

                var state = committedControllers.GetController(controllerId);
                return new ControllerInput(
                    input.ControllerId,
                    ControllerMode.Automatic,
                    setpoint,
                    state.LastOutput);
            }));

    private static bool TryValidateRequiredMeasurements(
        SupervisoryOperatingObjective objective,
        MeasuredSignalFrame measuredSignals,
        ReactorPrimaryControlSystemDefinition reactorDefinition,
        TurbineSecondaryControlSystemDefinition secondaryDefinition,
        out string? failureReason)
    {
        var requiredControllerIds = new List<(ControlSystemDefinition ControlSystem, string ControllerId)>();
        if (objective.Kind is SupervisoryOperatingObjectiveKind.HoldReactorPower or SupervisoryOperatingObjectiveKind.HoldOperatingPoint)
        {
            var loop = reactorDefinition.Loops.Single(static item => item.Kind == ReactorPrimaryControlLoopKind.ReactorPowerRodRegulation);
            requiredControllerIds.Add((reactorDefinition.ActuatorSystem.ControlSystem, loop.ControllerId));
        }
        if (objective.Kind is SupervisoryOperatingObjectiveKind.HoldTurbineSpeed or SupervisoryOperatingObjectiveKind.HoldOperatingPoint)
        {
            var loop = secondaryDefinition.Loops.Single(static item => item.Kind == TurbineSecondaryControlLoopKind.TurbineSpeedAdmission);
            requiredControllerIds.Add((secondaryDefinition.ActuatorSystem.ControlSystem, loop.ControllerId));
        }

        foreach (var (controlSystem, controllerId) in requiredControllerIds)
        {
            var controller = controlSystem.GetController(controllerId);
            var measurement = measuredSignals.GetSignal(controller.MeasurementChannelId);
            if (measurement.Validity != SignalValidity.Valid || !measurement.EngineeringValue.HasValue || !double.IsFinite(measurement.EngineeringValue.Value))
            {
                failureReason = $"Required measured signal '{controller.MeasurementChannelId}' for controller '{controllerId}' is unavailable or invalid.";
                return false;
            }
        }

        failureReason = null;
        return true;
    }

    private static SupervisoryOperationState NextState(
        SupervisoryOperationState committed,
        PlantControlAuthorityMode requested,
        PlantControlAuthorityMode effective,
        PlantControlAuthorityHealth health,
        string? reason,
        SupervisoryOperatingObjective? objective)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var changed = committed.RequestedAuthority != requested
            || committed.EffectiveAuthority != effective
            || committed.Health != health
            || !string.Equals(committed.DegradationReason, normalizedReason, StringComparison.Ordinal)
            || committed.Objective != objective;
        return new SupervisoryOperationState(
            requested,
            effective,
            health,
            normalizedReason,
            objective,
            changed ? checked(committed.TransitionSequence + 1) : committed.TransitionSequence);
    }

    private static string BuildProtectionSuspensionReason(ProtectionSystemSnapshot protection)
    {
        var active = new List<string>();
        if (protection.ReactorScramActive)
        {
            active.Add("reactor SCRAM");
        }
        if (protection.TurbineTripActive)
        {
            active.Add("turbine trip");
        }
        if (protection.GeneratorTripActive)
        {
            active.Add("generator trip");
        }
        return $"Supervisory decisions suspended by canonical protection: {string.Join(", ", active)}.";
    }

    private static void ValidateCanonicalBindings(
        MeasuredSignalFrame measuredSignals,
        ReactorPrimaryControlState reactorState,
        TurbineSecondaryControlState secondaryState,
        ReactorPrimaryControlInputs reactorInputs,
        TurbineSecondaryControlInputs secondaryInputs)
    {
        if (!ReferenceEquals(reactorState.Definition, reactorInputs.Definition)
            || !ReferenceEquals(secondaryState.Definition, secondaryInputs.Definition))
        {
            throw new ArgumentException("Supervisory state and controller inputs must use the same canonical M5 definitions.");
        }

        var instrumentation = reactorInputs.Definition.ActuatorSystem.ControlSystem.Instrumentation;
        if (!ReferenceEquals(measuredSignals.Definition, instrumentation)
            || !ReferenceEquals(secondaryInputs.Definition.ActuatorSystem.ControlSystem.Instrumentation, instrumentation))
        {
            throw new ArgumentException("Supervisory operation requires one canonical measured-signal definition across M5 control domains.");
        }
    }
}
