using NuclearReactorSimulator.Simulation.Runtime;
using NuclearReactorSimulator.Simulation.Runtime.Replay;
using NuclearReactorSimulator.Simulation.Tests.Harness;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Runtime;

public sealed class SimulationReplayTests
{
    [Fact]
    public void CommandTrace_RejectsInvalidOrOutOfOrderSteps()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationCommandTrace<AddCommand>(
            [new SimulationCommandTraceEntry<AddCommand>(0, new AddCommand(1))]));

        Assert.Throws<ArgumentException>(() => new SimulationCommandTrace<AddCommand>(
            [
                new SimulationCommandTraceEntry<AddCommand>(3, new AddCommand(1)),
                new SimulationCommandTraceEntry<AddCommand>(2, new AddCommand(1)),
            ]));
    }

    [Fact]
    public void Replay_ExecutesSameStepCommandsInTraceOrder()
    {
        var runtime = CreateRuntime();
        var trace = new SimulationCommandTrace<AddCommand>(
            [
                new(2, new AddCommand(10)),
                new(2, new AddCommand(20)),
                new(4, new AddCommand(-5)),
            ]);

        var snapshot = SimulationReplayRunner.ReplayToStep(runtime, trace, 5);

        Assert.Equal(30, snapshot.State.Value);
        Assert.Equal("1:10,2:20,3:-5", snapshot.State.ProcessedCommands);
        Assert.Equal(5L, snapshot.Runtime.StepIndex);
    }

    [Fact]
    public void IdenticalTrace_ReplaysToIdenticalFinalSnapshot()
    {
        var trace = new SimulationCommandTrace<AddCommand>(
            Enumerable.Range(1, 500)
                .Where(step => step % 7 == 0 || step % 11 == 0)
                .Select(step => new SimulationCommandTraceEntry<AddCommand>(step, new AddCommand((step % 9) - 4))));

        var first = SimulationReplayRunner.ReplayToStep(CreateRuntime(), trace, 500);
        var second = SimulationReplayRunner.ReplayToStep(CreateRuntime(), trace, 500);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ScenarioHarness_CapturesInitialAndEveryCommittedStep()
    {
        var harness = new DeterministicScenarioHarness<CounterState, AddCommand, CounterSnapshot>(CreateRuntime());
        var trace = new SimulationCommandTrace<AddCommand>(
            [new(2, new AddCommand(5))]);

        var result = harness.Run(trace, 4);

        Assert.Equal(5, result.Snapshots.Count);
        Assert.Equal(new long[] { 0, 1, 2, 3, 4 }, result.Snapshots.Select(snapshot => snapshot.Runtime.StepIndex));
        Assert.Equal(result.Snapshots[^1], result.FinalSnapshot);
        Assert.Equal(9, result.FinalSnapshot.State.Value);
    }

    [Fact]
    public void Replay_RejectsRunningRuntimeOrHiddenPendingCommands()
    {
        var running = CreateRuntime();
        running.Resume();

        Assert.Throws<InvalidOperationException>(() =>
            SimulationReplayRunner.ReplayToStep(
                running,
                new SimulationCommandTrace<AddCommand>([]),
                1));

        var queued = CreateRuntime();
        queued.EnqueueCommand(new AddCommand(1));

        Assert.Throws<InvalidOperationException>(() =>
            SimulationReplayRunner.ReplayToStep(
                queued,
                new SimulationCommandTrace<AddCommand>([]),
                1));
    }

    private static SimulationRuntime<CounterState, AddCommand, CounterSnapshot> CreateRuntime()
    {
        return new SimulationRuntime<CounterState, AddCommand, CounterSnapshot>(
            TimeSpan.FromMilliseconds(20),
            new CounterState(0, []),
            new CounterKernel());
    }

    private sealed record AddCommand(int Amount);

    private sealed record CounterState(int Value, IReadOnlyList<string> ProcessedCommands);

    private sealed record CounterSnapshot(int Value, string ProcessedCommands);

    private sealed class CounterKernel : ISimulationKernel<CounterState, AddCommand, CounterSnapshot>
    {
        public CounterState Step(
            CounterState state,
            IReadOnlyList<QueuedSimulationCommand<AddCommand>> commands,
            SimulationStepContext context)
        {
            var value = checked(state.Value + 1);
            var processedCommands = state.ProcessedCommands.ToList();

            foreach (var command in commands)
            {
                value = checked(value + command.Command.Amount);
                processedCommands.Add($"{command.Sequence}:{command.Command.Amount}");
            }

            return new CounterState(value, processedCommands.AsReadOnly());
        }

        public CounterSnapshot CreateSnapshot(CounterState state)
        {
            return new CounterSnapshot(state.Value, string.Join(",", state.ProcessedCommands));
        }
    }
}
