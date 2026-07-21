using NuclearReactorSimulator.Simulation.Runtime;
using NuclearReactorSimulator.Simulation.Runtime.Replay;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Runtime;

public sealed class SimulationLongRunDeterminismTests
{
    private static readonly TimeSpan FixedStep = TimeSpan.FromMilliseconds(20);

    [Fact]
    public void OneHundredThousandSteps_AreIndependentFromExternalPulseSegmentation()
    {
        const int stepCount = 100_000;
        var onePulse = CreateRuntime();
        var segmented = CreateRuntime();
        onePulse.Resume();
        segmented.Resume();

        var totalDuration = TimeSpan.FromTicks(checked(FixedStep.Ticks * stepCount));
        onePulse.Advance(totalDuration);

        const int pulseCount = 10_000;
        var pulseDuration = TimeSpan.FromTicks(totalDuration.Ticks / pulseCount);
        for (var index = 0; index < pulseCount; index++)
        {
            segmented.Advance(pulseDuration);
        }

        Assert.Equal(onePulse.GetSnapshot(), segmented.GetSnapshot());
        Assert.Equal((long)stepCount, onePulse.GetSnapshot().Runtime.StepIndex);
    }

    [Fact]
    public void LargeDeterministicCommandTrace_ReplaysIdentically()
    {
        const int finalStep = 20_000;
        var entries = new List<SimulationCommandTraceEntry<DeltaCommand>>();

        for (var step = 10; step <= finalStep; step += 10)
        {
            entries.Add(new SimulationCommandTraceEntry<DeltaCommand>(step, new DeltaCommand(step % 13)));
            entries.Add(new SimulationCommandTraceEntry<DeltaCommand>(step, new DeltaCommand(-(step % 5))));
        }

        var trace = new SimulationCommandTrace<DeltaCommand>(entries);
        var first = SimulationReplayRunner.ReplayToStep(CreateRuntime(), trace, finalStep);
        var second = SimulationReplayRunner.ReplayToStep(CreateRuntime(), trace, finalStep);

        Assert.Equal(first, second);
        Assert.Equal((long)finalStep, first.Runtime.StepIndex);
    }

    private static SimulationRuntime<LongState, DeltaCommand, LongSnapshot> CreateRuntime()
    {
        return new SimulationRuntime<LongState, DeltaCommand, LongSnapshot>(
            FixedStep,
            new LongState(0),
            new LongKernel());
    }

    private sealed record DeltaCommand(long Amount);

    private sealed record LongState(long Value);

    private sealed record LongSnapshot(long Value);

    private sealed class LongKernel : ISimulationKernel<LongState, DeltaCommand, LongSnapshot>
    {
        public LongState Step(
            LongState state,
            IReadOnlyList<QueuedSimulationCommand<DeltaCommand>> commands,
            SimulationStepContext context)
        {
            var next = checked(state.Value + 1);
            foreach (var command in commands)
            {
                next = checked(next + command.Command.Amount);
            }

            return new LongState(next);
        }

        public LongSnapshot CreateSnapshot(LongState state) => new(state.Value);
    }
}
