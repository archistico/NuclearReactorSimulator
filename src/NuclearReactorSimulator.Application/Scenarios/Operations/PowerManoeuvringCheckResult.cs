namespace NuclearReactorSimulator.Application.Scenarios.Operations;

public sealed record PowerManoeuvringCheckResult(
    PowerManoeuvringCheckDefinition Definition,
    bool IsSatisfied,
    string Observation);
