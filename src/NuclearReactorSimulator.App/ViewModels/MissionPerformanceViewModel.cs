using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NuclearReactorSimulator.App.Commands;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

namespace NuclearReactorSimulator.App.ViewModels;

/// <summary>
/// Presentation-only M10.9.7.3 ViewModel. It formats the immutable Application read model and never recomputes challenge,
/// scoring, protection or plant-control semantics.
/// </summary>
public sealed class MissionPerformanceViewModel : INotifyPropertyChanged
{
    private MissionPerformanceSnapshot? _snapshot;
    private IReadOnlyList<MissionPerformanceScoreDimensionRow> _scoreDimensions = Array.Empty<MissionPerformanceScoreDimensionRow>();
    private IReadOnlyList<MissionPerformanceEventRow> _recentEvents = Array.Empty<MissionPerformanceEventRow>();
    private IReadOnlyList<MissionPerformanceTimelineRow> _timeline = Array.Empty<MissionPerformanceTimelineRow>();
    private long _presentationRevision;

    public MissionPerformanceViewModel(MissionPerformanceSnapshot? initial = null)
    {
        Apply(initial);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MissionPerformanceDrillDownRequestedEventArgs>? DrillDownRequested;

    public long PresentationRevision => _presentationRevision;

    public bool HasMission => _snapshot is not null;

    public bool HasNoMission => !HasMission;

    public string ObjectiveTitle => _snapshot?.ObjectiveTitle ?? "NO ACTIVE MISSION";

    public string ObjectiveDescription => _snapshot?.ObjectiveDescription
        ?? "No M10.9.6 operational challenge pack is bound to this session. Plant operation remains available through the normal workspaces; this surface does not invent mission semantics.";

    public string LifecycleText => _snapshot is null
        ? "UNBOUND"
        : _snapshot.LifecycleState.ToString().ToUpperInvariant();

    public string LogicalStepText => _snapshot is null
        ? "STEP —"
        : string.Create(CultureInfo.InvariantCulture, $"STEP {_snapshot.LogicalStep}");

    public string ElapsedText => _snapshot?.ElapsedLogicalSteps is { } elapsed
        ? string.Create(CultureInfo.InvariantCulture, $"ELAPSED {elapsed} STEPS")
        : "ELAPSED —";

    public string TargetWindowText
    {
        get
        {
            if (_snapshot?.TargetWindowStartLogicalStep is not { } start
                || _snapshot.TargetWindowEndLogicalStep is not { } end)
            {
                return "TARGET WINDOW —";
            }
            return string.Create(CultureInfo.InvariantCulture, $"TARGET {start}–{end}");
        }
    }

    public string ExternalDemandText => FormatMegawatts(
        _snapshot?.Demand.ExternalDemandAvailable == true ? _snapshot.Demand.ExternalDemandMegawatts : null);

    public string RequestedLoadText => FormatMegawatts(_snapshot?.Demand.RequestedGeneratorLoadMegawatts);

    public string ActualOutputText => FormatMegawatts(_snapshot?.Demand.ActualElectricalOutputMegawatts);

    public string DemandErrorText => _snapshot?.Demand.DemandOutputErrorMegawatts is { } value
        ? string.Create(CultureInfo.InvariantCulture, $"{value:+0.000;-0.000;0.000} MWe")
        : "UNAVAILABLE";

    public string NextDemandText
    {
        get
        {
            if (_snapshot?.Demand.NextScheduledDemandChangeLogicalStep is not { } step
                || _snapshot.Demand.NextScheduledDemandMegawatts is not { } demand)
            {
                return "NEXT CHANGE —";
            }
            return string.Create(CultureInfo.InvariantCulture, $"STEP {step} → {demand:0.000} MWe");
        }
    }

    public bool ScoreAvailable => _snapshot?.Score.IsAvailable == true;

    public string ScoreText => _snapshot?.Score.FinalPercentage is { } percentage
        ? string.Create(CultureInfo.InvariantCulture, $"{percentage:0.##}%")
        : "UNAVAILABLE";

    public string GradeText => _snapshot?.Score.Grade?.ToString().ToUpperInvariant() ?? "NOT SCORED";

    public string ScoreEvidenceText => _snapshot?.Score.IsEvidenceComplete switch
    {
        true => "EVIDENCE COMPLETE",
        false => "EVIDENCE INCOMPLETE",
        _ => "EVIDENCE UNAVAILABLE",
    };

    public IReadOnlyList<MissionPerformanceScoreDimensionRow> ScoreDimensions => _scoreDimensions;

    public IReadOnlyList<MissionPerformanceEventRow> RecentEvents => _recentEvents;

    public bool HasRecentEvents => _recentEvents.Count != 0;

    public IReadOnlyList<MissionPerformanceTimelineRow> Timeline => _timeline;

    public bool HasTimeline => _timeline.Count != 0;

    public string TimelineRetentionText => _snapshot is null
        ? "LIFECYCLE SPINE — · RECENT EVIDENCE —"
        : string.Create(
            CultureInfo.InvariantCulture,
            $"LIFECYCLE SPINE {_snapshot.LifecycleSpine.Count} · RECENT EVIDENCE {_snapshot.RecentOperationalEvidence.Count}");

    public bool SafetyAlertActive => _snapshot is not null
        && (_snapshot.Score.DominanceOutcome == ChallengeScoreDominanceOutcome.CriticalSafetyFailure
            || _snapshot.RecentEvents.Any(static item => item.IsCritical));

    public string SafetyStatusText
    {
        get
        {
            if (_snapshot is null)
            {
                return "MISSION SAFETY EVIDENCE UNAVAILABLE";
            }
            if (_snapshot.Score.DominanceOutcome == ChallengeScoreDominanceOutcome.CriticalSafetyFailure)
            {
                return "CRITICAL SAFETY / PROTECTION FAILURE";
            }
            if (_snapshot.RecentEvents.Any(static item => item.IsCritical))
            {
                return "CRITICAL PROTECTION / SCORING EVIDENCE PRESENT";
            }
            return "NO CRITICAL SAFETY FAILURE EVIDENCE";
        }
    }

    public string AssistanceModeText => _snapshot?.AssistanceMode.ToString().ToUpperInvariant() ?? "UNAVAILABLE";

    public string ControlAuthorityText
    {
        get
        {
            if (_snapshot?.PlantControlAuthorityAvailable != true)
            {
                return "UNAVAILABLE";
            }
            var requested = _snapshot.RequestedControlAuthority?.ToString().ToUpperInvariant() ?? "—";
            var effective = _snapshot.EffectiveControlAuthority?.ToString().ToUpperInvariant() ?? "—";
            return $"REQ {requested} · EFF {effective}";
        }
    }

    public string ControlAuthorityHealthText => _snapshot?.PlantControlAuthorityAvailable == true
        ? _snapshot.ControlAuthorityHealth?.ToString().ToUpperInvariant() ?? "UNAVAILABLE"
        : "UNAVAILABLE";

    public string ControlAuthorityDegradationText => string.IsNullOrWhiteSpace(_snapshot?.ControlAuthorityDegradationReason)
        ? "No control-authority degradation reported."
        : _snapshot!.ControlAuthorityDegradationReason!;

    public bool UpdateSnapshot(MissionPerformanceSnapshot? snapshot)
    {
        if (MissionPerformancePresentationComparer.AreEquivalent(_snapshot, snapshot))
        {
            return false;
        }

        var listChanges = Apply(snapshot);
        OnAllPropertiesChanged(listChanges);
        return true;
    }

    private MissionPerformanceListChanges Apply(MissionPerformanceSnapshot? snapshot)
    {
        var previous = _snapshot;
        var scoreDimensionsChanged = !SequenceEquivalent(
            previous?.Score.Dimensions,
            snapshot?.Score.Dimensions);
        var recentEventsChanged = !SequenceEquivalent(
            previous?.RecentEvents,
            snapshot?.RecentEvents);
        var timelineChanged = !SequenceEquivalent(
            previous?.Timeline,
            snapshot?.Timeline);

        _snapshot = snapshot;
        if (scoreDimensionsChanged)
        {
            _scoreDimensions = snapshot?.Score.Dimensions
                .Select(static item => new MissionPerformanceScoreDimensionRow(
                    DisplayDimension(item.Kind),
                    string.Create(CultureInfo.InvariantCulture, $"{item.AwardedPoints:0.##} / {item.MaximumPoints:0.##}"),
                    item.IsEvidenceAvailable ? item.EvidenceSummary : "Evidence unavailable.",
                    item.IsCriticalFailure))
                .ToArray()
                ?? Array.Empty<MissionPerformanceScoreDimensionRow>();
        }
        if (recentEventsChanged)
        {
            _recentEvents = snapshot?.RecentEvents
                .OrderByDescending(static item => item.LogicalStep)
                .ThenByDescending(static item => item.SourceSequence ?? long.MinValue)
                .Select(static item => new MissionPerformanceEventRow(
                    string.Create(CultureInfo.InvariantCulture, $"STEP {item.LogicalStep}"),
                    item.Kind.ToString().ToUpperInvariant(),
                    item.SourceId,
                    item.Summary,
                    item.IsCritical))
                .ToArray()
                ?? Array.Empty<MissionPerformanceEventRow>();
        }
        if (timelineChanged)
        {
            _timeline = snapshot?.Timeline
                .OrderByDescending(static item => item.LogicalStep)
                .ThenByDescending(static item => item.SourceSequence ?? long.MinValue)
                .Select(item => new MissionPerformanceTimelineRow(
                    string.Create(CultureInfo.InvariantCulture, $"STEP {item.LogicalStep}"),
                    item.Kind.ToString().ToUpperInvariant(),
                    item.SourceId,
                    item.Summary,
                    item.IsCritical,
                    item.DrillDownTarget,
                    () => RequestDrillDown(item.DrillDownTarget)))
                .ToArray()
                ?? Array.Empty<MissionPerformanceTimelineRow>();
        }
        _presentationRevision++;
        return new MissionPerformanceListChanges(scoreDimensionsChanged, recentEventsChanged, timelineChanged);
    }

    private static bool SequenceEquivalent<T>(IReadOnlyList<T>? left, IReadOnlyList<T>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return left.Count == right.Count && left.SequenceEqual(right);
    }

    private void OnAllPropertiesChanged(MissionPerformanceListChanges listChanges)
    {
        OnPropertyChanged(nameof(PresentationRevision));
        OnPropertyChanged(nameof(HasMission));
        OnPropertyChanged(nameof(HasNoMission));
        OnPropertyChanged(nameof(ObjectiveTitle));
        OnPropertyChanged(nameof(ObjectiveDescription));
        OnPropertyChanged(nameof(LifecycleText));
        OnPropertyChanged(nameof(LogicalStepText));
        OnPropertyChanged(nameof(ElapsedText));
        OnPropertyChanged(nameof(TargetWindowText));
        OnPropertyChanged(nameof(ExternalDemandText));
        OnPropertyChanged(nameof(RequestedLoadText));
        OnPropertyChanged(nameof(ActualOutputText));
        OnPropertyChanged(nameof(DemandErrorText));
        OnPropertyChanged(nameof(NextDemandText));
        OnPropertyChanged(nameof(ScoreAvailable));
        OnPropertyChanged(nameof(ScoreText));
        OnPropertyChanged(nameof(GradeText));
        OnPropertyChanged(nameof(ScoreEvidenceText));
        if (listChanges.ScoreDimensionsChanged)
        {
            OnPropertyChanged(nameof(ScoreDimensions));
        }
        if (listChanges.RecentEventsChanged)
        {
            OnPropertyChanged(nameof(RecentEvents));
            OnPropertyChanged(nameof(HasRecentEvents));
        }
        if (listChanges.TimelineChanged)
        {
            OnPropertyChanged(nameof(Timeline));
            OnPropertyChanged(nameof(HasTimeline));
        }
        OnPropertyChanged(nameof(TimelineRetentionText));
        OnPropertyChanged(nameof(SafetyAlertActive));
        OnPropertyChanged(nameof(SafetyStatusText));
        OnPropertyChanged(nameof(AssistanceModeText));
        OnPropertyChanged(nameof(ControlAuthorityText));
        OnPropertyChanged(nameof(ControlAuthorityHealthText));
        OnPropertyChanged(nameof(ControlAuthorityDegradationText));
    }

    private static string FormatMegawatts(double? value)
        => value.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"{value.Value:0.000} MWe")
            : "UNAVAILABLE";

    private static string DisplayDimension(ChallengeScoreDimensionKind kind)
        => kind switch
        {
            ChallengeScoreDimensionKind.SafetyProtectionDiscipline => "SAFETY / PROTECTION",
            ChallengeScoreDimensionKind.ProcedureRequiredActions => "PROCEDURE",
            ChallengeScoreDimensionKind.StabilityOperatingQuality => "STABILITY / QUALITY",
            ChallengeScoreDimensionKind.DemandTracking => "DEMAND TRACKING",
            ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency => "LOGICAL TIME",
            _ => kind.ToString().ToUpperInvariant(),
        };

    private void RequestDrillDown(MissionPerformanceDrillDownTarget? target)
    {
        if (target is not null)
        {
            DrillDownRequested?.Invoke(this, new MissionPerformanceDrillDownRequestedEventArgs(target));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal readonly record struct MissionPerformanceListChanges(
    bool ScoreDimensionsChanged,
    bool RecentEventsChanged,
    bool TimelineChanged);


public sealed record MissionPerformanceScoreDimensionRow(
    string Title,
    string PointsText,
    string EvidenceText,
    bool IsCritical);

public sealed record MissionPerformanceEventRow(
    string StepText,
    string KindText,
    string SourceText,
    string DetailText,
    bool IsCritical);

public sealed class MissionPerformanceTimelineRow
{
    public MissionPerformanceTimelineRow(
        string stepText,
        string kindText,
        string sourceText,
        string detailText,
        bool isCritical,
        MissionPerformanceDrillDownTarget? target,
        Action requestDrillDown)
    {
        StepText = stepText;
        KindText = kindText;
        SourceText = sourceText;
        DetailText = detailText;
        IsCritical = isCritical;
        DrillDownText = target?.Label ?? "EVIDENCE ONLY";
        HasDrillDown = target is not null;
        DrillDownCommand = new DelegateCommand(requestDrillDown);
    }

    public string StepText { get; }
    public string KindText { get; }
    public string SourceText { get; }
    public string DetailText { get; }
    public bool IsCritical { get; }
    public bool HasDrillDown { get; }
    public string DrillDownText { get; }
    public ICommand DrillDownCommand { get; }
}

public sealed class MissionPerformanceDrillDownRequestedEventArgs : EventArgs
{
    public MissionPerformanceDrillDownRequestedEventArgs(MissionPerformanceDrillDownTarget target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public MissionPerformanceDrillDownTarget Target { get; }
}
