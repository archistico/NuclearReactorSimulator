using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ControlRoomRuntimeCoordinatorTests
{
    [Fact]
    public void Dispatch_RunPauseAndSingleStepOwnRuntimeStateWithoutBypassingEngine()
    {
        var engine = new FakeRuntimeEngine();
        var coordinator = new ControlRoomRuntimeCoordinator(engine);

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        Assert.Equal(ControlRoomRunState.Running, coordinator.RunState);
        Assert.Equal(0, engine.StepCount);

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        Assert.Equal(ControlRoomRunState.Paused, coordinator.RunState);
        Assert.Equal(0, engine.StepCount);

        coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.Equal(ControlRoomRunState.Paused, coordinator.RunState);
        Assert.Equal(1, engine.StepCount);
        Assert.Equal(1, coordinator.Current.LogicalStep);
    }

    [Fact]
    public void AdvanceRunning_PublicationStrideChangesOnlyPresentationTraffic()
    {
        var denseEngine = new FakeRuntimeEngine();
        var dense = new ControlRoomRuntimeCoordinator(denseEngine);
        var denseCompletedSteps = 0;
        dense.DeterministicStepCompleted += (_, _) => denseCompletedSteps++;
        dense.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var denseResult = dense.AdvanceRunning(25, publicationStride: 1);

        var sparseEngine = new FakeRuntimeEngine();
        var sparse = new ControlRoomRuntimeCoordinator(sparseEngine);
        var sparseCompletedSteps = 0;
        sparse.DeterministicStepCompleted += (_, _) => sparseCompletedSteps++;
        sparse.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var sparseResult = sparse.AdvanceRunning(25, publicationStride: 10);

        Assert.Equal(25, denseEngine.StepCount);
        Assert.Equal(25, sparseEngine.StepCount);
        Assert.Equal(25, denseCompletedSteps);
        Assert.Equal(25, sparseCompletedSteps);
        Assert.Equal(dense.Current.LogicalStep, sparse.Current.LogicalStep);
        Assert.Equal(25, denseResult.PublishedSnapshotCount);
        Assert.Equal(3, sparseResult.PublishedSnapshotCount);
    }

    [Fact]
    public void Dispatch_PlantCommandIsForwardedToRuntimeEngineWithoutStepping()
    {
        var engine = new FakeRuntimeEngine();
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var command = new ControlRoomCommand(
            ControlRoomCommandKind.MainCirculationPumpStart,
            "pump-a",
            ControlRoomCommandTargetKind.Pump);

        coordinator.Dispatch(command);

        Assert.Equal(0, engine.StepCount);
        Assert.Same(command, Assert.Single(engine.Commands));
    }

    private sealed class FakeRuntimeEngine : IControlRoomRuntimeEngine
    {
        public int StepCount { get; private set; }
        public long LogicalStep => StepCount;
        public List<ControlRoomCommand> Commands { get; } = new();

        public ControlRoomSnapshot CreatePresentationSnapshot(ControlRoomRunState runState)
            => Snapshot(runState);

        public ControlRoomSnapshot Step(ControlRoomRunState runState)
        {
            StepCount++;
            return Snapshot(runState);
        }

        public void QueueOperatorCommand(ControlRoomCommand command)
            => Commands.Add(command);

        private ControlRoomSnapshot Snapshot(ControlRoomRunState runState)
            => new(
                StepCount,
                runState,
                0,
                0,
                0,
                0,
                false,
                false,
                false);
    }
}
