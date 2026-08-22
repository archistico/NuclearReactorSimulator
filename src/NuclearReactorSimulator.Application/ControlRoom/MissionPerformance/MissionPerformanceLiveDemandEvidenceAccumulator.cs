using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;

namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// Incremental live-only demand evidence state. It preserves the M10.9.6 replay/projector semantics while avoiding
/// repeated full-prefix scans on every presentation refresh. Offline replay remains owned by the original full-timeline
/// projectors; this type is only the bounded read-side state required by the live MISSION/PERFORMANCE workspace.
/// </summary>
internal sealed class MissionPerformanceLiveDemandEvidenceAccumulator
{
    private readonly List<ExternalEnergyDemandEvidenceSnapshot> _recentDemandChanges = new(
        MissionPerformanceTimelineProjector.MaximumRecentOperationalEvidenceEntries);
    private ExternalEnergyDemandEvidenceSnapshot? _current;
    private bool _currentIsDemandChange;
    private bool _havePreviousAvailableBeforeCurrent;
    private string? _previousAvailableProfileBeforeCurrent;
    private double? _previousAvailableDemandBeforeCurrent;
    private int _historicalPairedSampleCount;
    private double _historicalSumAbsoluteErrorMegawatts;
    private double _historicalSumAbsoluteDemandMegawatts;

    public ExternalEnergyDemandEvidenceSnapshot Current
        => _current ?? throw new InvalidOperationException("Live demand evidence has not been initialized.");

    public IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot> RecentDemandChanges => _recentDemandChanges;

    public OperationalChallengeLiveDemandAggregate ScoreAggregate
    {
        get
        {
            var pairedSampleCount = _historicalPairedSampleCount;
            var sumAbsoluteErrorMegawatts = _historicalSumAbsoluteErrorMegawatts;
            var sumAbsoluteDemandMegawatts = _historicalSumAbsoluteDemandMegawatts;
            if (IsPairedScoreSample(Current))
            {
                pairedSampleCount++;
                sumAbsoluteErrorMegawatts += Math.Abs(Current.DemandOutputErrorMegawatts ?? 0d);
                sumAbsoluteDemandMegawatts += Math.Abs(Current.ExternalDemandMegawatts!.Value);
            }

            return new OperationalChallengeLiveDemandAggregate(
                Current.LogicalStep,
                pairedSampleCount,
                sumAbsoluteErrorMegawatts,
                sumAbsoluteDemandMegawatts);
        }
    }

    public void Seed(IEnumerable<ExternalEnergyDemandEvidenceSnapshot> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        foreach (var sample in samples)
        {
            Upsert(sample);
        }
    }

    public void Upsert(ExternalEnergyDemandEvidenceSnapshot sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (_current is null)
        {
            _havePreviousAvailableBeforeCurrent = false;
            _previousAvailableProfileBeforeCurrent = null;
            _previousAvailableDemandBeforeCurrent = null;
            ApplyCurrent(sample);
            return;
        }

        if (sample.LogicalStep < _current.LogicalStep)
        {
            throw new InvalidOperationException("Mission/Performance live demand evidence cannot move backwards in logical time.");
        }

        if (sample.LogicalStep == _current.LogicalStep)
        {
            if (Equals(sample, _current))
            {
                return;
            }

            if (_currentIsDemandChange)
            {
                if (_recentDemandChanges.Count == 0 || _recentDemandChanges[^1].LogicalStep != _current.LogicalStep)
                {
                    throw new InvalidOperationException("Live demand-change state is inconsistent with the current logical step.");
                }
                _recentDemandChanges.RemoveAt(_recentDemandChanges.Count - 1);
            }

            ApplyCurrent(sample);
            return;
        }

        CommitCurrentScoreContribution();

        if (IsTimelineAvailable(_current))
        {
            _havePreviousAvailableBeforeCurrent = true;
            _previousAvailableProfileBeforeCurrent = _current.ProfileExactId;
            _previousAvailableDemandBeforeCurrent = _current.ExternalDemandMegawatts;
        }

        ApplyCurrent(sample);
    }

    private void ApplyCurrent(ExternalEnergyDemandEvidenceSnapshot sample)
    {
        _current = sample;

        _currentIsDemandChange = IsTimelineAvailable(sample)
            && (!_havePreviousAvailableBeforeCurrent
                || _previousAvailableDemandBeforeCurrent != sample.ExternalDemandMegawatts
                || !string.Equals(_previousAvailableProfileBeforeCurrent, sample.ProfileExactId, StringComparison.Ordinal));
        if (!_currentIsDemandChange)
        {
            return;
        }

        _recentDemandChanges.Add(sample);
        if (_recentDemandChanges.Count > MissionPerformanceTimelineProjector.MaximumRecentOperationalEvidenceEntries)
        {
            _recentDemandChanges.RemoveAt(0);
        }
    }

    private void CommitCurrentScoreContribution()
    {
        if (_current is null || !IsPairedScoreSample(_current))
        {
            return;
        }

        _historicalPairedSampleCount++;
        _historicalSumAbsoluteErrorMegawatts += Math.Abs(_current.DemandOutputErrorMegawatts ?? 0d);
        _historicalSumAbsoluteDemandMegawatts += Math.Abs(_current.ExternalDemandMegawatts!.Value);
    }

    private static bool IsPairedScoreSample(ExternalEnergyDemandEvidenceSnapshot sample)
        => sample.IsAvailable
            && sample.ExternalDemandMegawatts.HasValue
            && sample.ActualElectricalOutputMegawatts.HasValue;

    private static bool IsTimelineAvailable(ExternalEnergyDemandEvidenceSnapshot sample)
        => sample.IsAvailable && sample.ExternalDemandMegawatts.HasValue;
}
