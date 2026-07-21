using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

/// <summary>
/// Deterministic M5.5 protection logic over measured signals only. It evaluates latching trips, non-latching interlocks and reset permissives.
/// Alarm/annunciator presentation is intentionally outside this solver.
/// </summary>
public sealed class ProtectionSystemSolver
{
    private readonly ProtectionSystemDefinition _definition;

    public ProtectionSystemSolver(ProtectionSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public ProtectionSystemStepResult Step(
        MeasuredSignalFrame measuredSignals,
        ProtectionSystemState committedState,
        ProtectionSystemInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(measuredSignals);
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(inputs);
        if (!ReferenceEquals(measuredSignals.Definition, _definition.Instrumentation))
        {
            throw new ArgumentException("Measured signals do not use the protection system's canonical instrumentation definition.", nameof(measuredSignals));
        }
        if (!ReferenceEquals(committedState.Definition, _definition))
        {
            throw new ArgumentException("Committed protection state does not use this solver's canonical definition.", nameof(committedState));
        }
        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Protection inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        var functionWork = _definition.TripFunctions.Select(function =>
        {
            var signal = measuredSignals.GetSignal(function.MeasurementChannelId);
            var trigger = IsInvalid(signal)
                ? function.TripOnInvalidMeasurement
                : Compare(signal.EngineeringValue!.Value, function.Comparison, function.TripThreshold);
            var resetSafe = !IsInvalid(signal) && IsResetSafe(signal.EngineeringValue!.Value, function);
            var wasLatched = committedState.IsFunctionLatched(function.Id);
            return new FunctionWork(function, signal, trigger, resetSafe, wasLatched, wasLatched || trigger);
        }).ToArray();

        var permissives = _definition.ResetPermissives.Select(definition =>
        {
            var signal = measuredSignals.GetSignal(definition.MeasurementChannelId);
            var satisfied = !IsInvalid(signal) && Compare(signal.EngineeringValue!.Value, definition.Comparison, definition.Threshold);
            return new ProtectionPermissiveSnapshot(
                definition.Id,
                definition.MeasurementChannelId,
                signal.EngineeringValue,
                signal.Validity,
                signal.Quality,
                satisfied);
        }).ToArray();

        var manualTripRequested = inputs.ManualReactorScram || inputs.ManualTurbineTrip || inputs.ManualGeneratorTrip;
        var resetAccepted = inputs.ResetRequested
            && !manualTripRequested
            && functionWork.All(static item => !item.TriggerActive && item.ResetConditionSafe)
            && permissives.All(static item => item.IsSatisfied);

        var manualActions = committedState.ManualLatchedActions;
        if (inputs.ManualReactorScram)
        {
            manualActions |= ProtectionAction.ReactorScram;
        }
        if (inputs.ManualTurbineTrip)
        {
            manualActions |= ProtectionAction.TurbineTrip;
        }
        if (inputs.ManualGeneratorTrip)
        {
            manualActions |= ProtectionAction.GeneratorTrip;
        }
        if (resetAccepted)
        {
            manualActions = ProtectionAction.None;
        }

        var candidateLatches = functionWork.Select(item => new ProtectionFunctionLatchState(
            item.Definition.Id,
            resetAccepted ? false : item.CandidateLatched)).ToArray();
        var candidateState = new ProtectionSystemState(_definition, candidateLatches, manualActions);

        var latchedActions = manualActions;
        foreach (var function in functionWork)
        {
            var isLatched = candidateState.IsFunctionLatched(function.Definition.Id);
            if (isLatched)
            {
                latchedActions |= function.Definition.Actions;
            }
        }

        var functionSnapshots = functionWork.Select(item => new ProtectionFunctionSnapshot(
            item.Definition.Id,
            item.Definition.MeasurementChannelId,
            item.Signal.EngineeringValue,
            item.Signal.Validity,
            item.Signal.Quality,
            item.TriggerActive,
            item.ResetConditionSafe,
            item.WasLatched,
            candidateState.IsFunctionLatched(item.Definition.Id),
            item.Definition.Actions)).ToArray();

        var interlockSnapshots = _definition.Interlocks.Select(definition =>
        {
            var signal = measuredSignals.GetSignal(definition.MeasurementChannelId);
            var active = IsInvalid(signal)
                ? definition.BlockOnInvalidMeasurement
                : Compare(signal.EngineeringValue!.Value, definition.Comparison, definition.Threshold);
            return new ProtectionInterlockSnapshot(
                definition.Id,
                definition.MeasurementChannelId,
                signal.EngineeringValue,
                signal.Validity,
                signal.Quality,
                active,
                definition.Actions);
        }).ToArray();
        var activeInterlocks = interlockSnapshots.Where(static item => item.IsActive)
            .Aggregate(ProtectionInterlockAction.None, static (current, item) => current | item.Actions);

        var snapshot = new ProtectionSystemSnapshot(
            _definition,
            functionSnapshots,
            interlockSnapshots,
            permissives,
            latchedActions,
            activeInterlocks,
            inputs.ResetRequested,
            resetAccepted);
        return new ProtectionSystemStepResult(candidateState, snapshot);
    }

    private static bool IsInvalid(MeasuredSignal signal)
        => signal.Validity != SignalValidity.Valid || !signal.EngineeringValue.HasValue;

    private static bool Compare(double value, ProtectionComparison comparison, double threshold)
        => comparison == ProtectionComparison.High ? value >= threshold : value <= threshold;

    private static bool IsResetSafe(double value, ProtectionFunctionDefinition definition)
        => definition.Comparison == ProtectionComparison.High
            ? value <= definition.ResetThreshold
            : value >= definition.ResetThreshold;

    private sealed record FunctionWork(
        ProtectionFunctionDefinition Definition,
        MeasuredSignal Signal,
        bool TriggerActive,
        bool ResetConditionSafe,
        bool WasLatched,
        bool CandidateLatched);
}
