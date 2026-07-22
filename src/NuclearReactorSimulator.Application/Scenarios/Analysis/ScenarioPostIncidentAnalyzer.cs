using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.Scenarios.Analysis;

/// <summary>
/// M9.2 deterministic post-incident analyzer over immutable M9.1 recordings. It reports observed event ordering and temporal
/// response metrics only; it never asserts causal relationships that are not explicitly represented by the source recording.
/// </summary>
public sealed class ScenarioPostIncidentAnalyzer
{
    public PostIncidentAnalysisReport Analyze(
        ScenarioRecording recording,
        PostIncidentAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(recording);
        var requested = options ?? PostIncidentAnalysisOptions.Default;
        var anchor = ResolveAnchor(recording, requested);
        if (anchor.LogicalStep < recording.InitialLogicalStep || anchor.LogicalStep > recording.FinalLogicalStep)
        {
            throw new InvalidOperationException("Selected incident anchor lies outside the recorded fixed-step frame range.");
        }

        var windowStart = Math.Max(recording.InitialLogicalStep, anchor.LogicalStep - requested.PreIncidentSteps);
        var availablePostSteps = recording.FinalLogicalStep - anchor.LogicalStep;
        var windowEnd = anchor.LogicalStep + Math.Min(requested.PostIncidentSteps, availablePostSteps);
        var frames = recording.Frames
            .Where(frame => frame.LogicalStep >= windowStart && frame.LogicalStep <= windowEnd)
            .ToArray();
        if (frames.Length == 0)
        {
            throw new InvalidOperationException("Selected post-incident analysis window contains no recorded frames.");
        }

        var timeline = recording.Events
            .Where(item => item.LogicalStep >= windowStart && item.LogicalStep <= windowEnd)
            .OrderBy(static item => item.Sequence)
            .Select(item => new PostIncidentAnalysisTimelineEntry(
                item.Sequence,
                item.LogicalStep,
                item.LogicalStep - anchor.LogicalStep,
                item.Sequence == anchor.Sequence
                    ? PostIncidentTemporalRelation.Anchor
                    : item.LogicalStep < anchor.LogicalStep
                        || (item.LogicalStep == anchor.LogicalStep && item.Sequence < anchor.Sequence)
                            ? PostIncidentTemporalRelation.BeforeAnchor
                            : PostIncidentTemporalRelation.AfterAnchor,
                item.Kind,
                item.SourceId,
                item.Detail,
                item.OperatorCommand))
            .ToArray();

        var anchorFrame = recording.Frames.Single(frame => frame.LogicalStep == anchor.LogicalStep);
        var precedingCheckpoint = recording.Checkpoints
            .Where(checkpoint => checkpoint.LogicalStep <= anchor.LogicalStep)
            .OrderByDescending(static checkpoint => checkpoint.LogicalStep)
            .ThenBy(static checkpoint => checkpoint.CheckpointId, StringComparer.Ordinal)
            .FirstOrDefault();

        return new PostIncidentAnalysisReport(
            recording.ScenarioId,
            recording.InitialCondition,
            PostIncidentAnalysisReport.CurrentSchemaVersion,
            anchor.Sequence,
            anchor.LogicalStep,
            MapAnchorKind(anchor.Kind),
            anchor.SourceId,
            anchor.Detail,
            windowStart,
            windowEnd,
            timeline,
            frames.Select(frame => new PostIncidentTrendSample(frame.Snapshot, anchor.LogicalStep)),
            new PostIncidentStateSummary(frames[0].Snapshot),
            new PostIncidentStateSummary(anchorFrame.Snapshot),
            new PostIncidentStateSummary(frames[^1].Snapshot),
            BuildMetrics(anchor.LogicalStep, windowEnd, timeline, frames),
            precedingCheckpoint?.CheckpointId);
    }

    private static ScenarioRecordingEvent ResolveAnchor(
        ScenarioRecording recording,
        PostIncidentAnalysisOptions options)
    {
        if (options.AnchorEventSequence is long explicitSequence)
        {
            return recording.Events.SingleOrDefault(item => item.Sequence == explicitSequence)
                ?? throw new ArgumentException(
                    $"Recorder event sequence {explicitSequence} does not exist in the recording.",
                    nameof(options));
        }

        // Prefer initiating/activation evidence. Within each class the recorder monotonic sequence is authoritative.
        // This is deterministic anchor selection only, not a claim of physical causality.
        var candidate = recording.Events
            .Where(static item => item.Kind == ScenarioRecordingEventKind.FaultTransition
                && string.Equals(item.Detail, "Active", StringComparison.Ordinal))
            .OrderBy(static item => item.Sequence)
            .FirstOrDefault();
        candidate ??= recording.Events
            .Where(static item => item.Kind == ScenarioRecordingEventKind.ProtectionTransition
                && string.Equals(item.Detail, "Active", StringComparison.Ordinal))
            .OrderBy(static item => item.Sequence)
            .FirstOrDefault();
        candidate ??= recording.Events
            .Where(static item => item.Kind == ScenarioRecordingEventKind.Alarm
                && item.Detail.StartsWith("Activated:", StringComparison.Ordinal))
            .OrderBy(static item => item.Sequence)
            .FirstOrDefault();
        candidate ??= recording.Events
            .Where(static item => item.Kind == ScenarioRecordingEventKind.OperatorAction)
            .OrderBy(static item => item.Sequence)
            .FirstOrDefault();
        candidate ??= recording.Events
            .OrderBy(static item => item.Sequence)
            .FirstOrDefault();

        return candidate
            ?? throw new InvalidOperationException("Post-incident analysis requires at least one recorded event.");
    }

    private static PostIncidentAnalysisAnchorKind MapAnchorKind(ScenarioRecordingEventKind kind)
        => kind switch
        {
            ScenarioRecordingEventKind.FaultTransition => PostIncidentAnalysisAnchorKind.FaultTransition,
            ScenarioRecordingEventKind.ProtectionTransition => PostIncidentAnalysisAnchorKind.ProtectionTransition,
            ScenarioRecordingEventKind.Alarm => PostIncidentAnalysisAnchorKind.Alarm,
            ScenarioRecordingEventKind.OperatorAction => PostIncidentAnalysisAnchorKind.OperatorAction,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported recorder event kind."),
        };

    private static PostIncidentResponseMetrics BuildMetrics(
        long anchorStep,
        long windowEnd,
        IReadOnlyList<PostIncidentAnalysisTimelineEntry> timeline,
        IReadOnlyList<ScenarioRecordingFrame> frames)
    {
        long? Latency(ScenarioRecordingEventKind kind, Func<PostIncidentAnalysisTimelineEntry, bool>? predicate = null)
        {
            var match = timeline
                .Where(item => item.LogicalStep >= anchorStep
                    && item.LogicalStep <= windowEnd
                    && item.Kind == kind
                    && (predicate is null || predicate(item)))
                .OrderBy(static item => item.Sequence)
                .FirstOrDefault();
            return match is null ? null : match.LogicalStep - anchorStep;
        }

        return new PostIncidentResponseMetrics(
            Latency(
                ScenarioRecordingEventKind.Alarm,
                static item => item.Detail.StartsWith("Activated:", StringComparison.Ordinal)),
            Latency(
                ScenarioRecordingEventKind.ProtectionTransition,
                static item => string.Equals(item.Detail, "Active", StringComparison.Ordinal)),
            Latency(ScenarioRecordingEventKind.OperatorAction),
            Latency(
                ScenarioRecordingEventKind.FaultTransition,
                static item => string.Equals(item.Detail, "Cleared", StringComparison.Ordinal)),
            frames.Max(static frame => frame.Snapshot.InvalidMeasuredSignalCount),
            frames.Max(static frame => frame.Snapshot.AnnunciatedAlarmCount),
            frames.Max(static frame => frame.Snapshot.UnacknowledgedAlarmCount),
            frames.Max(static frame => frame.Snapshot.Faults.ActiveCount));
    }
}
