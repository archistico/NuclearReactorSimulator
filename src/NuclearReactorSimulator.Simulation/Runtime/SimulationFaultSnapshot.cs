namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Stable diagnostic data describing the step that failed without exposing mutable exception objects through snapshots.
/// </summary>
public sealed record SimulationFaultSnapshot(
    long FailedStepIndex,
    TimeSpan FailedStepStartTime,
    string ExceptionType,
    string Message);
