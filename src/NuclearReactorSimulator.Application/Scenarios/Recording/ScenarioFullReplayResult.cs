namespace NuclearReactorSimulator.Application.Scenarios.Recording;

public sealed record ScenarioFullReplayResult(
    ScenarioSession Session,
    ScenarioRecording ReplayedRecording,
    int VerifiedFrameCount,
    int VerifiedEventCount);
