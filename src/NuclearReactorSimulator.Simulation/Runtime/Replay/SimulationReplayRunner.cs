namespace NuclearReactorSimulator.Simulation.Runtime.Replay;

/// <summary>
/// Replays a logical command trace against a paused deterministic runtime using fixed-step execution only.
/// </summary>
public static class SimulationReplayRunner
{
    public static SimulationSnapshot<TStateSnapshot> ReplayToStep<TState, TCommand, TStateSnapshot>(
        SimulationRuntime<TState, TCommand, TStateSnapshot> runtime,
        SimulationCommandTrace<TCommand> trace,
        long finalStepIndex)
        where TState : notnull
        where TCommand : notnull
        where TStateSnapshot : notnull
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(trace);

        var initial = runtime.GetSnapshot();

        if (initial.Runtime.RunState != SimulationRunState.Paused)
        {
            throw new InvalidOperationException("Replay requires a paused simulation runtime.");
        }

        if (initial.Runtime.PendingCommandCount != 0)
        {
            throw new InvalidOperationException("Replay requires an empty runtime command queue.");
        }

        if (finalStepIndex < initial.Runtime.StepIndex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finalStepIndex),
                "The replay target cannot precede the current simulation step.");
        }

        if (trace.LastStepIndex > finalStepIndex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finalStepIndex),
                "The replay target must include every command trace entry.");
        }

        if (trace.Entries.Any(entry => entry.StepIndex <= initial.Runtime.StepIndex))
        {
            throw new ArgumentException(
                "Command trace entries must target steps after the runtime's current step.",
                nameof(trace));
        }

        var traceIndex = 0;
        var snapshot = initial;

        while (snapshot.Runtime.StepIndex < finalStepIndex)
        {
            var nextStep = checked(snapshot.Runtime.StepIndex + 1);

            while (traceIndex < trace.Entries.Count && trace.Entries[traceIndex].StepIndex == nextStep)
            {
                runtime.EnqueueCommand(trace.Entries[traceIndex].Command);
                traceIndex++;
            }

            snapshot = runtime.StepOnce();
        }

        return snapshot;
    }
}
