namespace NuclearReactorSimulator.Simulation.Runtime.Replay;

/// <summary>
/// One logical operator/system command scheduled for a specific fixed-step boundary.
/// Entries sharing a step execute in trace order.
/// </summary>
public sealed record SimulationCommandTraceEntry<TCommand>(long StepIndex, TCommand Command)
    where TCommand : notnull;
