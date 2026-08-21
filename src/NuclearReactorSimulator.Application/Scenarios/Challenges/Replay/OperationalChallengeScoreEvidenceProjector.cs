using System.Globalization;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;

/// <summary>
/// M10.9.6.5 authored observational evidence projection for the initial pack. Fractions are derived only from recorded
/// lifecycle/snapshot/demand evidence; the projector cannot mutate plant/controller/protection state.
/// </summary>
public static class OperationalChallengeScoreEvidenceProjector
{
    private static readonly HashSet<string> CriticalSafetyFailureConditionIds = new(StringComparer.Ordinal)
    {
        "prestart:unexpected-trip",
        "sync:unexpected-trip",
        "demand:unexpected-trip",
        "stabilize:unexpected-trip",
    };

    private static readonly HashSet<string> CriticalProcedureFailureConditionIds = new(StringComparer.Ordinal)
    {
        "shutdown:emergency-action-used",
    };

    public static IReadOnlyList<ChallengeScoreDimensionEvidence> Project(
        OperationalChallengePackDefinition pack,
        ScenarioRecording recording,
        ChallengeLifecycleSnapshot lifecycle,
        IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot> demandTimeline)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(recording);
        ArgumentNullException.ThrowIfNull(lifecycle);
        ArgumentNullException.ThrowIfNull(demandTimeline);

        if (!string.Equals(pack.Challenge.ExactId, lifecycle.ChallengeExactId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Challenge pack and lifecycle exact identities must match.", nameof(lifecycle));
        }
        if (recording.FinalLogicalStep != lifecycle.LogicalStep)
        {
            throw new ArgumentException("Score projection requires lifecycle evidence at the recording final logical step.", nameof(lifecycle));
        }
        if (demandTimeline.Count != recording.Frames.Count
            || demandTimeline.Count == 0
            || demandTimeline[0].LogicalStep != recording.InitialLogicalStep
            || demandTimeline[^1].LogicalStep != recording.FinalLogicalStep)
        {
            throw new ArgumentException("Demand evidence must cover the same contiguous recording frame span.", nameof(demandTimeline));
        }

        var unclassifiedFailureIds = pack.Challenge.FailureConditions
            .Select(static condition => condition.ConditionId)
            .Where(id => !CriticalSafetyFailureConditionIds.Contains(id) && !CriticalProcedureFailureConditionIds.Contains(id))
            .ToArray();
        if (unclassifiedFailureIds.Length != 0)
        {
            throw new InvalidOperationException(
                $"Challenge pack '{pack.ExactId}' has failure conditions without an authored M10.9.6.5 score-dominance classification: {string.Join(", ", unclassifiedFailureIds)}.");
        }

        return Array.AsReadOnly(pack.ScoreEvidenceBindings
            .Select(binding => ProjectDimension(pack, lifecycle, demandTimeline, binding))
            .ToArray());
    }

    private static ChallengeScoreDimensionEvidence ProjectDimension(
        OperationalChallengePackDefinition pack,
        ChallengeLifecycleSnapshot lifecycle,
        IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot> demandTimeline,
        OperationalChallengeScoreEvidenceBinding binding)
        => binding.Kind switch
        {
            ChallengeScoreDimensionKind.SafetyProtectionDiscipline => Safety(lifecycle, binding),
            ChallengeScoreDimensionKind.ProcedureRequiredActions => Procedure(pack, lifecycle, binding),
            ChallengeScoreDimensionKind.StabilityOperatingQuality => Stability(pack, lifecycle, binding),
            ChallengeScoreDimensionKind.DemandTracking => Demand(demandTimeline, binding),
            ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency => LogicalTime(lifecycle, binding),
            _ => throw new ArgumentOutOfRangeException(nameof(binding), binding.Kind, "Unsupported challenge score dimension."),
        };

    private static ChallengeScoreDimensionEvidence Safety(
        ChallengeLifecycleSnapshot lifecycle,
        OperationalChallengeScoreEvidenceBinding binding)
    {
        var critical = lifecycle.Observations.Any(observation =>
            observation.IsSatisfied && CriticalSafetyFailureConditionIds.Contains(observation.ConditionId));
        var fraction = critical ? 0m : 1m;
        return Available(
            binding,
            fraction,
            critical
                ? "A challenge-authored critical safety/protection failure condition is satisfied."
                : "No challenge-authored critical safety/protection failure condition is satisfied.",
            critical);
    }

    private static ChallengeScoreDimensionEvidence Procedure(
        OperationalChallengePackDefinition pack,
        ChallengeLifecycleSnapshot lifecycle,
        OperationalChallengeScoreEvidenceBinding binding)
    {
        var critical = lifecycle.Observations.Any(observation =>
            observation.IsSatisfied && CriticalProcedureFailureConditionIds.Contains(observation.ConditionId));
        var relevant = pack.Challenge.RequiredObservations
            .Concat(pack.Challenge.CompletionConditions)
            .Select(static condition => condition.ConditionId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var satisfied = relevant.Count(id => IsSatisfied(lifecycle, id));
        var fraction = relevant.Length == 0 ? 1m : Ratio(satisfied, relevant.Length);
        if (critical)
        {
            fraction = 0m;
        }
        return Available(
            binding,
            fraction,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Authored required/completion conditions satisfied={satisfied}/{relevant.Length}; critical-procedure-failure={critical}."),
            critical);
    }

    private static ChallengeScoreDimensionEvidence Stability(
        OperationalChallengePackDefinition pack,
        ChallengeLifecycleSnapshot lifecycle,
        OperationalChallengeScoreEvidenceBinding binding)
    {
        var completionIds = pack.Challenge.CompletionConditions.Select(static condition => condition.ConditionId).ToArray();
        var observed = completionIds.Count(id => lifecycle.Observations.Any(item => string.Equals(item.ConditionId, id, StringComparison.Ordinal)));
        if (observed == 0)
        {
            return Unavailable(binding, "No authored completion/stability observation has been recorded yet.");
        }
        var satisfied = completionIds.Count(id => IsSatisfied(lifecycle, id));
        return Available(
            binding,
            Ratio(satisfied, completionIds.Length),
            string.Create(CultureInfo.InvariantCulture, $"Authored completion/stability conditions satisfied={satisfied}/{completionIds.Length}."));
    }

    private static ChallengeScoreDimensionEvidence Demand(
        IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot> demandTimeline,
        OperationalChallengeScoreEvidenceBinding binding)
    {
        var samples = demandTimeline
            .Where(static item => item.IsAvailable && item.ExternalDemandMegawatts.HasValue && item.ActualElectricalOutputMegawatts.HasValue)
            .ToArray();
        if (samples.Length == 0)
        {
            return Unavailable(binding, "External-demand/actual-output paired evidence is unavailable.");
        }

        var meanAbsoluteError = samples.Average(static item => Math.Abs(item.DemandOutputErrorMegawatts ?? 0d));
        var meanDemandMagnitude = samples.Average(static item => Math.Abs(item.ExternalDemandMegawatts!.Value));
        if (meanDemandMagnitude <= 0d)
        {
            return Unavailable(binding, "External-demand magnitude is not positive enough to normalize tracking error.");
        }
        var normalizedError = Math.Clamp(meanAbsoluteError / meanDemandMagnitude, 0d, 1d);
        var fraction = (decimal)(1d - normalizedError);
        return Available(
            binding,
            fraction,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Paired demand/output samples={samples.Length}; mean-absolute-error={meanAbsoluteError:0.######} MWe; mean-demand={meanDemandMagnitude:0.######} MWe."));
    }

    private static ChallengeScoreDimensionEvidence LogicalTime(
        ChallengeLifecycleSnapshot lifecycle,
        OperationalChallengeScoreEvidenceBinding binding)
    {
        if (!lifecycle.ActivatedLogicalStep.HasValue)
        {
            return Unavailable(binding, "Challenge has not activated; logical-time efficiency evidence is unavailable.");
        }
        if (!lifecycle.TerminalLogicalStep.HasValue)
        {
            return Unavailable(binding, "Challenge is not terminal; completion-efficiency evidence is not final.");
        }
        if (!lifecycle.TargetWindowStartLogicalStep.HasValue || !lifecycle.TargetWindowEndLogicalStep.HasValue)
        {
            return Available(binding, 1m, "Challenge is terminal and has no authored target completion window.");
        }

        var terminal = lifecycle.TerminalLogicalStep.Value;
        var start = lifecycle.TargetWindowStartLogicalStep.Value;
        var end = lifecycle.TargetWindowEndLogicalStep.Value;
        decimal fraction;
        if (terminal <= end)
        {
            fraction = 1m;
        }
        else
        {
            var width = Math.Max(1L, end - start + 1L);
            var lateness = terminal - end;
            fraction = Math.Max(0m, 1m - ((decimal)lateness / width));
        }
        return Available(
            binding,
            fraction,
            string.Create(CultureInfo.InvariantCulture, $"Terminal-step={terminal}; target-window={start}..{end}."));
    }

    private static bool IsSatisfied(ChallengeLifecycleSnapshot lifecycle, string conditionId)
        => lifecycle.Observations.Any(observation =>
            string.Equals(observation.ConditionId, conditionId, StringComparison.Ordinal) && observation.IsSatisfied);

    private static decimal Ratio(int numerator, int denominator)
        => denominator <= 0 ? 1m : Math.Clamp((decimal)numerator / denominator, 0m, 1m);

    private static ChallengeScoreDimensionEvidence Available(
        OperationalChallengeScoreEvidenceBinding binding,
        decimal fraction,
        string summary,
        bool critical = false)
        => new(binding.Kind, true, fraction, binding.EvidenceSourceId, summary, critical);

    private static ChallengeScoreDimensionEvidence Unavailable(
        OperationalChallengeScoreEvidenceBinding binding,
        string summary)
        => new(binding.Kind, false, null, binding.EvidenceSourceId, summary);
}
