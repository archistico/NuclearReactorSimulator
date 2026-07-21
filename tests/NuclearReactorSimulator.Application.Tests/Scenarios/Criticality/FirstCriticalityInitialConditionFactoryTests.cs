using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Criticality;

public sealed class FirstCriticalityInitialConditionFactoryTests
{
    [Fact]
    public void Factory_ReconstructsPreparedSourceRangeHandoffThroughCanonicalM72Recipe()
    {
        var factory = new FirstCriticalityInitialConditionFactory();

        var left = factory.CreateRuntimeEngine();
        var right = factory.CreateRuntimeEngine();
        var snapshot = left.CreatePresentationSnapshot(ControlRoomRunState.Paused);

        Assert.NotSame(left, right);
        Assert.Equal(FirstCriticalityLowPowerProgram.InitialCondition, factory.Descriptor.Reference);
        Assert.Equal(0, snapshot.LogicalStep);
        Assert.Equal(ControlRoomRunState.Paused, snapshot.RunState);
        Assert.Equal(0, snapshot.InvalidMeasuredSignalCount);
        Assert.False(snapshot.AnyTripActive);
        Assert.All(snapshot.ReactorCore.Rods, static rod => Assert.Equal(0d, rod.PercentWithdrawn, 8));
        Assert.All(snapshot.PrimaryCircuit.Pumps, static pump => Assert.True(pump.IsRunning));
        Assert.True(snapshot.ReactorCore.ReactorThermalPower.NumericValue.GetValueOrDefault() > 0d);
        Assert.All(snapshot.Electrical.Generators, static generator => Assert.False(generator.BreakerClosed));
        Assert.All(snapshot.TurbineSecondary.AdmissionTrains, static train =>
        {
            Assert.InRange(Math.Abs(train.StopValvePosition.NumericValue ?? double.NaN), 0d, 1e-8d);
            Assert.InRange(Math.Abs(train.ControlValvePosition.NumericValue ?? double.NaN), 0d, 1e-8d);
            Assert.InRange(Math.Abs(train.AdmissionValvePosition.NumericValue ?? double.NaN), 0d, 1e-8d);
        });
    }

    [Fact]
    public void ScenarioSession_AllowsControlledRodMotionButRejectsM74AndM75Actions()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new ColdShutdownInitialConditionFactory(),
            new FirstCriticalityInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(FirstCriticalityLowPowerProgram.Scenario);
        var rodTarget = Assert.Single(session.Coordinator.Current.ReactorCore.RodTargets);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodWithdraw,
            rodTarget.TargetId,
            rodTarget.TargetKind));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        Assert.Contains(session.Coordinator.Current.ReactorCore.Rods, static rod => rod.PercentWithdrawn > 0d);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodHold,
            rodTarget.TargetId,
            rodTarget.TargetKind));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var breaker = Assert.Single(session.Coordinator.Current.Electrical.Generators).BreakerId;
        Assert.Throws<InvalidOperationException>(() => session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            breaker,
            ControlRoomCommandTargetKind.Breaker)));
        Assert.Throws<InvalidOperationException>(() => session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedRaise,
            Assert.Single(session.Coordinator.Current.TurbineSecondary.Rotors).RotorId,
            ControlRoomCommandTargetKind.TurbineRotor)));
    }

    [Fact]
    public void InitialChecklist_ConfirmsPrecriticalityHandoffWithoutPretendingCriticalityOrXenonAvailability()
    {
        var snapshot = new FirstCriticalityInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var evaluator = new FirstCriticalityChecklistEvaluator();
        var results = evaluator.Evaluate(snapshot, FirstCriticalityLowPowerProgram.Guidance.Checks)
            .ToDictionary(static result => result.Definition.CheckId, StringComparer.Ordinal);

        Assert.True(results["signals-healthy"].IsSatisfied);
        Assert.True(results["protection-clear"].IsSatisfied);
        Assert.True(results["mcp-running"].IsSatisfied);
        Assert.True(results["steam-isolated"].IsSatisfied);
        Assert.True(results["breakers-open"].IsSatisfied);
        Assert.True(results["withdrawal-permitted"].IsSatisfied);
        Assert.True(results["source-range"].IsSatisfied);
        Assert.False(results["critical"].IsSatisfied);
        Assert.False(results["low-power"].IsSatisfied);
        Assert.True(results["xenon-boundary"].IsSatisfied);
    }

    [Fact]
    public void ControlledWithdrawal_MovesCanonicalRodReactivityTowardCriticalityThroughRuntimeSteps()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new FirstCriticalityInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(FirstCriticalityLowPowerProgram.Scenario);
        var rodTarget = Assert.Single(session.Coordinator.Current.ReactorCore.RodTargets);
        var initialReactivity = session.Coordinator.Current.ReactorCore.RodReactivity.NumericValue;

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodWithdraw,
            rodTarget.TargetId,
            rodTarget.TargetKind));
        for (var step = 0; step < 100; step++)
        {
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        }

        var candidate = session.Coordinator.Current;
        Assert.True(Assert.Single(candidate.ReactorCore.Rods).PercentWithdrawn > 0d);
        Assert.True(initialReactivity.HasValue);
        Assert.True(candidate.ReactorCore.RodReactivity.NumericValue.HasValue);
        Assert.True(candidate.ReactorCore.RodReactivity.NumericValue.Value > initialReactivity.Value);
        Assert.True(candidate.ReactorCore.ReactorThermalPower.NumericValue.GetValueOrDefault() > 0d);
    }
}
