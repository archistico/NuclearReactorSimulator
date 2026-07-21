namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Immutable runtime metadata exposed together with a model-specific state snapshot.
/// </summary>
public sealed record SimulationRuntimeSnapshot(
    long StepIndex,
    TimeSpan ElapsedSimulationTime,
    TimeSpan FixedTimeStep,
    SimulationRunState RunState,
    SimulationSpeed Speed,
    int PendingCommandCount,
    SimulationFaultSnapshot? Fault);
