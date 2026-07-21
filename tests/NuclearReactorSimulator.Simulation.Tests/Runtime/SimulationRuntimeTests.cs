using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Runtime;

public sealed class SimulationRuntimeTests
{
    private static readonly TimeSpan FixedStep = TimeSpan.FromMilliseconds(20);

    [Fact]
    public void NewRuntime_StartsPausedAtStepZero()
    {
        var runtime = CreateRuntime();

        var snapshot = runtime.GetSnapshot();

        Assert.Equal(SimulationRunState.Paused, snapshot.Runtime.RunState);
        Assert.Equal(0L, snapshot.Runtime.StepIndex);
        Assert.Equal(TimeSpan.Zero, snapshot.Runtime.ElapsedSimulationTime);
        Assert.Equal(FixedStep, snapshot.Runtime.FixedTimeStep);
        Assert.Equal(SimulationSpeed.Normal, snapshot.Runtime.Speed);
    }

    [Fact]
    public void Advance_WhilePaused_DoesNotAdvanceSimulation()
    {
        var runtime = CreateRuntime();

        var result = runtime.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(0L, result.StepsExecuted);
        Assert.Equal(0L, result.Snapshot.Runtime.StepIndex);
        Assert.Equal(0, result.Snapshot.State.Value);
    }

    [Fact]
    public void Advance_WhenRunning_UsesOnlyFixedTimesteps()
    {
        var runtime = CreateRuntime();
        runtime.Resume();

        var first = runtime.Advance(TimeSpan.FromMilliseconds(19));
        var second = runtime.Advance(TimeSpan.FromMilliseconds(1));

        Assert.Equal(0L, first.StepsExecuted);
        Assert.Equal(1L, second.StepsExecuted);
        Assert.Equal(1L, second.Snapshot.Runtime.StepIndex);
        Assert.Equal(FixedStep, second.Snapshot.Runtime.ElapsedSimulationTime);
        Assert.Equal(1, second.Snapshot.State.Value);
    }

    [Fact]
    public void Advance_WithDifferentPulseSegmentation_ProducesSameResult()
    {
        var singlePulse = CreateRuntime();
        var irregularPulses = CreateRuntime();
        singlePulse.Resume();
        irregularPulses.Resume();

        singlePulse.Advance(TimeSpan.FromSeconds(2));

        var pulses = new[] { 17, 83, 1, 299, 400, 53, 647, 500 };
        foreach (var milliseconds in pulses)
        {
            irregularPulses.Advance(TimeSpan.FromMilliseconds(milliseconds));
        }

        Assert.Equal(singlePulse.GetSnapshot(), irregularPulses.GetSnapshot());
    }

    [Theory]
    [MemberData(nameof(SpeedCases))]
    public void Advance_AppliesSupportedSpeedExactly(SimulationSpeed speed, int expectedSteps)
    {
        var runtime = CreateRuntime();
        runtime.SetSpeed(speed);
        runtime.Resume();

        var result = runtime.Advance(TimeSpan.FromMilliseconds(80));

        Assert.Equal((long)expectedSteps, result.StepsExecuted);
        Assert.Equal(expectedSteps, result.Snapshot.State.Value);
    }

    [Fact]
    public void QuarterSpeed_PreservesFractionalScaledTicksAcrossPulses()
    {
        var onePulse = CreateRuntime(TimeSpan.FromTicks(1));
        var fourPulses = CreateRuntime(TimeSpan.FromTicks(1));
        onePulse.SetSpeed(SimulationSpeed.Quarter);
        fourPulses.SetSpeed(SimulationSpeed.Quarter);
        onePulse.Resume();
        fourPulses.Resume();

        onePulse.Advance(TimeSpan.FromTicks(4));
        for (var index = 0; index < 4; index++)
        {
            fourPulses.Advance(TimeSpan.FromTicks(1));
        }

        Assert.Equal(onePulse.GetSnapshot(), fourPulses.GetSnapshot());
        Assert.Equal(1L, fourPulses.GetSnapshot().Runtime.StepIndex);
    }

    [Fact]
    public void Pause_FreezesSimulationUntilResumed()
    {
        var runtime = CreateRuntime();
        runtime.Resume();
        runtime.Advance(TimeSpan.FromMilliseconds(40));
        runtime.Pause();

        runtime.Advance(TimeSpan.FromSeconds(1));
        var paused = runtime.GetSnapshot();
        runtime.Resume();
        runtime.Advance(TimeSpan.FromMilliseconds(20));
        var resumed = runtime.GetSnapshot();

        Assert.Equal(2L, paused.Runtime.StepIndex);
        Assert.Equal(3L, resumed.Runtime.StepIndex);
    }

    [Fact]
    public void StepOnce_WhenPaused_ExecutesExactlyOneStep()
    {
        var runtime = CreateRuntime();

        var snapshot = runtime.StepOnce();

        Assert.Equal(1L, snapshot.Runtime.StepIndex);
        Assert.Equal(1, snapshot.State.Value);
        Assert.Equal(SimulationRunState.Paused, snapshot.Runtime.RunState);
    }

    [Fact]
    public void StepOnce_WhenRunning_IsRejected()
    {
        var runtime = CreateRuntime();
        runtime.Resume();

        Assert.Throws<InvalidOperationException>(() => runtime.StepOnce());
    }

    [Fact]
    public void Commands_AreNumberedAndConsumedInFifoOrderAtNextStepBoundary()
    {
        var runtime = CreateRuntime();

        var firstSequence = runtime.EnqueueCommand(new AddCommand(7));
        var secondSequence = runtime.EnqueueCommand(new AddCommand(3));
        var queued = runtime.GetSnapshot();
        var stepped = runtime.StepOnce();
        var nextStep = runtime.StepOnce();

        Assert.Equal(1L, firstSequence);
        Assert.Equal(2L, secondSequence);
        Assert.Equal(2, queued.Runtime.PendingCommandCount);
        Assert.Equal(11, stepped.State.Value);
        Assert.Equal("1,2", stepped.State.ProcessedCommandSequences);
        Assert.Equal(0, stepped.Runtime.PendingCommandCount);
        Assert.Equal(12, nextStep.State.Value);
        Assert.Equal("1,2", nextStep.State.ProcessedCommandSequences);
    }

    [Fact]
    public void CommandSequenceAndPulseSequence_AreRepeatable()
    {
        var left = CreateRuntime();
        var right = CreateRuntime();

        ReplayDeterministicSequence(left);
        ReplayDeterministicSequence(right);

        Assert.Equal(left.GetSnapshot(), right.GetSnapshot());
    }

    [Fact]
    public void Advance_WithNegativeElapsedTime_IsRejected()
    {
        var runtime = CreateRuntime();

        Assert.Throws<ArgumentOutOfRangeException>(() => runtime.Advance(TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void Clock_RejectsNonPositiveFixedTimestep()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationClock(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationClock(TimeSpan.FromTicks(-1)));
    }

    public static TheoryData<SimulationSpeed, int> SpeedCases => new()
    {
        { SimulationSpeed.Quarter, 1 },
        { SimulationSpeed.Half, 2 },
        { SimulationSpeed.Normal, 4 },
        { SimulationSpeed.Double, 8 },
        { SimulationSpeed.FiveTimes, 20 },
        { SimulationSpeed.TenTimes, 40 },
    };

    private static SimulationRuntime<CounterState, AddCommand, CounterSnapshot> CreateRuntime(
        TimeSpan? fixedStep = null)
    {
        return new SimulationRuntime<CounterState, AddCommand, CounterSnapshot>(
            fixedStep ?? FixedStep,
            new CounterState(0, []),
            new CounterKernel());
    }

    private static void ReplayDeterministicSequence(
        SimulationRuntime<CounterState, AddCommand, CounterSnapshot> runtime)
    {
        runtime.EnqueueCommand(new AddCommand(10));
        runtime.StepOnce();
        runtime.SetSpeed(SimulationSpeed.Double);
        runtime.Resume();
        runtime.Advance(TimeSpan.FromMilliseconds(35));
        runtime.EnqueueCommand(new AddCommand(-4));
        runtime.Advance(TimeSpan.FromMilliseconds(25));
        runtime.Pause();
        runtime.EnqueueCommand(new AddCommand(8));
        runtime.StepOnce();
    }

    private sealed record AddCommand(int Amount);

    private sealed record CounterState(int Value, IReadOnlyList<long> ProcessedCommandSequences);

    private sealed record CounterSnapshot(int Value, string ProcessedCommandSequences);

    private sealed class CounterKernel : ISimulationKernel<CounterState, AddCommand, CounterSnapshot>
    {
        public CounterState Step(
            CounterState state,
            IReadOnlyList<QueuedSimulationCommand<AddCommand>> commands,
            SimulationStepContext context)
        {
            var value = checked(state.Value + 1);
            var processedSequences = state.ProcessedCommandSequences.ToList();

            foreach (var command in commands)
            {
                value = checked(value + command.Command.Amount);
                processedSequences.Add(command.Sequence);
            }

            Assert.Equal(context.StartTime + context.DeltaTime, context.EndTime);
            return new CounterState(value, processedSequences.AsReadOnly());
        }

        public CounterSnapshot CreateSnapshot(CounterState state)
        {
            return new CounterSnapshot(
                state.Value,
                string.Join(",", state.ProcessedCommandSequences));
        }
    }
}
