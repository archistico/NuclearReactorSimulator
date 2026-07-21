namespace NuclearReactorSimulator.Application.Scenarios.Criticality;

public sealed record FirstCriticalityCheckResult(
    FirstCriticalityCheckDefinition Definition,
    bool IsSatisfied,
    string Observation);
