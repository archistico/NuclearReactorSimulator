using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// M10.9.7.3/7.4 live read-side adapter. It observes deterministic session evidence, accumulates the same demand samples
/// used by M10.9.6 scoring and publishes immutable MissionPerformanceSnapshot instances at presentation boundaries. An
/// optional already-verified recording prefix reconstructs challenge/demand evidence before future live evidence is attached.
/// It cannot dispatch plant commands, request control authority or mutate challenge/scoring owners.
/// </summary>
public sealed class MissionPerformanceLiveSnapshotSource : IMissionPerformanceSnapshotSource, IDisposable
{
    private readonly object _gate = new();
    private readonly ScenarioSession _session;
    private readonly OperationalChallengePackDefinition _pack;
    private readonly ScenarioChallengeTracker _tracker;
    private readonly ReplayedChallengeContinuationEvidenceSource? _replayedContinuationEvidence;
    private readonly ScenarioRecorder? _recorder;
    private readonly MissionPerformanceLiveDemandEvidenceAccumulator _demandEvidence = new();
    private IReadOnlyList<ScenarioRecordingEvent> _cachedRecordingEvents = Array.Empty<ScenarioRecordingEvent>();
    private int _cachedRecorderEventCount = -1;
    private TrainingGuidanceMode _assistanceMode;
    private ControlRoomSnapshot _latestDeterministicSnapshot;
    private MissionPerformanceSnapshot _current;
    private bool _disposed;

    public MissionPerformanceLiveSnapshotSource(
        ScenarioSession session,
        OperationalChallengePackDefinition pack,
        TrainingGuidanceMode assistanceMode = TrainingGuidanceMode.Guided,
        ScenarioRecorder? recorder = null,
        ScenarioRecording? reconstructedPrefix = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _pack = pack ?? throw new ArgumentNullException(nameof(pack));
        _recorder = recorder;
        if (!Enum.IsDefined(assistanceMode))
        {
            throw new ArgumentOutOfRangeException(nameof(assistanceMode));
        }
        if (!string.Equals(session.Scenario.ScenarioId, pack.Scenario.ScenarioId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Mission pack scenario identity must match the live scenario session.", nameof(pack));
        }

        _assistanceMode = assistanceMode;
        _latestDeterministicSnapshot = session.Coordinator.Current;
        if (reconstructedPrefix is null)
        {
            _tracker = ScenarioChallengeTracker.Attach(session, pack.Challenge, pack.ConditionEvaluator);
        }
        else
        {
            if (!string.Equals(reconstructedPrefix.ScenarioId, pack.Scenario.ScenarioId, StringComparison.Ordinal)
                || reconstructedPrefix.InitialCondition != pack.Scenario.InitialCondition
                || reconstructedPrefix.FinalLogicalStep != session.Coordinator.Current.LogicalStep)
            {
                throw new ArgumentException("Reconstructed mission prefix must match the exact pack/session identity and current logical step.", nameof(reconstructedPrefix));
            }

            var replayProjection = OperationalChallengeRecordingProjector.Project(
                pack,
                reconstructedPrefix,
                assistanceMode,
                session.PlantControlAuthority.CurrentAutomation.IsAvailable
                    ? session.PlantControlAuthority.CurrentAutomation.EffectiveAuthority
                    : PlantControlAuthorityMode.Manual);
            _demandEvidence.Seed(replayProjection.Frames.Select(static frame => frame.ExternalDemand));

            _replayedContinuationEvidence = new ReplayedChallengeContinuationEvidenceSource(reconstructedPrefix.Frames[0].Snapshot);
            _tracker = ScenarioChallengeTracker.AttachDeterministicEvidence(
                pack.Scenario,
                _replayedContinuationEvidence,
                pack.Challenge,
                pack.ConditionEvaluator);
            _replayedContinuationEvidence.ReplayPrefix(reconstructedPrefix);
            _replayedContinuationEvidence.AttachLive(session);
        }

        UpsertDemandSample(_latestDeterministicSnapshot);
        _current = BuildCurrent(_latestDeterministicSnapshot);

        _tracker.LifecycleChanged += OnLifecycleChanged;
        session.Coordinator.DeterministicStepCompleted += OnDeterministicStepCompleted;
        session.Coordinator.SnapshotChanged += OnPresentationSnapshotChanged;
        session.PlantControlAuthority.AuthorityChanged += OnAuthorityChanged;
    }

    public event EventHandler<MissionPerformanceSnapshotChangedEventArgs>? SnapshotChanged;

    public MissionPerformanceSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                return _current;
            }
        }
    }

    public TrainingGuidanceMode AssistanceMode
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                return _assistanceMode;
            }
        }
    }

    /// <summary>Updates presentation/scoring assistance context only; it never mutates challenge or plant state.</summary>
    public void SetAssistanceMode(TrainingGuidanceMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        MissionPerformanceSnapshot? publish;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_assistanceMode == mode)
            {
                return;
            }
            _assistanceMode = mode;
            publish = RefreshLocked(_latestDeterministicSnapshot);
        }
        Publish(publish);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
        }

        _tracker.LifecycleChanged -= OnLifecycleChanged;
        _session.Coordinator.DeterministicStepCompleted -= OnDeterministicStepCompleted;
        _session.Coordinator.SnapshotChanged -= OnPresentationSnapshotChanged;
        _session.PlantControlAuthority.AuthorityChanged -= OnAuthorityChanged;
        _tracker.Dispose();
        _replayedContinuationEvidence?.Dispose();
    }

    private void OnDeterministicStepCompleted(object? sender, ControlRoomSnapshotChangedEventArgs e)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _latestDeterministicSnapshot = e.Snapshot;
            UpsertDemandSample(e.Snapshot);
        }
    }

    private void OnPresentationSnapshotChanged(object? sender, ControlRoomSnapshotChangedEventArgs e)
    {
        MissionPerformanceSnapshot? publish;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            // AdvanceRunning publishes all deterministic-step evidence before it emits the batch's presentation snapshots.
            // Intermediate presentation snapshots can therefore arrive after the live evidence timeline has already advanced
            // beyond their logical step. They are stale for Mission/Performance and must not rewind demand/scoring evidence.
            if (e.Snapshot.LogicalStep < _latestDeterministicSnapshot.LogicalStep)
            {
                return;
            }
            if (e.Snapshot.LogicalStep > _latestDeterministicSnapshot.LogicalStep)
            {
                throw new InvalidOperationException(
                    "Mission/Performance presentation evidence cannot lead deterministic-step evidence in logical time.");
            }

            publish = RefreshLocked(e.Snapshot);
        }
        Publish(publish);
    }

    private void OnLifecycleChanged(object? sender, ChallengeLifecycleChangedEventArgs e)
    {
        MissionPerformanceSnapshot? publish = null;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            // Operator-action evidence can change lifecycle at the currently published step and should update the HMI
            // immediately. Deterministic-step lifecycle events are published later by the normal presentation cadence.
            if (e.Snapshot.LogicalStep == _latestDeterministicSnapshot.LogicalStep)
            {
                publish = RefreshLocked(_latestDeterministicSnapshot);
            }
        }
        Publish(publish);
    }

    private void OnAuthorityChanged(object? sender, PlantControlAuthorityChangedEventArgs e)
    {
        MissionPerformanceSnapshot? publish;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            publish = RefreshLocked(_latestDeterministicSnapshot);
        }
        Publish(publish);
    }

    private MissionPerformanceSnapshot? RefreshLocked(ControlRoomSnapshot snapshot)
    {
        var candidate = BuildCurrent(snapshot);
        if (MissionPerformancePresentationComparer.AreEquivalent(_current, candidate))
        {
            return null;
        }
        _current = candidate;
        return candidate;
    }

    private MissionPerformanceSnapshot BuildCurrent(ControlRoomSnapshot snapshot)
    {
        var lifecycle = ChallengeLifecycleLogicalStepAlignment.Align(_tracker.Snapshot, snapshot.LogicalStep);
        UpsertDemandSample(snapshot);
        var demand = _demandEvidence.Current;
        var scoreEvidence = OperationalChallengeScoreEvidenceProjector.ProjectLive(_pack, lifecycle, _demandEvidence.ScoreAggregate);
        var automation = _session.PlantControlAuthority.CurrentAutomation;
        var authorityMode = automation.IsAvailable ? automation.EffectiveAuthority : PlantControlAuthorityMode.Manual;
        var score = ChallengeScoreCalculator.Evaluate(
            _pack.Challenge,
            _pack.ScoringPolicy,
            _assistanceMode,
            authorityMode,
            scoreEvidence);

        return MissionPerformanceSnapshotProjector.Project(
            _pack,
            lifecycle,
            snapshot,
            demand,
            score,
            _assistanceMode,
            automation,
            CurrentRecordingEvents(),
            _demandEvidence.RecentDemandChanges);
    }

    private void UpsertDemandSample(ControlRoomSnapshot snapshot)
    {
        var lifecycle = ChallengeLifecycleLogicalStepAlignment.Align(_tracker.Snapshot, snapshot.LogicalStep);
        var sample = ScenarioChallengeExternalDemandProjector.Project(_pack.Challenge, lifecycle, snapshot);
        _demandEvidence.Upsert(sample);
    }

    private IReadOnlyList<ScenarioRecordingEvent>? CurrentRecordingEvents()
    {
        if (_recorder is null)
        {
            return null;
        }
        if (_cachedRecorderEventCount != _recorder.EventCount)
        {
            _cachedRecordingEvents = _recorder.Events;
            _cachedRecorderEventCount = _recorder.EventCount;
        }
        return _cachedRecordingEvents;
    }

    private void Publish(MissionPerformanceSnapshot? snapshot)
    {
        if (snapshot is not null)
        {
            SnapshotChanged?.Invoke(this, new MissionPerformanceSnapshotChangedEventArgs(snapshot));
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MissionPerformanceLiveSnapshotSource));
        }
    }
}
