using NuclearReactorSimulator.Simulation.Runtime;
using NuclearReactorSimulator.Simulation.Runtime.Invariants;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Runtime;

public sealed class SimulationRuntimeHardeningTests
{
    private static readonly TimeSpan FixedStep = TimeSpan.FromMilliseconds(20);

    [Fact]
    public void KernelFault_DoesNotCommitStateClockOrCommands()
    {
        var runtime = new SimulationRuntime<TestState, TestCommand, TestSnapshot>(
            FixedStep,
            new TestState(10),
            new FaultingKernel(failAtStep: 1));

        runtime.EnqueueCommand(new TestCommand(5));

        var exception = Assert.Throws<SimulationRuntimeFaultException>(() => runtime.StepOnce());
        var snapshot = runtime.GetSnapshot();

        Assert.Equal(1L, exception.Fault.FailedStepIndex);
        Assert.Equal(TimeSpan.Zero, exception.Fault.FailedStepStartTime);
        Assert.Equal(SimulationRunState.Faulted, snapshot.Runtime.RunState);
        Assert.Equal(0L, snapshot.Runtime.StepIndex);
        Assert.Equal(TimeSpan.Zero, snapshot.Runtime.ElapsedSimulationTime);
        Assert.Equal(1, snapshot.Runtime.PendingCommandCount);
        Assert.Equal(10, snapshot.State.Value);
        Assert.NotNull(snapshot.Runtime.Fault);
        Assert.Contains(nameof(InvalidOperationException), snapshot.Runtime.Fault!.ExceptionType);
    }

    [Fact]
    public void FaultedRuntime_RejectsFurtherMutationOrExecution()
    {
        var runtime = new SimulationRuntime<TestState, TestCommand, TestSnapshot>(
            FixedStep,
            new TestState(0),
            new FaultingKernel(failAtStep: 1));

        Assert.Throws<SimulationRuntimeFaultException>(() => runtime.StepOnce());

        Assert.Throws<InvalidOperationException>(() => runtime.EnqueueCommand(new TestCommand(1)));
        Assert.Throws<InvalidOperationException>(() => runtime.Pause());
        Assert.Throws<InvalidOperationException>(() => runtime.Resume());
        Assert.Throws<InvalidOperationException>(() => runtime.SetSpeed(SimulationSpeed.Double));
        Assert.Throws<InvalidOperationException>(() => runtime.Advance(TimeSpan.Zero));
        Assert.Throws<InvalidOperationException>(() => runtime.StepOnce());

        Assert.Equal(SimulationRunState.Faulted, runtime.GetSnapshot().Runtime.RunState);
    }

    [Fact]
    public void InvariantViolation_PreventsCandidateStateFromBeingCommitted()
    {
        var invariant = new MaximumValueInvariant(5);
        var runtime = new SimulationRuntime<TestState, TestCommand, TestSnapshot>(
            FixedStep,
            new TestState(5),
            new IncrementKernel(),
            [invariant]);

        var exception = Assert.Throws<SimulationRuntimeFaultException>(() => runtime.StepOnce());
        var snapshot = runtime.GetSnapshot();
        var violation = Assert.IsType<SimulationInvariantViolationException>(exception.InnerException);

        Assert.Equal("MaximumValue", violation.InvariantName);
        Assert.Equal(1L, violation.StepIndex);
        Assert.Equal(5, snapshot.State.Value);
        Assert.Equal(0L, snapshot.Runtime.StepIndex);
        Assert.Equal(SimulationRunState.Faulted, snapshot.Runtime.RunState);
    }

    [Fact]
    public void Invariants_RunAfterKernelAndBeforeCommit()
    {
        var observed = new RecordingInvariant();
        var runtime = new SimulationRuntime<TestState, TestCommand, TestSnapshot>(
            FixedStep,
            new TestState(2),
            new IncrementKernel(),
            [observed]);

        var snapshot = runtime.StepOnce();

        Assert.Equal(3, observed.ObservedValue);
        Assert.Equal(1L, observed.ObservedStepIndex);
        Assert.Equal(3, snapshot.State.Value);
        Assert.Equal(1L, snapshot.Runtime.StepIndex);
    }


    [Fact]
    public void SnapshotProjectionFault_DoesNotCommitCandidateStateOrClock()
    {
        var runtime = new SimulationRuntime<TestState, TestCommand, TestSnapshot>(
            FixedStep,
            new TestState(0),
            new SnapshotFaultingKernel(failForValue: 1));

        var exception = Assert.Throws<SimulationRuntimeFaultException>(() => runtime.StepOnce());
        var snapshot = runtime.GetSnapshot();

        Assert.Contains("snapshot projection", exception.InnerException!.Message);
        Assert.Equal(0L, snapshot.Runtime.StepIndex);
        Assert.Equal(0, snapshot.State.Value);
        Assert.Equal(SimulationRunState.Faulted, snapshot.Runtime.RunState);
    }

    [Fact]
    public void GetSnapshot_UsesCommittedCachedProjectionWithoutReinvokingKernel()
    {
        var kernel = new CountingSnapshotKernel();
        var runtime = new SimulationRuntime<TestState, TestCommand, TestSnapshot>(
            FixedStep,
            new TestState(0),
            kernel);

        runtime.GetSnapshot();
        runtime.GetSnapshot();
        runtime.GetSnapshot();
        Assert.Equal(1, kernel.SnapshotCalls);

        runtime.StepOnce();
        runtime.GetSnapshot();
        runtime.GetSnapshot();
        Assert.Equal(2, kernel.SnapshotCalls);
    }

    [Fact]
    public void FaultDuringMultiStepAdvance_CommitsOnlyPreviouslyCompletedSteps()
    {
        var runtime = new SimulationRuntime<TestState, TestCommand, TestSnapshot>(
            FixedStep,
            new TestState(0),
            new FaultingKernel(failAtStep: 3));
        runtime.Resume();

        Assert.Throws<SimulationRuntimeFaultException>(() => runtime.Advance(TimeSpan.FromMilliseconds(100)));
        var snapshot = runtime.GetSnapshot();

        Assert.Equal(2L, snapshot.Runtime.StepIndex);
        Assert.Equal(TimeSpan.FromMilliseconds(40), snapshot.Runtime.ElapsedSimulationTime);
        Assert.Equal(2, snapshot.State.Value);
        Assert.Equal(3L, snapshot.Runtime.Fault!.FailedStepIndex);
    }

    private sealed record TestCommand(int Amount);

    private sealed record TestState(int Value);

    private sealed record TestSnapshot(int Value);

    private sealed class IncrementKernel : ISimulationKernel<TestState, TestCommand, TestSnapshot>
    {
        public TestState Step(
            TestState state,
            IReadOnlyList<QueuedSimulationCommand<TestCommand>> commands,
            SimulationStepContext context)
        {
            var commandDelta = commands.Sum(command => command.Command.Amount);
            return new TestState(checked(state.Value + 1 + commandDelta));
        }

        public TestSnapshot CreateSnapshot(TestState state) => new(state.Value);
    }

    private sealed class FaultingKernel : ISimulationKernel<TestState, TestCommand, TestSnapshot>
    {
        private readonly long _failAtStep;

        public FaultingKernel(long failAtStep)
        {
            _failAtStep = failAtStep;
        }

        public TestState Step(
            TestState state,
            IReadOnlyList<QueuedSimulationCommand<TestCommand>> commands,
            SimulationStepContext context)
        {
            if (context.StepIndex == _failAtStep)
            {
                throw new InvalidOperationException("Synthetic deterministic kernel fault.");
            }

            var commandDelta = commands.Sum(command => command.Command.Amount);
            return new TestState(checked(state.Value + 1 + commandDelta));
        }

        public TestSnapshot CreateSnapshot(TestState state) => new(state.Value);
    }


    private sealed class SnapshotFaultingKernel : ISimulationKernel<TestState, TestCommand, TestSnapshot>
    {
        private readonly int _failForValue;

        public SnapshotFaultingKernel(int failForValue)
        {
            _failForValue = failForValue;
        }

        public TestState Step(
            TestState state,
            IReadOnlyList<QueuedSimulationCommand<TestCommand>> commands,
            SimulationStepContext context)
        {
            return new TestState(checked(state.Value + 1));
        }

        public TestSnapshot CreateSnapshot(TestState state)
        {
            if (state.Value == _failForValue)
            {
                throw new InvalidOperationException("Synthetic snapshot projection fault.");
            }

            return new TestSnapshot(state.Value);
        }
    }

    private sealed class CountingSnapshotKernel : ISimulationKernel<TestState, TestCommand, TestSnapshot>
    {
        public int SnapshotCalls { get; private set; }

        public TestState Step(
            TestState state,
            IReadOnlyList<QueuedSimulationCommand<TestCommand>> commands,
            SimulationStepContext context)
        {
            return new TestState(checked(state.Value + 1));
        }

        public TestSnapshot CreateSnapshot(TestState state)
        {
            SnapshotCalls++;
            return new TestSnapshot(state.Value);
        }
    }

    private sealed class MaximumValueInvariant : ISimulationInvariant<TestState>
    {
        private readonly int _maximum;

        public MaximumValueInvariant(int maximum)
        {
            _maximum = maximum;
        }

        public string Name => "MaximumValue";

        public SimulationInvariantResult Evaluate(TestState state, SimulationStepContext context)
        {
            return state.Value <= _maximum
                ? SimulationInvariantResult.Satisfied()
                : SimulationInvariantResult.Violated($"Value {state.Value} exceeded {_maximum}.");
        }
    }

    private sealed class RecordingInvariant : ISimulationInvariant<TestState>
    {
        public string Name => "RecordingInvariant";

        public int ObservedValue { get; private set; }

        public long ObservedStepIndex { get; private set; }

        public SimulationInvariantResult Evaluate(TestState state, SimulationStepContext context)
        {
            ObservedValue = state.Value;
            ObservedStepIndex = context.StepIndex;
            return SimulationInvariantResult.Satisfied();
        }
    }
}
