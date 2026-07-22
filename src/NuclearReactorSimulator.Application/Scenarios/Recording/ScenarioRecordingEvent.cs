using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>One deterministic recorder event ordered only by session-local logical sequence.</summary>
public sealed record ScenarioRecordingEvent
{
    public ScenarioRecordingEvent(
        long sequence,
        long logicalStep,
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
            throw new ArgumentException("Operator-action recorder events require the accepted typed command.", nameof(operatorCommand));
        }
        if (kind != ScenarioRecordingEventKind.OperatorAction && operatorCommand is not null)
        {
            throw new ArgumentException("Only operator-action recorder events may carry a typed operator command.", nameof(operatorCommand));
        }

        Sequence = sequence;
        LogicalStep = logicalStep;
        Kind = kind;
        SourceId = sourceId.Trim();
        Detail = detail.Trim();
        OperatorCommand = operatorCommand;
    }

    public long Sequence { get; }
    public long LogicalStep { get; }
    public ScenarioRecordingEventKind Kind { get; }
    public string SourceId { get; }
    public string Detail { get; }
    public ControlRoomCommand? OperatorCommand { get; }
}
