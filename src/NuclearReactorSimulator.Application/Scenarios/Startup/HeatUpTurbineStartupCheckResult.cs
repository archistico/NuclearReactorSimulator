namespace NuclearReactorSimulator.Application.Scenarios.Startup;

public sealed record HeatUpTurbineStartupCheckResult(
    HeatUpTurbineStartupCheckDefinition Definition,
    bool IsSatisfied,
    string Observation);
