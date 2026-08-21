using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;

/// <summary>
/// M10.9.6.5 deterministic reconstruction of lifecycle, demand and score from the canonical M9.1/M10.7 recording model.
/// No challenge-specific opaque checkpoint blob is introduced.
/// </summary>
public static class OperationalChallengeRecordingProjector
{
    public static OperationalChallengeReplayProjection Project(
        OperationalChallengePackDefinition pack,
        ScenarioRecording recording,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.Hidden,
        PlantControlAuthorityMode authorityMode = PlantControlAuthorityMode.Manual)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(recording);
        if (!string.Equals(pack.Scenario.ScenarioId, recording.ScenarioId, StringComparison.Ordinal)
            || pack.Scenario.InitialCondition != recording.InitialCondition)
        {
            throw new InvalidOperationException("Challenge pack scenario/initial-condition identity does not match the recording.");
        }
        foreach (var frame in recording.Frames)
        {
            var actualFingerprint = ControlRoomSnapshotFingerprint.Compute(frame.Snapshot);
            if (!string.Equals(actualFingerprint, frame.SnapshotFingerprint, StringComparison.Ordinal))
            {
                throw new ScenarioReplayDivergenceException(frame.LogicalStep, frame.SnapshotFingerprint, actualFingerprint);
            }
        }

        var first = recording.Frames[0];
        var evidence = new RecordedChallengeEvidenceSource(first.Snapshot);
        using var tracker = ScenarioChallengeTracker.AttachDeterministicEvidence(
            pack.Scenario,
            evidence,
            pack.Challenge,
            pack.ConditionEvaluator);

        var actionsByStep = recording.OperatorActions
            .GroupBy(static action => action.LogicalStep)
            .ToDictionary(static group => group.Key, static group => group.OrderBy(static item => item.Sequence).ToArray());
        var frames = new List<OperationalChallengeReplayFrameEvidence>(recording.Frames.Count);
        var demand = new List<ExternalEnergyDemandEvidenceSnapshot>(recording.Frames.Count);

        for (var index = 0; index < recording.Frames.Count; index++)
        {
            var frame = recording.Frames[index];
            if (index > 0)
            {
                evidence.Advance(frame.Snapshot);
            }

            if (actionsByStep.TryGetValue(frame.LogicalStep, out var acceptedAtStep))
            {
                foreach (var action in acceptedAtStep)
                {
                    evidence.Accept(action);
                }
            }

            var lifecycle = ChallengeLifecycleLogicalStepAlignment.Align(tracker.Snapshot, frame.LogicalStep);
            var demandEvidence = ScenarioChallengeExternalDemandProjector.Project(pack.Challenge, lifecycle, frame.Snapshot);
            demand.Add(demandEvidence);
            frames.Add(new OperationalChallengeReplayFrameEvidence(
                frame.LogicalStep,
                frame.SnapshotFingerprint,
                lifecycle.State,
                lifecycle.ActivatedLogicalStep,
                lifecycle.TerminalLogicalStep,
                demandEvidence));
        }

        var finalLifecycle = ChallengeLifecycleLogicalStepAlignment.Align(tracker.Snapshot, recording.FinalLogicalStep);
        var scoreEvidence = OperationalChallengeScoreEvidenceProjector.Project(
            pack,
            recording,
            finalLifecycle,
            Array.AsReadOnly(demand.ToArray()));
        var score = ChallengeScoreCalculator.Evaluate(
            pack.Challenge,
            pack.ScoringPolicy,
            guidanceMode,
            authorityMode,
            scoreEvidence);
        var frameEvidence = Array.AsReadOnly(frames.ToArray());
        var fingerprint = OperationalChallengeReplayFingerprint.Compute(pack.ExactId, finalLifecycle, frameEvidence, score);

        return new OperationalChallengeReplayProjection(
            pack.ExactId,
            recording.ScenarioId,
            recording.InitialLogicalStep,
            recording.FinalLogicalStep,
            finalLifecycle,
            frameEvidence,
            score,
            fingerprint);
    }


}
