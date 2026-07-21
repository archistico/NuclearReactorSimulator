using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

/// <summary>
/// Headless deterministic M5.7 gate runner. It executes explicit immutable phases for reference hold, setpoint changes,
/// disturbances and protection-matrix cases without trimming state or scheduling hidden outcomes.
/// </summary>
public sealed class AutomaticOperationVerificationRunner
{
    private readonly IntegratedAutomaticOperationSolver _solver;

    public AutomaticOperationVerificationRunner(
        IntegratedAutomaticOperationState canonicalState,
        IFluidThermodynamicModel thermodynamicModel,
        InstrumentSignalSourceCatalog signalSources)
    {
        ArgumentNullException.ThrowIfNull(canonicalState);
        _solver = new IntegratedAutomaticOperationSolver(
            canonicalState.ReactorPrimaryControlState.Definition,
            canonicalState.TurbineSecondaryControlState.Definition,
            canonicalState.ProtectionState.Definition,
            canonicalState.AlarmState.Definition,
            thermodynamicModel,
            signalSources);
    }

    public AutomaticOperationVerificationResult Run(AutomaticOperationVerificationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var state = plan.InitialState;
        var phaseResults = new List<AutomaticOperationVerificationPhaseResult>(plan.Phases.Count);
        var totalSteps = 0;
        var maxMassClosure = 0d;
        var maxEnergyClosure = 0d;
        var maxInvalidSignals = 0;
        var maxUnacknowledgedAlarms = 0;

        foreach (var phase in plan.Phases)
        {
            IntegratedAutomaticOperationStepResult? finalStep = null;
            for (var index = 0; index < phase.StepCount; index++)
            {
                finalStep = _solver.Step(state, phase.Inputs, plan.StepSize);
                state = finalStep.CandidateState;
                totalSteps++;

                var heatBalance = finalStep.Snapshot.Control.ProtectedControl.FullPlant.HeatBalance;
                maxMassClosure = Math.Max(maxMassClosure, Math.Abs(heatBalance.MassClosureResidualKilograms));
                maxEnergyClosure = Math.Max(maxEnergyClosure, Math.Abs(heatBalance.FullEnergyPathClosureResidualJoules));
                maxInvalidSignals = Math.Max(
                    maxInvalidSignals,
                    finalStep.Snapshot.NextMeasuredSignals.Signals.Count(static signal => signal.Validity != SignalValidity.Valid));
                maxUnacknowledgedAlarms = Math.Max(
                    maxUnacknowledgedAlarms,
                    finalStep.Snapshot.Control.Alarms.UnacknowledgedCount);
            }

            var final = finalStep ?? throw new InvalidOperationException("Verification phase did not execute any steps.");
            var protection = final.Snapshot.Control.ProtectedControl.Protection;
            var protectionSatisfied = protection.LatchedActions == phase.ExpectedLatchedProtectionActions
                && protection.ActiveInterlocks == phase.ExpectedActiveInterlocks;
            var tracking = phase.TrackingTargets.Select(target => EvaluateTracking(target, state.MeasuredSignals)).ToArray();
            phaseResults.Add(new AutomaticOperationVerificationPhaseResult(
                phase.Id,
                phase.StepCount,
                final,
                tracking,
                phase.ExpectedLatchedProtectionActions,
                phase.ExpectedActiveInterlocks,
                protectionSatisfied));
        }

        var criteria = plan.Criteria;
        var criteriaSatisfied = maxMassClosure <= criteria.MaximumAbsoluteMassClosureResidualKilograms
            && maxEnergyClosure <= criteria.MaximumAbsoluteFullEnergyPathClosureResidualJoules
            && maxInvalidSignals <= criteria.MaximumInvalidMeasuredSignalCount
            && maxUnacknowledgedAlarms <= criteria.MaximumUnacknowledgedAlarmCount;

        return new AutomaticOperationVerificationResult(
            plan.Id,
            totalSteps,
            TimeSpan.FromTicks(checked(plan.StepSize.Ticks * (long)totalSteps)),
            state,
            phaseResults,
            maxMassClosure,
            maxEnergyClosure,
            maxInvalidSignals,
            maxUnacknowledgedAlarms,
            criteria,
            criteriaSatisfied);
    }

    private static AutomaticOperationTrackingResult EvaluateTracking(
        AutomaticOperationTrackingTarget target,
        MeasuredSignalFrame measuredSignals)
    {
        var signal = measuredSignals.GetSignal(target.ChannelId);
        double? error = signal.EngineeringValue.HasValue
            ? Math.Abs(signal.EngineeringValue.Value - target.TargetEngineeringValue)
            : null;
        return new AutomaticOperationTrackingResult(
            target.ChannelId,
            target.TargetEngineeringValue,
            signal.EngineeringValue,
            error,
            target.MaximumAbsoluteFinalError,
            signal.Validity == SignalValidity.Valid
                && error.HasValue
                && error.Value <= target.MaximumAbsoluteFinalError);
    }
}
