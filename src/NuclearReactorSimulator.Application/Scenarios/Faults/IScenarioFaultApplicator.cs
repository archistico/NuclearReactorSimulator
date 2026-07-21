namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>
/// Runtime-side fault-effect boundary. M8.1 owns scheduling/lifecycle only; M8.2+ applicators own typed subsystem effects.
/// </summary>
public interface IScenarioFaultApplicator
{
    void Activate(ScenarioFaultDefinition fault);

    void Deactivate(ScenarioFaultDefinition fault);
}
