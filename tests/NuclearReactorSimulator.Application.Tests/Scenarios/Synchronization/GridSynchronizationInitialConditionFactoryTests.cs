using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization;

public sealed class GridSynchronizationInitialConditionFactoryTests
{
    [Fact]
    public void Factory_ReconstructsMatchedPreSynchronizationHandoffThroughCanonicalOwners()
    {
        var factory = new GridSynchronizationInitialConditionFactory();

        var left = factory.CreateRuntimeEngine();
        var right = factory.CreateRuntimeEngine();
        var snapshot = left.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generator = Assert.Single(snapshot.Electrical.Generators);
        var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);

        Assert.NotSame(left, right);
        Assert.Equal(GridSynchronizationLoadProgram.InitialCondition, factory.Descriptor.Reference);
        Assert.Equal(0, snapshot.LogicalStep);
        Assert.False(snapshot.AnyTripActive);
        Assert.InRange(rotor.Speed.NumericValue ?? double.NaN, 2_990d, 3_010d);
        Assert.True(generator.SynchronizationConditionsSatisfied);
        Assert.False(generator.BreakerClosed);
        Assert.InRange(Math.Abs(generator.ElectricalOutput.NumericValue ?? double.NaN), 0d, 0.001d);
    }

    [Fact]
    public void InitialChecklist_ConfirmsSynchronizationWindowButNotParallelOrLoadedState()
    {
        var snapshot = new GridSynchronizationInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var results = new GridSynchronizationChecklistEvaluator()
            .Evaluate(snapshot, GridSynchronizationLoadProgram.Guidance.Checks)
            .ToDictionary(static result => result.Definition.CheckId, StringComparer.Ordinal);

        Assert.True(results["signals-healthy"].IsSatisfied);
        Assert.True(results["protection-clear"].IsSatisfied);
        Assert.True(results["mcp-running"].IsSatisfied);
        Assert.True(results["reactor-power"].IsSatisfied);
        Assert.True(results["sync-speed"].IsSatisfied);
        Assert.True(results["sync-window"].IsSatisfied);
        Assert.True(results["breakers-open"].IsSatisfied);
        Assert.True(results["generator-unloaded"].IsSatisfied);
        Assert.False(results["breakers-closed"].IsSatisfied);
        Assert.False(results["initial-load"].IsSatisfied);
    }

    [Fact]
    public void BreakerCloseAndLoadRaise_UseCanonicalM45InputsAndProduceElectricalOutput()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new GridSynchronizationInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(GridSynchronizationLoadProgram.Scenario);
        var initialGenerator = Assert.Single(session.Coordinator.Current.Electrical.Generators);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            initialGenerator.BreakerId,
            ControlRoomCommandTargetKind.Breaker));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var paralleled = Assert.Single(session.Coordinator.Current.Electrical.Generators);
        Assert.True(paralleled.CloseCommandAccepted);
        Assert.False(paralleled.CloseCommandRejected);
        Assert.True(paralleled.BreakerClosed);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            paralleled.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var loaded = Assert.Single(session.Coordinator.Current.Electrical.Generators);
        Assert.True(loaded.BreakerClosed);
        Assert.InRange(loaded.ElectricalOutput.NumericValue ?? double.NaN, 4.9d, 5.1d);
    }
}
