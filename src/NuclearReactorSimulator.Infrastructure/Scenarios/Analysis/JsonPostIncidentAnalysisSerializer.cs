using System.Text.Json;
using System.Text.Json.Serialization;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Infrastructure.Scenarios.Analysis;

public sealed class JsonPostIncidentAnalysisSerializer : IPostIncidentAnalysisSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public string Serialize(PostIncidentAnalysisReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (report.SchemaVersion != PostIncidentAnalysisReport.CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Post-incident analysis schema version {report.SchemaVersion} is not supported.");
        }

        return JsonSerializer.Serialize(new ReportDocument
        {
            SchemaVersion = report.SchemaVersion,
            ScenarioId = report.ScenarioId,
            InitialConditionId = report.InitialCondition.InitialConditionId,
            InitialConditionVersion = report.InitialCondition.Version,
            AnchorEventSequence = report.AnchorEventSequence,
            AnchorLogicalStep = report.AnchorLogicalStep,
            AnchorKind = report.AnchorKind,
            AnchorSourceId = report.AnchorSourceId,
            AnchorDetail = report.AnchorDetail,
            WindowStartLogicalStep = report.WindowStartLogicalStep,
            WindowEndLogicalStep = report.WindowEndLogicalStep,
            Timeline = report.Timeline.Select(static item => new TimelineDocument
            {
                Sequence = item.Sequence,
                LogicalStep = item.LogicalStep,
                RelativeLogicalStep = item.RelativeLogicalStep,
                Relation = item.Relation,
                Kind = item.Kind,
                SourceId = item.SourceId,
                Detail = item.Detail,
                OperatorCommand = item.OperatorCommand,
            }).ToArray(),
            Trends = report.Trends.Select(ToDocument).ToArray(),
            WindowStartState = ToDocument(report.WindowStartState),
            AnchorState = ToDocument(report.AnchorState),
            WindowEndState = ToDocument(report.WindowEndState),
            Metrics = new MetricsDocument
            {
                FirstAlarmLatencySteps = report.Metrics.FirstAlarmLatencySteps,
                FirstProtectionActivationLatencySteps = report.Metrics.FirstProtectionActivationLatencySteps,
                FirstOperatorActionLatencySteps = report.Metrics.FirstOperatorActionLatencySteps,
                FirstFaultClearLatencySteps = report.Metrics.FirstFaultClearLatencySteps,
                PeakInvalidMeasuredSignalCount = report.Metrics.PeakInvalidMeasuredSignalCount,
                PeakAnnunciatedAlarmCount = report.Metrics.PeakAnnunciatedAlarmCount,
                PeakUnacknowledgedAlarmCount = report.Metrics.PeakUnacknowledgedAlarmCount,
                PeakActiveFaultCount = report.Metrics.PeakActiveFaultCount,
            },
            PrecedingCheckpointId = report.PrecedingCheckpointId,
        }, Options);
    }

    public PostIncidentAnalysisReport Deserialize(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var document = JsonSerializer.Deserialize<ReportDocument>(content, Options)
            ?? throw new InvalidDataException("Post-incident analysis document could not be deserialized.");
        if (document.SchemaVersion != PostIncidentAnalysisReport.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Post-incident analysis schema version {document.SchemaVersion} is not supported. Current version is {PostIncidentAnalysisReport.CurrentSchemaVersion}.");
        }
        Validate(document);

        return new PostIncidentAnalysisReport(
            document.ScenarioId!,
            new InitialConditionReference(document.InitialConditionId!, document.InitialConditionVersion),
            document.SchemaVersion,
            document.AnchorEventSequence,
            document.AnchorLogicalStep,
            document.AnchorKind,
            document.AnchorSourceId!,
            document.AnchorDetail!,
            document.WindowStartLogicalStep,
            document.WindowEndLogicalStep,
            document.Timeline!.Select(static item => new PostIncidentAnalysisTimelineEntry(
                item.Sequence,
                item.LogicalStep,
                item.RelativeLogicalStep,
                item.Relation,
                item.Kind,
                item.SourceId!,
                item.Detail!,
                item.OperatorCommand)),
            document.Trends!.Select(FromDocument),
            FromDocument(document.WindowStartState!),
            FromDocument(document.AnchorState!),
            FromDocument(document.WindowEndState!),
            new PostIncidentResponseMetrics(
                document.Metrics!.FirstAlarmLatencySteps,
                document.Metrics.FirstProtectionActivationLatencySteps,
                document.Metrics.FirstOperatorActionLatencySteps,
                document.Metrics.FirstFaultClearLatencySteps,
                document.Metrics.PeakInvalidMeasuredSignalCount,
                document.Metrics.PeakAnnunciatedAlarmCount,
                document.Metrics.PeakUnacknowledgedAlarmCount,
                document.Metrics.PeakActiveFaultCount),
            document.PrecedingCheckpointId);
    }

    private static TrendDocument ToDocument(PostIncidentTrendSample sample) => new()
    {
        LogicalStep = sample.LogicalStep,
        RelativeLogicalStep = sample.RelativeLogicalStep,
        ReactorThermalPower = sample.ReactorThermalPower,
        TotalPrimaryMass = sample.TotalPrimaryMass,
        TotalFeedwaterFlow = sample.TotalFeedwaterFlow,
        TotalSteamExportFlow = sample.TotalSteamExportFlow,
        TotalSteamFlow = sample.TotalSteamFlow,
        TotalTurbineShaftPower = sample.TotalTurbineShaftPower,
        TotalCondenserHeatRejection = sample.TotalCondenserHeatRejection,
        GrossElectricalOutput = sample.GrossElectricalOutput,
        InvalidMeasuredSignalCount = sample.InvalidMeasuredSignalCount,
        AnnunciatedAlarmCount = sample.AnnunciatedAlarmCount,
        UnacknowledgedAlarmCount = sample.UnacknowledgedAlarmCount,
        ActiveFaultCount = sample.ActiveFaultCount,
        ReactorScramActive = sample.ReactorScramActive,
        TurbineTripActive = sample.TurbineTripActive,
        GeneratorTripActive = sample.GeneratorTripActive,
    };

    private static PostIncidentTrendSample FromDocument(TrendDocument sample)
        => PostIncidentTrendSample.FromValues(
            sample.LogicalStep,
            sample.RelativeLogicalStep,
            sample.ReactorThermalPower,
            sample.TotalPrimaryMass,
            sample.TotalFeedwaterFlow,
            sample.TotalSteamExportFlow,
            sample.TotalSteamFlow,
            sample.TotalTurbineShaftPower,
            sample.TotalCondenserHeatRejection,
            sample.GrossElectricalOutput,
            sample.InvalidMeasuredSignalCount,
            sample.AnnunciatedAlarmCount,
            sample.UnacknowledgedAlarmCount,
            sample.ActiveFaultCount,
            sample.ReactorScramActive,
            sample.TurbineTripActive,
            sample.GeneratorTripActive);

    private static StateDocument ToDocument(PostIncidentStateSummary state) => new()
    {
        LogicalStep = state.LogicalStep,
        InvalidMeasuredSignalCount = state.InvalidMeasuredSignalCount,
        AnnunciatedAlarmCount = state.AnnunciatedAlarmCount,
        UnacknowledgedAlarmCount = state.UnacknowledgedAlarmCount,
        ReactorScramActive = state.ReactorScramActive,
        TurbineTripActive = state.TurbineTripActive,
        GeneratorTripActive = state.GeneratorTripActive,
        ActiveFaultCount = state.ActiveFaultCount,
    };

    private static PostIncidentStateSummary FromDocument(StateDocument state)
        => PostIncidentStateSummary.FromValues(
            state.LogicalStep,
            state.InvalidMeasuredSignalCount,
            state.AnnunciatedAlarmCount,
            state.UnacknowledgedAlarmCount,
            state.ReactorScramActive,
            state.TurbineTripActive,
            state.GeneratorTripActive,
            state.ActiveFaultCount);

    private static void Validate(ReportDocument d)
    {
        ValidateText(d.ScenarioId, "scenarioId");
        ValidateText(d.InitialConditionId, "initialConditionId");
        ValidateText(d.AnchorSourceId, "anchorSourceId");
        ValidateText(d.AnchorDetail, "anchorDetail");
        if (d.Timeline is null || d.Trends is null || d.WindowStartState is null || d.AnchorState is null || d.WindowEndState is null || d.Metrics is null)
        {
            throw new InvalidDataException("Post-incident analysis document is incomplete.");
        }
        foreach (var item in d.Timeline)
        {
            ValidateText(item.SourceId, "timeline.sourceId");
            ValidateText(item.Detail, "timeline.detail");
        }
    }

    private static void ValidateText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Post-incident analysis document field '{field}' is required.");
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class ReportDocument
    {
        public int SchemaVersion { get; set; }
        public string? ScenarioId { get; set; }
        public string? InitialConditionId { get; set; }
        public int InitialConditionVersion { get; set; }
        public long AnchorEventSequence { get; set; }
        public long AnchorLogicalStep { get; set; }
        public PostIncidentAnalysisAnchorKind AnchorKind { get; set; }
        public string? AnchorSourceId { get; set; }
        public string? AnchorDetail { get; set; }
        public long WindowStartLogicalStep { get; set; }
        public long WindowEndLogicalStep { get; set; }
        public TimelineDocument[]? Timeline { get; set; }
        public TrendDocument[]? Trends { get; set; }
        public StateDocument? WindowStartState { get; set; }
        public StateDocument? AnchorState { get; set; }
        public StateDocument? WindowEndState { get; set; }
        public MetricsDocument? Metrics { get; set; }
        public string? PrecedingCheckpointId { get; set; }
    }

    private sealed class TimelineDocument
    {
        public long Sequence { get; set; }
        public long LogicalStep { get; set; }
        public long RelativeLogicalStep { get; set; }
        public PostIncidentTemporalRelation Relation { get; set; }
        public ScenarioRecordingEventKind Kind { get; set; }
        public string? SourceId { get; set; }
        public string? Detail { get; set; }
        public ControlRoomCommand? OperatorCommand { get; set; }
    }

    private sealed class TrendDocument
    {
        public long LogicalStep { get; set; }
        public long RelativeLogicalStep { get; set; }
        public double? ReactorThermalPower { get; set; }
        public double? TotalPrimaryMass { get; set; }
        public double? TotalFeedwaterFlow { get; set; }
        public double? TotalSteamExportFlow { get; set; }
        public double? TotalSteamFlow { get; set; }
        public double? TotalTurbineShaftPower { get; set; }
        public double? TotalCondenserHeatRejection { get; set; }
        public double? GrossElectricalOutput { get; set; }
        public int InvalidMeasuredSignalCount { get; set; }
        public int AnnunciatedAlarmCount { get; set; }
        public int UnacknowledgedAlarmCount { get; set; }
        public int ActiveFaultCount { get; set; }
        public bool ReactorScramActive { get; set; }
        public bool TurbineTripActive { get; set; }
        public bool GeneratorTripActive { get; set; }
    }

    private sealed class StateDocument
    {
        public long LogicalStep { get; set; }
        public int InvalidMeasuredSignalCount { get; set; }
        public int AnnunciatedAlarmCount { get; set; }
        public int UnacknowledgedAlarmCount { get; set; }
        public bool ReactorScramActive { get; set; }
        public bool TurbineTripActive { get; set; }
        public bool GeneratorTripActive { get; set; }
        public int ActiveFaultCount { get; set; }
    }

    private sealed class MetricsDocument
    {
        public long? FirstAlarmLatencySteps { get; set; }
        public long? FirstProtectionActivationLatencySteps { get; set; }
        public long? FirstOperatorActionLatencySteps { get; set; }
        public long? FirstFaultClearLatencySteps { get; set; }
        public int PeakInvalidMeasuredSignalCount { get; set; }
        public int PeakAnnunciatedAlarmCount { get; set; }
        public int PeakUnacknowledgedAlarmCount { get; set; }
        public int PeakActiveFaultCount { get; set; }
    }
}
