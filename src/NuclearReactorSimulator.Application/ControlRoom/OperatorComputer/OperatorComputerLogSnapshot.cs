using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerTrendSummarySnapshot(
    string SourceId,
    string Title,
    string Unit,
    string Provenance,
    string Current,
    string Minimum,
    string Maximum,
    string Sparkline,
    int SampleCount);

public sealed record OperatorComputerSessionEventSnapshot(
    long Sequence,
    long LogicalStep,
    string Kind,
    string SourceId,
    string Detail);

public sealed record OperatorComputerIncidentTimelineSnapshot(
    long Sequence,
    long LogicalStep,
    long RelativeLogicalStep,
    string Relation,
    string Kind,
    string SourceId,
    string Detail);

public sealed class OperatorComputerIncidentSnapshot
{
    public OperatorComputerIncidentSnapshot(
        string anchorText,
        IEnumerable<OperatorComputerIncidentTimelineSnapshot> timeline,
        IEnumerable<string> metricLines,
        string? precedingCheckpointId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anchorText);
        AnchorText = anchorText.Trim();
        Timeline = new ReadOnlyCollection<OperatorComputerIncidentTimelineSnapshot>((timeline ?? throw new ArgumentNullException(nameof(timeline))).ToArray());
        MetricLines = new ReadOnlyCollection<string>((metricLines ?? throw new ArgumentNullException(nameof(metricLines))).ToArray());
        PrecedingCheckpointId = string.IsNullOrWhiteSpace(precedingCheckpointId) ? null : precedingCheckpointId.Trim();
    }

    public string AnchorText { get; }
    public IReadOnlyList<OperatorComputerIncidentTimelineSnapshot> Timeline { get; }
    public IReadOnlyList<string> MetricLines { get; }
    public string? PrecedingCheckpointId { get; }
}

public sealed class OperatorComputerLogSnapshot
{
    public OperatorComputerLogSnapshot(
        IEnumerable<OperatorComputerTrendSummarySnapshot> liveTrends,
        IEnumerable<OperatorComputerAlarmEventSnapshot> liveEvents,
        IEnumerable<OperatorComputerSessionEventSnapshot>? sessionEvents = null,
        bool sessionEvidenceAvailable = false,
        OperatorComputerIncidentSnapshot? incident = null)
    {
        LiveTrends = new ReadOnlyCollection<OperatorComputerTrendSummarySnapshot>((liveTrends ?? throw new ArgumentNullException(nameof(liveTrends))).ToArray());
        LiveEvents = new ReadOnlyCollection<OperatorComputerAlarmEventSnapshot>((liveEvents ?? throw new ArgumentNullException(nameof(liveEvents))).ToArray());
        SessionEvents = new ReadOnlyCollection<OperatorComputerSessionEventSnapshot>((sessionEvents ?? Array.Empty<OperatorComputerSessionEventSnapshot>()).ToArray());
        SessionEvidenceAvailable = sessionEvidenceAvailable;
        Incident = incident;
    }

    public IReadOnlyList<OperatorComputerTrendSummarySnapshot> LiveTrends { get; }
    public IReadOnlyList<OperatorComputerAlarmEventSnapshot> LiveEvents { get; }
    public IReadOnlyList<OperatorComputerSessionEventSnapshot> SessionEvents { get; }
    public bool SessionEvidenceAvailable { get; }
    public OperatorComputerIncidentSnapshot? Incident { get; }
}
