namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// A command tagged with a monotonic sequence number that defines deterministic execution order.
/// </summary>
public sealed record QueuedSimulationCommand<TCommand>(long Sequence, TCommand Command)
    where TCommand : notnull;
