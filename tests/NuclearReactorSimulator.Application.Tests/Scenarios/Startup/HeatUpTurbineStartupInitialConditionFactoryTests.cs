using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Startup;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Startup;

public sealed class HeatUpTurbineStartupInitialConditionFactoryTests
{
    [Fact]
    public void Factory_ReconstructsWarmLowPowerStartupLineupThroughCanonicalOwners()
    {
        var factory = new HeatUpTurbineStartupInitialConditionFactory();

        var left = factory.CreateRuntimeEngine();
        var right = factory.CreateRuntimeEngine();
        var snapshot = left.CreatePresentationSnapshot(ControlRoomRunState.Paused);

        Assert.NotSame(left, right);
        Assert.Equal(HeatUpTurbineStartupProgram.InitialCondition, factory.Descriptor.Reference);
        Assert.Equal(0, snapshot.LogicalStep);
        Assert.Equal(ControlRoomRunState.Paused, snapshot.RunState);
        Assert.Equal(0, snapshot.InvalidMeasuredSignalCount);
        Assert.False(snapshot.AnyTripActive);
        Assert.All(snapshot.ReactorCore.Rods, static rod => Assert.InRange(rod.PercentWithdrawn, 49.9d, 50.1d));
        Assert.All(snapshot.PrimaryCircuit.Pumps, static pump => Assert.True(pump.IsRunning));
        Assert.InRange(snapshot.ReactorCore.ReactorThermalPower.NumericValue ?? double.NaN, 0.01d, 20d);
        Assert.All(snapshot.Electrical.Generators, static generator => Assert.False(generator.BreakerClosed));
        Assert.All(snapshot.TurbineSecondary.Rotors, static rotor =>
            Assert.InRange(Math.Abs(rotor.Speed.NumericValue ?? double.NaN), 0d, 1d));
        Assert.All(snapshot.TurbineSecondary.AdmissionTrains, static train =>
        {
            Assert.InRange(train.StopValvePosition.NumericValue ?? double.NaN, 99d, 100d);
            Assert.InRange(Math.Abs(train.ControlValvePosition.NumericValue ?? double.NaN), 0d, 0.1d);
            Assert.InRange(train.AdmissionValvePosition.NumericValue ?? double.NaN, 99d, 100d);
        });
    }

    [Fact]
    public void InitialChecklist_ConfirmsStartupHandoffButNotTurbineRollingOrSynchronizationSpeed()
    {
        var snapshot = new HeatUpTurbineStartupInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var evaluator = new HeatUpTurbineStartupChecklistEvaluator();
        var results = evaluator.Evaluate(snapshot, HeatUpTurbineStartupProgram.Guidance.Checks)
            .ToDictionary(static result => result.Definition.CheckId, StringComparer.Ordinal);

        Assert.True(results["signals-healthy"].IsSatisfied);
        Assert.True(results["protection-clear"].IsSatisfied);
        Assert.True(results["mcp-running"].IsSatisfied);
        Assert.True(results["heating-power"].IsSatisfied);
        Assert.True(results["steam-pressure"].IsSatisfied);
        Assert.True(results["drum-inventory"].IsSatisfied);
        Assert.True(results["startup-lineup"].IsSatisfied);
        Assert.True(results["turbine-stopped"].IsSatisfied);
        Assert.True(results["breakers-open"].IsSatisfied);
        Assert.True(results["generator-unloaded"].IsSatisfied);
        Assert.False(results["turbine-rolling"].IsSatisfied);
        Assert.False(results["warmup-speed"].IsSatisfied);
        Assert.False(results["near-sync-speed"].IsSatisfied);
    }

    [Fact]
    public void TurbineSpeedRaise_UsesValidatedControllerSeamAndKeepsBreakerCloseFailClosed()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new HeatUpTurbineStartupInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(HeatUpTurbineStartupProgram.Scenario);
        var rotor = Assert.Single(session.Coordinator.Current.TurbineSecondary.Rotors);
        var train = Assert.Single(session.Coordinator.Current.TurbineSecondary.AdmissionTrains);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedRaise,
            rotor.RotorId,
            ControlRoomCommandTargetKind.TurbineRotor));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var candidateTrain = Assert.Single(session.Coordinator.Current.TurbineSecondary.AdmissionTrains);
        Assert.True(candidateTrain.ControlValvePosition.NumericValue.GetValueOrDefault() >
            train.ControlValvePosition.NumericValue.GetValueOrDefault());

        var breaker = Assert.Single(session.Coordinator.Current.Electrical.Generators).BreakerId;
        Assert.Throws<InvalidOperationException>(() => session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            breaker,
            ControlRoomCommandTargetKind.Breaker)));
    }
}
