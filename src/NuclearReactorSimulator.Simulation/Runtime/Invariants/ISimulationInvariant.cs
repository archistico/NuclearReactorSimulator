namespace NuclearReactorSimulator.Simulation.Runtime.Invariants;

/// <summary>
/// Evaluates a deterministic invariant against a candidate state before a simulation step is committed.
/// </summary>
public interface ISimulationInvariant<TState>
    where TState : notnull
{
    string Name { get; }

    SimulationInvariantResult Evaluate(TState state, SimulationStepContext context);
}
