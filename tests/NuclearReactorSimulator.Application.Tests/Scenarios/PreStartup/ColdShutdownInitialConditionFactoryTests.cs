using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.PreStartup;

public sealed class ColdShutdownInitialConditionFactoryTests
{
    [Fact]
    public void Factory_ReconstructsFreshColdShutdownRuntimeWithCanonicalObservableConditions()
    {
        var factory = new ColdShutdownInitialConditionFactory();

        var left = factory.CreateRuntimeEngine();
        var right = factory.CreateRuntimeEngine();
        var snapshot = left.CreatePresentationSnapshot(ControlRoomRunState.Paused);

        Assert.NotSame(left, right);
        Assert.Equal(ColdShutdownPreStartupProgram.InitialCondition, factory.Descriptor.Reference);
        Assert.Equal(0, snapshot.LogicalStep);
        Assert.Equal(ControlRoomRunState.Paused, snapshot.RunState);
        Assert.Equal(0, snapshot.InvalidMeasuredSignalCount);
        Assert.False(snapshot.AnyTripActive);
        Assert.All(snapshot.ReactorCore.Rods, static rod => Assert.Equal(0d, rod.PercentWithdrawn, 8));
        Assert.All(snapshot.PrimaryCircuit.Pumps, static pump => Assert.False(pump.IsRunning));
        Assert.All(snapshot.TurbineSecondary.Rotors, static rotor => Assert.InRange(Math.Abs(rotor.Speed.NumericValue ?? double.NaN), 0d, 1e-8d));
        Assert.All(snapshot.Electrical.Generators, static generator => Assert.False(generator.BreakerClosed));
        Assert.All(snapshot.TurbineSecondary.AdmissionTrains, static train =>
        {
            Assert.InRange(Math.Abs(train.StopValvePosition.NumericValue ?? double.NaN), 0d, 1e-8d);
            Assert.InRange(Math.Abs(train.ControlValvePosition.NumericValue ?? double.NaN), 0d, 1e-8d);
            Assert.InRange(Math.Abs(train.AdmissionValvePosition.NumericValue ?? double.NaN), 0d, 1e-8d);
        });
    }

    [Fact]
    public void ScenarioSession_AllowsPreStartPumpPreparationButRejectsFirstCriticalityActions()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new ColdShutdownInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(ColdShutdownPreStartupProgram.Scenario);
        var pump = Assert.Single(session.Coordinator.Current.PrimaryCircuit.Pumps);

        Assert.Throws<InvalidOperationException>(() => session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodWithdraw,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup)));

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.MainCirculationPumpStart,
            pump.PumpId,
            ControlRoomCommandTargetKind.Pump));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        Assert.True(Assert.Single(session.Coordinator.Current.PrimaryCircuit.Pumps).IsRunning);
        Assert.False(session.Coordinator.Current.AnyTripActive);
        Assert.All(session.Coordinator.Current.ReactorCore.Rods, static rod => Assert.Equal(0d, rod.PercentWithdrawn, 8));
        Assert.All(session.Coordinator.Current.Electrical.Generators, static generator => Assert.False(generator.BreakerClosed));
    }

    [Fact]
    public void BaselineChecklist_IsSatisfiedBeforePreparationExceptForRunningCirculationCheck()
    {
        var factory = new ColdShutdownInitialConditionFactory();
        var snapshot = factory.CreateRuntimeEngine().CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var evaluator = new PreStartupChecklistEvaluator();
        var results = evaluator.Evaluate(snapshot, ColdShutdownPreStartupProgram.Guidance.Checks)
            .ToDictionary(static result => result.Definition.CheckId, StringComparer.Ordinal);

        Assert.True(results["signals-healthy"].IsSatisfied);
        Assert.True(results["protection-clear"].IsSatisfied);
        Assert.True(results["reactor-shutdown"].IsSatisfied);
        Assert.True(results["rods-inserted"].IsSatisfied);
        Assert.True(results["mcp-stopped"].IsSatisfied);
        Assert.False(results["mcp-running"].IsSatisfied);
        Assert.True(results["turbine-stopped"].IsSatisfied);
        Assert.True(results["breakers-open"].IsSatisfied);
        Assert.True(results["steam-isolated"].IsSatisfied);
        Assert.True(results["alarms-clear"].IsSatisfied);
    }
}
