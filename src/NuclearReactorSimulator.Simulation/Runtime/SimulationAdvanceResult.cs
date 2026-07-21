namespace NuclearReactorSimulator.Simulation.Runtime;

public sealed record SimulationAdvanceResult<TStateSnapshot>(
    long StepsExecuted,
    SimulationSnapshot<TStateSnapshot> Snapshot)
    where TStateSnapshot : notnull;
