using NuclearReactorSimulator.Simulation.Runtime;
using NuclearReactorSimulator.Simulation.Runtime.Replay;

namespace NuclearReactorSimulator.Simulation.Tests.Harness;

/// <summary>
/// Reusable headless scenario harness for deterministic simulation tests.
/// It schedules commands by logical step and captures every resulting immutable snapshot.
/// </summary>
internal sealed class DeterministicScenarioHarness<TState, TCommand, TStateSnapshot>
    where TState : notnull
    where TCommand : notnull
    where TStateSnapshot : notnull
{
    private readonly SimulationRuntime<TState, TCommand, TStateSnapshot> _runtime;

    public DeterministicScenarioHarness(
        SimulationRuntime<TState, TCommand, TStateSnapshot> runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
    }

    public DeterministicScenarioResult<TStateSnapshot> Run(
        SimulationCommandTrace<TCommand> trace,
        long finalStepIndex)
    {
        ArgumentNullException.ThrowIfNull(trace);

        var initial = _runtime.GetSnapshot();
        if (initial.Runtime.RunState != SimulationRunState.Paused)
        {
            throw new InvalidOperationException("Scenario harness requires a paused runtime.");
        }

        if (initial.Runtime.PendingCommandCount != 0)
        {
            throw new InvalidOperationException("Scenario harness requires an empty command queue.");
        }

        if (finalStepIndex < initial.Runtime.StepIndex || trace.LastStepIndex > finalStepIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(finalStepIndex));
        }

        if (trace.Entries.Any(entry => entry.StepIndex <= initial.Runtime.StepIndex))
        {
            throw new ArgumentException("Trace contains commands for already executed steps.", nameof(trace));
        }

        var snapshots = new List<SimulationSnapshot<TStateSnapshot>> { initial };
        var traceIndex = 0;
        var current = initial;

        while (current.Runtime.StepIndex < finalStepIndex)
        {
            var nextStep = checked(current.Runtime.StepIndex + 1);

            while (traceIndex < trace.Entries.Count && trace.Entries[traceIndex].StepIndex == nextStep)
            {
                _runtime.EnqueueCommand(trace.Entries[traceIndex].Command);
                traceIndex++;
            }

            current = _runtime.StepOnce();
            snapshots.Add(current);
        }

        return new DeterministicScenarioResult<TStateSnapshot>(
            current,
            snapshots.AsReadOnly());
    }
}

internal sealed record DeterministicScenarioResult<TStateSnapshot>(
    SimulationSnapshot<TStateSnapshot> FinalSnapshot,
    IReadOnlyList<SimulationSnapshot<TStateSnapshot>> Snapshots)
    where TStateSnapshot : notnull;
