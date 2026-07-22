using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class DesktopIntegratedOperationsProgramTests
{
    [Fact]
    public void DesktopProfile_UsesANewVersionedIdentityWithoutMutatingValidatedM7Content()
    {
        Assert.Equal(new InitialConditionReference("stable-low-load-parallel-operation", 1), PowerManoeuvringNormalShutdownProgram.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 1), DesktopIntegratedOperationsInitialConditionFactory.Reference);
        Assert.NotEqual(
            PowerManoeuvringNormalShutdownProgram.InitialCondition,
            DesktopIntegratedOperationsInitialConditionFactory.Reference);
        Assert.NotEqual(
            IntegratedOperationsTrainingProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProgram.Scenario.ScenarioId);
    }

    [Fact]
    public void DesktopProfile_ContinuousRunRemainsStableForTenSimulatedSeconds()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopIntegratedOperationsInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(DesktopIntegratedOperationsProgram.Scenario);
        var initial = session.Coordinator.Current;
        var initialChecks = new PowerManoeuvringChecklistEvaluator()
            .Evaluate(initial, DesktopIntegratedOperationsProgram.ProcedureGuidance.Checks)
            .ToDictionary(static result => result.Definition.CheckId, StringComparer.Ordinal);

        Assert.True(initialChecks["low-load"].IsSatisfied);
        Assert.True(initialChecks["protection-clear"].IsSatisfied);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var executed = 0;
        for (var batch = 0; batch < 10; batch++)
        {
            executed += session.Coordinator.AdvanceRunning(stepCount: 100, publicationStride: 100).ExecutedStepCount;
        }

        Assert.Equal(1_000, executed);
        Assert.Equal(initial.LogicalStep + 1_000, session.Coordinator.Current.LogicalStep);
        Assert.Equal(ControlRoomRunState.Running, session.Coordinator.Current.RunState);
        Assert.NotEmpty(session.Coordinator.Current.TurbineSecondary.Rotors);
        Assert.All(session.Coordinator.Current.TurbineSecondary.Rotors, static rotor =>
            Assert.True(double.IsFinite(rotor.Speed.NumericValue ?? double.NaN)));
    }

    [Fact]
    public void DesktopProfile_FreshSessionReloadRestartsAtExactInitialState()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopIntegratedOperationsInitialConditionFactory(),
        });
        var factory = new ScenarioSessionFactory(registry);
        var first = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        var initialFingerprint = ControlRoomSnapshotFingerprint.Compute(first.Coordinator.Current);

        first.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = first.Coordinator.AdvanceRunning(stepCount: 250, publicationStride: 250);
        Assert.Equal(250, first.Coordinator.Current.LogicalStep);

        var reloaded = factory.Load(DesktopIntegratedOperationsProgram.Scenario);

        Assert.Equal(0, reloaded.Coordinator.Current.LogicalStep);
        Assert.Equal(ControlRoomRunState.Paused, reloaded.Coordinator.Current.RunState);
        Assert.Equal(initialFingerprint, ControlRoomSnapshotFingerprint.Compute(reloaded.Coordinator.Current));
    }
}
