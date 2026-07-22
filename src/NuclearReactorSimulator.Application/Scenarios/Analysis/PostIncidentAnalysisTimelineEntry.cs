using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.Scenarios.Analysis;

/// <summary>
/// One observed recorder fact placed relative to the selected incident anchor. Temporal adjacency is not asserted as causality.
/// Accepted operator actions retain their typed command payload.
/// </summary>
public sealed record PostIncidentAnalysisTimelineEntry
{
    public PostIncidentAnalysisTimelineEntry(
        long sequence,
        long logicalStep,
        long relativeLogicalStep,
        PostIncidentTemporalRelation relation,
        ScenarioRecordingEventKind kind,
        string sourceId,
        string detail,
        ControlRoomCommand? operatorCommand = null)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        if (kind == ScenarioRecordingEventKind.OperatorAction && operatorCommand is null)
        {
            throw new ArgumentException("Operator-action timeline entries require the accepted typed command.", nameof(operatorCommand));
        }
        if (kind != ScenarioRecordingEventKind.OperatorAction && operatorCommand is not null)
        {
            throw new ArgumentException("Only operator-action timeline entries may carry a typed command.", nameof(operatorCommand));
        }

        Sequence = sequence;
        LogicalStep = logicalStep;
        RelativeLogicalStep = relativeLogicalStep;
        Relation = relation;
        Kind = kind;
        SourceId = sourceId.Trim();
        Detail = detail.Trim();
        OperatorCommand = operatorCommand;
    }

    public long Sequence { get; }
    public long LogicalStep { get; }
    public long RelativeLogicalStep { get; }
    public PostIncidentTemporalRelation Relation { get; }
    public ScenarioRecordingEventKind Kind { get; }
    public string SourceId { get; }
    public string Detail { get; }
    public ControlRoomCommand? OperatorCommand { get; }
}
