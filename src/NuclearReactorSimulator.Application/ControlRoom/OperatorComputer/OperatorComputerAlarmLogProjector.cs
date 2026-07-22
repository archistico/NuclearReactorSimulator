using System.Globalization;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public static class OperatorComputerAlarmLogProjector
{
    public static OperatorComputerAlarmSnapshot ProjectAlarms(
        ControlRoomSnapshot snapshot,
        ControlRoomOperationalHistorySnapshot? history = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var alarms = snapshot.AlarmEvents.Alarms
            .Where(static alarm => alarm.IsAnnunciated)
            .OrderByDescending(static alarm => alarm.IsFirstOut)
            .ThenBy(static alarm => alarm.ActivationSequence ?? long.MaxValue)
            .ThenBy(static alarm => alarm.AlarmId, StringComparer.Ordinal)
            .Select(static alarm => new OperatorComputerAlarmItemSnapshot(
                alarm.AlarmId,
                alarm.Title,
                alarm.SeverityText,
                alarm.AnnunciatorText,
                alarm.IsFirstOut,
                alarm.CanAcknowledge,
                alarm.CanReset));

        var events = (history?.Events ?? snapshot.AlarmEvents.Events)
            .OrderByDescending(static item => item.Sequence)
            .Take(100)
            .Select(MapAlarmEvent);

        return new OperatorComputerAlarmSnapshot(
            alarms,
            events,
            snapshot.AlarmEvents.AnnunciatedCount,
            snapshot.AlarmEvents.UnacknowledgedCount,
            snapshot.AlarmEvents.FirstOutCount);
    }

    public static OperatorComputerLogSnapshot ProjectLog(
        ControlRoomOperationalHistorySnapshot history,
        IEnumerable<ScenarioRecordingEvent>? sessionEvents = null,
        PostIncidentAnalysisReport? incident = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        var trends = history.TrendSeries.Select(static series => new OperatorComputerTrendSummarySnapshot(
            series.SourceId,
            series.Title,
            series.Unit,
            series.Provenance,
            series.CurrentText,
            series.MinimumText,
            series.MaximumText,
            series.SparklineText,
            series.Points.Count));
        var liveEvents = history.Events.OrderByDescending(static item => item.Sequence).Take(100).Select(MapAlarmEvent);
        var recorded = (sessionEvents ?? Array.Empty<ScenarioRecordingEvent>())
            .OrderByDescending(static item => item.Sequence)
            .Take(100)
            .Select(static item => new OperatorComputerSessionEventSnapshot(
                item.Sequence,
                item.LogicalStep,
                item.Kind.ToString().ToUpperInvariant(),
                item.SourceId,
                item.Detail));

        return new OperatorComputerLogSnapshot(trends, liveEvents, recorded, sessionEvents is not null, incident is null ? null : ProjectIncident(incident));
    }

    private static OperatorComputerAlarmEventSnapshot MapAlarmEvent(ControlRoomAlarmEventPresentationSnapshot item)
        => new(item.Sequence, item.LogicalStep, item.AlarmId, item.AlarmTitle, item.KindText);

    private static OperatorComputerIncidentSnapshot ProjectIncident(PostIncidentAnalysisReport report)
    {
        var timeline = report.Timeline.Select(static item => new OperatorComputerIncidentTimelineSnapshot(
            item.Sequence,
            item.LogicalStep,
            item.RelativeLogicalStep,
            item.Relation.ToString().ToUpperInvariant(),
            item.Kind.ToString().ToUpperInvariant(),
            item.SourceId,
            item.Detail));
        var metrics = report.Metrics;
        var metricLines = new[]
        {
            $"FIRST ALARM LATENCY           {FormatLatency(metrics.FirstAlarmLatencySteps)}",
            $"FIRST PROTECTION LATENCY      {FormatLatency(metrics.FirstProtectionActivationLatencySteps)}",
            $"FIRST OPERATOR ACTION LATENCY {FormatLatency(metrics.FirstOperatorActionLatencySteps)}",
            $"FIRST FAULT CLEAR LATENCY     {FormatLatency(metrics.FirstFaultClearLatencySteps)}",
            $"PEAK INVALID SIGNALS          {metrics.PeakInvalidMeasuredSignalCount.ToString(CultureInfo.InvariantCulture)}",
            $"PEAK ANNUNCIATED ALARMS       {metrics.PeakAnnunciatedAlarmCount.ToString(CultureInfo.InvariantCulture)}",
            $"PEAK UNACKNOWLEDGED ALARMS     {metrics.PeakUnacknowledgedAlarmCount.ToString(CultureInfo.InvariantCulture)}",
            $"PEAK ACTIVE FAULTS             {metrics.PeakActiveFaultCount.ToString(CultureInfo.InvariantCulture)}",
        };
        return new OperatorComputerIncidentSnapshot(
            $"STEP {report.AnchorLogicalStep:D8} · {report.AnchorKind.ToString().ToUpperInvariant()} · {report.AnchorSourceId} · {report.AnchorDetail}",
            timeline,
            metricLines,
            report.PrecedingCheckpointId);
    }

    private static string FormatLatency(long? value)
        => value.HasValue ? $"{value.Value.ToString(CultureInfo.InvariantCulture)} steps" : "NOT OBSERVED IN WINDOW";
}
