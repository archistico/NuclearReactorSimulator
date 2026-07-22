using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.Scenarios.Analysis;

/// <summary>
/// Immutable M9.2 post-incident evidence report. It contains observed facts and temporal derivations only, never inferred
/// physical causality or a second authoritative plant state.
/// </summary>
public sealed class PostIncidentAnalysisReport
{
    public const int CurrentSchemaVersion = 1;

    public PostIncidentAnalysisReport(
        string scenarioId,
        InitialConditionReference initialCondition,
        int schemaVersion,
        long anchorEventSequence,
        long anchorLogicalStep,
        PostIncidentAnalysisAnchorKind anchorKind,
        string anchorSourceId,
        string anchorDetail,
        long windowStartLogicalStep,
        long windowEndLogicalStep,
        IEnumerable<PostIncidentAnalysisTimelineEntry> timeline,
        IEnumerable<PostIncidentTrendSample> trends,
        PostIncidentStateSummary windowStartState,
        PostIncidentStateSummary anchorState,
        PostIncidentStateSummary windowEndState,
        PostIncidentResponseMetrics metrics,
        string? precedingCheckpointId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        InitialCondition = initialCondition ?? throw new ArgumentNullException(nameof(initialCondition));
        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }
        if (anchorEventSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(anchorEventSequence));
        }
        if (anchorLogicalStep < 0 || windowStartLogicalStep < 0 || windowEndLogicalStep < windowStartLogicalStep)
        {
            throw new ArgumentOutOfRangeException(nameof(anchorLogicalStep));
        }
        if (anchorLogicalStep < windowStartLogicalStep || anchorLogicalStep > windowEndLogicalStep)
        {
            throw new ArgumentException("Anchor logical step must lie inside the analysis window.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(anchorSourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(anchorDetail);

        var timelineArray = (timeline ?? throw new ArgumentNullException(nameof(timeline)))
            .OrderBy(static item => item.Sequence)
            .ToArray();
        for (var index = 0; index < timelineArray.Length; index++)
        {
            var item = timelineArray[index];
            if (item.LogicalStep < windowStartLogicalStep || item.LogicalStep > windowEndLogicalStep)
            {
                throw new ArgumentException("Timeline entries must lie inside the selected analysis window.", nameof(timeline));
            }
            if (item.RelativeLogicalStep != item.LogicalStep - anchorLogicalStep)
            {
                throw new ArgumentException("Timeline relative logical steps must be derived from the selected anchor.", nameof(timeline));
            }
            if (index > 0 && item.Sequence <= timelineArray[index - 1].Sequence)
            {
                throw new ArgumentException("Timeline event sequences must be strictly increasing.", nameof(timeline));
            }
        }

        var anchorTimelineEntry = timelineArray.SingleOrDefault(item => item.Sequence == anchorEventSequence)
            ?? throw new ArgumentException("Timeline must contain the selected anchor event.", nameof(timeline));
        if (anchorTimelineEntry.Relation != PostIncidentTemporalRelation.Anchor
            || anchorTimelineEntry.LogicalStep != anchorLogicalStep
            || MapAnchorKind(anchorTimelineEntry.Kind) != anchorKind
            || !string.Equals(anchorTimelineEntry.SourceId, anchorSourceId.Trim(), StringComparison.Ordinal)
            || !string.Equals(anchorTimelineEntry.Detail, anchorDetail.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("Timeline anchor entry is inconsistent with the report anchor.", nameof(timeline));
        }

        ScenarioId = scenarioId.Trim();
        SchemaVersion = schemaVersion;
        AnchorEventSequence = anchorEventSequence;
        AnchorLogicalStep = anchorLogicalStep;
        AnchorKind = anchorKind;
        AnchorSourceId = anchorSourceId.Trim();
        AnchorDetail = anchorDetail.Trim();
        WindowStartLogicalStep = windowStartLogicalStep;
        WindowEndLogicalStep = windowEndLogicalStep;
        var trendArray = (trends ?? throw new ArgumentNullException(nameof(trends)))
            .OrderBy(static item => item.LogicalStep)
            .ToArray();
        if (trendArray.Length == 0)
        {
            throw new ArgumentException("Post-incident analysis must contain at least one synchronized trend sample.", nameof(trends));
        }
        if (trendArray[0].LogicalStep != windowStartLogicalStep || trendArray[^1].LogicalStep != windowEndLogicalStep)
        {
            throw new ArgumentException("Trend samples must cover the complete selected analysis window.", nameof(trends));
        }
        for (var index = 0; index < trendArray.Length; index++)
        {
            if (trendArray[index].RelativeLogicalStep != trendArray[index].LogicalStep - anchorLogicalStep)
            {
                throw new ArgumentException("Trend sample relative logical steps must be derived from the selected anchor.", nameof(trends));
            }
            if (index > 0 && trendArray[index].LogicalStep != checked(trendArray[index - 1].LogicalStep + 1))
            {
                throw new ArgumentException("Trend samples must cover contiguous logical steps.", nameof(trends));
            }
        }

        Timeline = Array.AsReadOnly(timelineArray);
        Trends = Array.AsReadOnly(trendArray);
        WindowStartState = windowStartState ?? throw new ArgumentNullException(nameof(windowStartState));
        AnchorState = anchorState ?? throw new ArgumentNullException(nameof(anchorState));
        WindowEndState = windowEndState ?? throw new ArgumentNullException(nameof(windowEndState));
        if (WindowStartState.LogicalStep != windowStartLogicalStep
            || AnchorState.LogicalStep != anchorLogicalStep
            || WindowEndState.LogicalStep != windowEndLogicalStep)
        {
            throw new ArgumentException("State summaries must match the selected analysis window and anchor logical steps.");
        }
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        PrecedingCheckpointId = string.IsNullOrWhiteSpace(precedingCheckpointId) ? null : precedingCheckpointId.Trim();
    }

    public string ScenarioId { get; }
    public InitialConditionReference InitialCondition { get; }
    public int SchemaVersion { get; }
    public long AnchorEventSequence { get; }
    public long AnchorLogicalStep { get; }
    public PostIncidentAnalysisAnchorKind AnchorKind { get; }
    public string AnchorSourceId { get; }
    public string AnchorDetail { get; }
    public long WindowStartLogicalStep { get; }
    public long WindowEndLogicalStep { get; }
    public IReadOnlyList<PostIncidentAnalysisTimelineEntry> Timeline { get; }
    public IReadOnlyList<PostIncidentTrendSample> Trends { get; }
    public PostIncidentStateSummary WindowStartState { get; }
    public PostIncidentStateSummary AnchorState { get; }
    public PostIncidentStateSummary WindowEndState { get; }
    public PostIncidentResponseMetrics Metrics { get; }
    public string? PrecedingCheckpointId { get; }

    private static PostIncidentAnalysisAnchorKind MapAnchorKind(ScenarioRecordingEventKind kind)
        => kind switch
        {
            ScenarioRecordingEventKind.FaultTransition => PostIncidentAnalysisAnchorKind.FaultTransition,
            ScenarioRecordingEventKind.ProtectionTransition => PostIncidentAnalysisAnchorKind.ProtectionTransition,
            ScenarioRecordingEventKind.Alarm => PostIncidentAnalysisAnchorKind.Alarm,
            ScenarioRecordingEventKind.OperatorAction => PostIncidentAnalysisAnchorKind.OperatorAction,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported recorder event kind."),
        };
}
