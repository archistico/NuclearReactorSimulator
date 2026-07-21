namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Defines the deterministic boundary between the generic runtime and a concrete plant model.
/// </summary>
public interface ISimulationKernel<TState, TCommand, TStateSnapshot>
    where TState : notnull
    where TCommand : notnull
    where TStateSnapshot : notnull
{
    TState Step(
        TState state,
        IReadOnlyList<QueuedSimulationCommand<TCommand>> commands,
        SimulationStepContext context);

    TStateSnapshot CreateSnapshot(TState state);
}
