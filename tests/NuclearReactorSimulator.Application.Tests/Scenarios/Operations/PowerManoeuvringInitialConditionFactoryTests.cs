using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Operations;

public sealed class PowerManoeuvringInitialConditionFactoryTests
{
    [Fact]
    public void Factory_ReconstructsStableLowLoadParallelHandoffThroughCanonicalOwners()
    {
        var factory = new PowerManoeuvringInitialConditionFactory();

        var left = factory.CreateRuntimeEngine();
        var right = factory.CreateRuntimeEngine();
        var snapshot = left.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generator = Assert.Single(snapshot.Electrical.Generators);
        var rotor = Assert.Single(snapshot.TurbineSecondary.Rotors);

        Assert.NotSame(left, right);
        Assert.Equal(PowerManoeuvringNormalShutdownProgram.InitialCondition, factory.Descriptor.Reference);
        Assert.Equal(0, snapshot.LogicalStep);
        Assert.False(snapshot.AnyTripActive);
        Assert.True(generator.BreakerClosed);
        Assert.InRange(rotor.Speed.NumericValue ?? double.NaN, 2_980d, 3_020d);
        Assert.InRange(generator.ElectricalOutput.NumericValue ?? double.NaN, 4.8d, 5.2d);
        Assert.True((snapshot.ReactorCore.ReactorThermalPower.NumericValue ?? 0d) > 5d);
    }

    [Fact]
    public void InitialChecklist_ConfirmsLowLoadHandoffAndExplicitXenonBoundary()
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var results = new PowerManoeuvringChecklistEvaluator()
            .Evaluate(snapshot, PowerManoeuvringNormalShutdownProgram.Guidance.Checks)
            .ToDictionary(static result => result.Definition.CheckId, StringComparer.Ordinal);

        Assert.True(results["signals-healthy"].IsSatisfied);
        Assert.True(results["protection-clear"].IsSatisfied);
        Assert.True(results["mcp-running"].IsSatisfied);
        Assert.True(results["breakers-closed"].IsSatisfied);
        Assert.True(results["low-load"].IsSatisfied);
        Assert.True(results["temperature-feedback"].IsSatisfied);
        Assert.True(results["xenon-boundary"].IsSatisfied);
        Assert.False(results["generator-unloaded"].IsSatisfied);
        Assert.False(results["breakers-open"].IsSatisfied);
        Assert.False(results["reactor-shutdown"].IsSatisfied);
    }

    [Fact]
    public void LoadRaiseLowerAndDisconnect_UseCanonicalCommandSeams()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        var initialGenerator = Assert.Single(session.Coordinator.Current.Electrical.Generators);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            initialGenerator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.InRange(session.Coordinator.Current.Electrical.GrossElectricalOutput.NumericValue ?? double.NaN, 9.5d, 10.5d);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            initialGenerator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.InRange(session.Coordinator.Current.Electrical.GrossElectricalOutput.NumericValue ?? double.NaN, 4.5d, 5.5d);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            initialGenerator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.InRange(Math.Abs(session.Coordinator.Current.Electrical.GrossElectricalOutput.NumericValue ?? double.NaN), 0d, 0.001d);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerOpen,
            initialGenerator.BreakerId,
            ControlRoomCommandTargetKind.Breaker));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        Assert.False(Assert.Single(session.Coordinator.Current.Electrical.Generators).BreakerClosed);
    }

    [Fact]
    public void NormalShutdown_RodInsertUsesExistingControlRodSeamWithoutScram()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        var target = Assert.Single(session.Coordinator.Current.ReactorCore.RodTargets);
        var before = session.Coordinator.Current.ReactorCore.AverageRodWithdrawal.NumericValue ?? double.NaN;

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodInsert,
            target.TargetId,
            target.TargetKind));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var after = session.Coordinator.Current.ReactorCore.AverageRodWithdrawal.NumericValue ?? double.NaN;
        Assert.True(after < before);
        Assert.False(session.Coordinator.Current.ReactorCore.ReactorScramActive);
    }
}
