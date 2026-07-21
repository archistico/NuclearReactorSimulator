namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Immutable publication envelope used to expose simulation state without leaking mutable engine internals.
/// </summary>
public sealed record SimulationSnapshot<TStateSnapshot>(
    SimulationRuntimeSnapshot Runtime,
    TStateSnapshot State)
    where TStateSnapshot : notnull;
