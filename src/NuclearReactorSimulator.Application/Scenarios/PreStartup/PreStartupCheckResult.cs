namespace NuclearReactorSimulator.Application.Scenarios.PreStartup;

public sealed record PreStartupCheckResult(
    PreStartupCheckDefinition Definition,
    bool IsSatisfied,
    string Observation);
