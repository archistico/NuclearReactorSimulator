using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.29 deterministic observational counter over already-emitted H.20/H.22 numerical telemetry. It has no authority over
/// state commitment, rollback, protection or operator presentation and therefore cannot weaken the existing fail-closed path.
/// </summary>
public sealed class FourNodeProductionActivationTelemetryCounter
{
    private readonly Dictionary<FourNodeBranchContinuityActivationReason, long> _rollbackReasonCounts = new();
    private readonly Dictionary<FourNodeBranchContinuityCorrectedCommitReason, long> _commitReasonCounts = new();
    private long _observedSteps;
    private long _fourNodeTelemetrySteps;
    private long _triggeredSteps;
    private long _candidateEligibleSteps;
    private long _commitAuthorizedSteps;
    private long _correctedCommittedSteps;
    private long _explicitFallbackSteps;
    private long _rollbackSteps;
    private long _fallbackCommitViolations;
    private long _unsafeCommitViolations;
    private long _untargetedBranchDisagreementSteps;

    public void Observe(PlantNetworkHydraulicNumericalSnapshot numerics)
    {
        ArgumentNullException.ThrowIfNull(numerics);
        _observedSteps++;

        var telemetry = numerics.FourNodeBranchContinuity;
        if (telemetry is null)
        {
            return;
        }

        _fourNodeTelemetrySteps++;
        Increment(_commitReasonCounts, telemetry.CorrectedCommitReason);

        if (telemetry.TriggerObserved)
        {
            _triggeredSteps++;
        }

        if (telemetry.ShadowCorrectedCandidateEligible)
        {
            _candidateEligibleSteps++;
        }

        if (telemetry.CorrectedCommitAuthorized)
        {
            _commitAuthorizedSteps++;
        }

        if (telemetry.CorrectedCandidateCommitted)
        {
            _correctedCommittedSteps++;
        }

        if (telemetry.TriggerObserved && !telemetry.CorrectedCandidateCommitted)
        {
            _explicitFallbackSteps++;
        }

        if (telemetry.RollbackRequired)
        {
            _rollbackSteps++;
            Increment(_rollbackReasonCounts, telemetry.Reason);
        }

        if (telemetry.UntargetedBranchDisagreementDetected)
        {
            _untargetedBranchDisagreementSteps++;
        }

        if (telemetry.RollbackRequired && telemetry.CorrectedCandidateCommitted)
        {
            _fallbackCommitViolations++;
        }

        if (telemetry.CorrectedCandidateCommitted
            && (!telemetry.ShadowCorrectedCandidateEligible
                || !telemetry.CorrectedCommitAuthorized
                || telemetry.RollbackRequired
                || telemetry.UntargetedBranchDisagreementDetected))
        {
            _unsafeCommitViolations++;
        }
    }

    public FourNodeProductionActivationTelemetrySnapshot Snapshot()
        => new(
            _observedSteps,
            _fourNodeTelemetrySteps,
            _triggeredSteps,
            _candidateEligibleSteps,
            _commitAuthorizedSteps,
            _correctedCommittedSteps,
            _explicitFallbackSteps,
            _rollbackSteps,
            _fallbackCommitViolations,
            _unsafeCommitViolations,
            _untargetedBranchDisagreementSteps,
            ReadOnlyCopy(_rollbackReasonCounts),
            ReadOnlyCopy(_commitReasonCounts));

    private static void Increment<TEnum>(Dictionary<TEnum, long> counts, TEnum key)
        where TEnum : struct, Enum
        => counts[key] = counts.TryGetValue(key, out var current) ? checked(current + 1) : 1;

    private static IReadOnlyDictionary<TEnum, long> ReadOnlyCopy<TEnum>(Dictionary<TEnum, long> source)
        where TEnum : struct, Enum
        => new ReadOnlyDictionary<TEnum, long>(
            source.OrderBy(static item => item.Key).ToDictionary(static item => item.Key, static item => item.Value));
}
