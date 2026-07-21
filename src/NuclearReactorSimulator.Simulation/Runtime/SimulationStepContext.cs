namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Immutable timing data for one physical simulation step.
/// </summary>
public readonly record struct SimulationStepContext(
    long StepIndex,
    TimeSpan StartTime,
    TimeSpan EndTime,
    TimeSpan DeltaTime);
