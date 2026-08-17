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
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 2), DesktopSustainedGenerationInitialConditionFactory.Reference);
        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, DesktopIntegratedOperationsProgram.Scenario.InitialCondition);
        Assert.NotEqual(
            PowerManoeuvringNormalShutdownProgram.InitialCondition,
            DesktopSustainedGenerationInitialConditionFactory.Reference);
        Assert.NotEqual(
            IntegratedOperationsTrainingProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProgram.Scenario.ScenarioId);
    }

    [Fact]
    public void DesktopProfile_ContinuousRunRemainsStableForTenSimulatedSeconds()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
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
        {
            Assert.True(double.IsFinite(rotor.Speed.NumericValue ?? double.NaN));

            // G.2 intentionally changes passive current-v2 steam-path advection from u*m_dot to h*m_dot.
            // This is a stability envelope, not an exact pre-migration calibration point. Keep the lower
            // bound at 49.0 Hz mechanical-equivalent speed, still above the 48.8 Hz underfrequency pickup.
            Assert.InRange(rotor.Speed.NumericValue ?? double.NaN, 2_940d, 3_050d);
            Assert.True((rotor.ShaftPower.NumericValue ?? 0d) > 4.5d);
            Assert.False(rotor.TripCommandActive);
            Assert.False(rotor.OverspeedDetected);
        });
        var electrical = session.Coordinator.Current.Electrical;
        var grossGridExchange = electrical.GrossElectricalOutput.NumericValue ?? double.NaN;

        // E.2/E.3 validated a signed bidirectional grid exchange: a healthy breaker-closed
        // operating trajectory may cross zero briefly because phase and frequency correction
        // remain dynamic. Stability therefore means finite, nameplate-bounded exchange while
        // the breaker stays closed, requested load remains generation-ready and no trip latches.
        Assert.False(electrical.GeneratorTripActive);
        Assert.True(double.IsFinite(grossGridExchange));
        Assert.InRange(grossGridExchange, -10d, 10d);
        Assert.NotEmpty(electrical.Generators);
        Assert.All(electrical.Generators, static generator =>
        {
            var generatorExchange = generator.ElectricalOutput.NumericValue ?? double.NaN;

            Assert.True(generator.BreakerClosed);
            Assert.True((generator.RequestedElectricalPower.NumericValue ?? 0d) > 4.5d);
            Assert.True(double.IsFinite(generatorExchange));
            Assert.InRange(generatorExchange, -10d, 10d);
        });
    }

    [Fact]
    public void DesktopProfile_FreshSessionReloadRestartsAtExactInitialState()
    {
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
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
