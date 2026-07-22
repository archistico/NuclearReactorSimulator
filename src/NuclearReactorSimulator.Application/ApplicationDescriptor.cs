namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.7 — Session, Checkpoint, Replay & Save Workspace",
        "Implementation candidate — M10.7 on validated M10.6 baseline; adds explicit opt-in M9.1 recording, replay-backed checkpoints, compact versioned session archives, exact-version save/load/restore through ScenarioFullReplayRunner, and continuation recording after verified restore without opaque solver-state dumps");
}
