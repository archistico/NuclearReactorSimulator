namespace NuclearReactorSimulator.Application.Scenarios.Synchronization;

public sealed record GridSynchronizationCheckResult(
    GridSynchronizationCheckDefinition Definition,
    bool IsSatisfied,
    string Observation);
