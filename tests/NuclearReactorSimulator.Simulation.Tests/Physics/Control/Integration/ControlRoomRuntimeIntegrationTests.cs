using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Domain.Physics.Control;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Control.Integration;

public sealed class ControlRoomRuntimeIntegrationTests
{
    [Fact]
    public void RuntimeEngine_TranslatesScramAndAlarmAcknowledgeThroughValidatedM5Path()
    {
        var fixture = IntegratedAutomaticOperationTests.CreateFixture();
        var solver = IntegratedAutomaticOperationTests.CreateSolver(fixture);
        var seed = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var engine = new IntegratedAutomaticOperationRuntimeEngine(
            solver,
            seed.CandidateState,
            fixture.Inputs,
            seed.Snapshot,
            TimeSpan.FromMilliseconds(1d),
            initialLogicalStep: 1);

        engine.QueueOperatorCommand(new ControlRoomCommand(ControlRoomCommandKind.ReactorScram));
        var scram = engine.Step(ControlRoomRunState.Paused);

        Assert.True(scram.ReactorScramActive);
        var alarm = scram.AlarmEvents.Alarms.Single(static item => item.AlarmId == "scram-active");
        Assert.True(alarm.IsAnnunciated);
        Assert.False(alarm.IsAcknowledged);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.AlarmAcknowledge,
            "scram-active",
            ControlRoomCommandTargetKind.Alarm));
        var acknowledged = engine.Step(ControlRoomRunState.Paused);
        var acknowledgedAlarm = acknowledged.AlarmEvents.Alarms.Single(static item => item.AlarmId == "scram-active");

        Assert.True(acknowledged.ReactorScramActive);
        Assert.True(acknowledgedAlarm.IsAcknowledged);
    }

    [Fact]
    public void RuntimeEngine_MapsManualRodPumpAndSpeedCommandsToCanonicalControllerInputs()
    {
        var fixture = IntegratedAutomaticOperationTests.CreateFixture();
        var solver = IntegratedAutomaticOperationTests.CreateSolver(fixture);
        var seed = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var engine = new IntegratedAutomaticOperationRuntimeEngine(
            solver,
            seed.CandidateState,
            fixture.Inputs,
            seed.Snapshot,
            TimeSpan.FromMilliseconds(1d),
            initialLogicalStep: 1);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodInsert,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup));
        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.MainCirculationPumpStart,
            "pump",
            ControlRoomCommandTargetKind.Pump));
        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedRaise,
            "rotor",
            ControlRoomCommandTargetKind.TurbineRotor));

        var reactorControllers = engine.PersistentInputs.ReactorPrimaryInputs.Controllers;
        var secondaryControllers = engine.PersistentInputs.TurbineSecondaryInputs.Controllers;

        Assert.Equal(ControllerMode.Manual, reactorControllers.GetController("power-control").Mode);
        Assert.Equal(-1d, reactorControllers.GetController("power-control").ManualOutput, 8);
        Assert.Equal(ControllerMode.Manual, reactorControllers.GetController("flow-control").Mode);
        Assert.Equal(100d, reactorControllers.GetController("flow-control").ManualOutput, 8);
        Assert.Equal(
            fixture.Inputs.TurbineSecondaryInputs.Controllers.GetController("speed-control").Setpoint + 10d,
            secondaryControllers.GetController("speed-control").Setpoint,
            8);
    }

    [Fact]
    public void RuntimeCoordinator_PresentationPublicationStrideDoesNotChangePhysicalResult()
    {
        var denseFixture = CreateRuntime();
        var sparseFixture = CreateRuntime();

        denseFixture.Coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        sparseFixture.Coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = denseFixture.Coordinator.AdvanceRunning(8, publicationStride: 1);
        _ = sparseFixture.Coordinator.AdvanceRunning(8, publicationStride: 4);

        var denseInventory = denseFixture.Engine.CurrentState.PlantState.PlantState.GetFluidNode("drum").Inventory;
        var sparseInventory = sparseFixture.Engine.CurrentState.PlantState.PlantState.GetFluidNode("drum").Inventory;

        Assert.Equal(denseFixture.Engine.LogicalStep, sparseFixture.Engine.LogicalStep);
        Assert.Equal(denseInventory, sparseInventory);
        Assert.Equal(
            denseFixture.Engine.CurrentState.ReactorPrimaryControlState.ControlRods.GetRod("rod-1").Position,
            sparseFixture.Engine.CurrentState.ReactorPrimaryControlState.ControlRods.GetRod("rod-1").Position);
    }

    private static RuntimeFixture CreateRuntime()
    {
        var fixture = IntegratedAutomaticOperationTests.CreateFixture();
        var solver = IntegratedAutomaticOperationTests.CreateSolver(fixture);
        var seed = solver.Step(fixture.State, fixture.Inputs, TimeSpan.FromMilliseconds(1d));
        var engine = new IntegratedAutomaticOperationRuntimeEngine(
            solver,
            seed.CandidateState,
            fixture.Inputs,
            seed.Snapshot,
            TimeSpan.FromMilliseconds(1d),
            initialLogicalStep: 1);
        return new RuntimeFixture(engine, new ControlRoomRuntimeCoordinator(engine));
    }

    private sealed record RuntimeFixture(
        IntegratedAutomaticOperationRuntimeEngine Engine,
        ControlRoomRuntimeCoordinator Coordinator);
}
