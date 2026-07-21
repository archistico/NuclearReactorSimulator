namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>Single-pass lifecycle for one deterministic scenario fault.</summary>
public enum ScenarioFaultLifecycleState
{
    Pending = 0,
    Active = 1,
    Cleared = 2,
}
