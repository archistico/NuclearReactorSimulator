using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

/// <summary>
/// Thin M10.7 Application-layer lifecycle facade over the existing M9.1 recorder/checkpoint/replay owners. It creates no
/// physical state and no second checkpoint/restore format.
/// </summary>
public sealed class OperatorComputerSessionWorkspaceController
{
    private readonly ScenarioSession _session;
    private readonly ScenarioRecorder? _recorder;
    private readonly ScenarioFullReplayRunner _replayRunner;
    private readonly IScenarioSessionArchiveSerializer _archiveSerializer;

    public OperatorComputerSessionWorkspaceController(
        ScenarioSession session,
        ScenarioRecorder? recorder,
        ScenarioFullReplayRunner replayRunner,
        IScenarioSessionArchiveSerializer archiveSerializer)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _recorder = recorder;
        _replayRunner = replayRunner ?? throw new ArgumentNullException(nameof(replayRunner));
        _archiveSerializer = archiveSerializer ?? throw new ArgumentNullException(nameof(archiveSerializer));
    }

    public event EventHandler? Changed;

    public bool RecorderActive => _recorder is not null;

    public OperatorComputerSessionSnapshot Current
        => new(
            RecorderActive,
            _session.Scenario.ScenarioId,
            _session.Scenario.Title,
            $"{_session.InitialCondition.Reference.InitialConditionId} v{_session.InitialCondition.Reference.Version}",
            _session.Coordinator.Current.LogicalStep,
            _recorder?.FrameCount ?? 0,
            (_recorder?.Checkpoints ?? Array.Empty<ScenarioCheckpoint>()).Select(static checkpoint =>
                new OperatorComputerSessionCheckpointSnapshot(
                    checkpoint.CheckpointId,
                    checkpoint.LogicalStep,
                    checkpoint.SnapshotFingerprint)));

    public ScenarioCheckpoint CreateCheckpoint()
    {
        EnsureRecorderAndPaused();
        var checkpointId = $"cp-{_recorder!.Checkpoints.Count + 1:D3}-step-{_session.Coordinator.Current.LogicalStep:D8}";
        var checkpoint = _recorder.CreateCheckpoint(checkpointId);
        Changed?.Invoke(this, EventArgs.Empty);
        return checkpoint;
    }

    public ScenarioSessionArchive CaptureArchive()
    {
        EnsureRecorderAndPaused();
        var recording = _recorder!.Capture();
        var archiveId = $"{_session.Scenario.ScenarioId}-step-{recording.FinalLogicalStep:D8}";
        return ScenarioSessionArchive.FromRecording(archiveId, _session.Scenario, recording);
    }

    public string ExportArchive()
        => _archiveSerializer.Serialize(CaptureArchive());

    public string VerifyCurrentReplay()
    {
        var archive = CaptureArchive();
        var result = _replayRunner.ReplayAndVerify(archive);
        return $"VERIFIED — {result.VerifiedFrameCount} frames · {result.VerifiedEventCount} events · final STEP {result.Session.Coordinator.Current.LogicalStep:D8}";
    }

    private void EnsureRecorderAndPaused()
    {
        if (_recorder is null)
        {
            throw new InvalidOperationException("M9.1 recorder is not active. Restart the desktop as a recorded session before creating checkpoints or archives.");
        }
        if (_session.Coordinator.Current.RunState != ControlRoomRunState.Paused)
        {
            throw new InvalidOperationException("Pause the runtime before checkpoint, replay verification or session archive operations.");
        }
    }
}
