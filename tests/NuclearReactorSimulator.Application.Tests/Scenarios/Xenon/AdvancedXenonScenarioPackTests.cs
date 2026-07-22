using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.Xenon;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Xenon;

public sealed class AdvancedXenonScenarioPackTests
{
    [Fact]
    public void RestartSeed_PromotesCanonicalNegativeXenonReactivityWithoutScenarioOwnedPhysics()
    {
        var factory = new XenonRestartInitialConditionFactory();
        var snapshot = factory.CreateRuntimeEngine().CreatePresentationSnapshot(ControlRoomRunState.Paused);

        Assert.Equal(AdvancedXenonScenarioPack.RestartInitialCondition, factory.Descriptor.Reference);
        Assert.NotEqual(ControlRoomVisualState.Unavailable, snapshot.ReactorCore.XenonReactivity.State);
        var xenonPcm = snapshot.ReactorCore.XenonReactivity.NumericValue;
        Assert.True(xenonPcm.HasValue);
        Assert.True(double.IsFinite(xenonPcm.Value));
        Assert.True(xenonPcm.Value < 0d);
        Assert.True(snapshot.ReactorCore.NonRodReactivity.NumericValue.HasValue);
        Assert.InRange(
            Math.Abs(snapshot.ReactorCore.NonRodReactivity.NumericValue.Value - xenonPcm.Value),
            0d,
            1e-9d);
    }

    [Fact]
    public void ExistingM7V1Seed_RemainsXenonUnavailableForExactVersionCompatibility()
    {
        var factory = new FirstCriticalityInitialConditionFactory();
        var snapshot = factory.CreateRuntimeEngine().CreatePresentationSnapshot(ControlRoomRunState.Paused);

        Assert.Equal(ControlRoomVisualState.Unavailable, snapshot.ReactorCore.XenonReactivity.State);
        Assert.Null(snapshot.ReactorCore.XenonReactivity.NumericValue);
    }

    [Fact]
    public void RestartSeed_XenonBuildsDeterministicallyFromCanonicalHistoryDuringLowPowerWindow()
    {
        var left = new XenonRestartInitialConditionFactory().CreateRuntimeEngine();
        var right = new XenonRestartInitialConditionFactory().CreateRuntimeEngine();
        var initialXenon = left.CreatePresentationSnapshot(ControlRoomRunState.Paused).ReactorCore.XenonReactivity.NumericValue;

        // This Application-level regression verifies that the versioned seed is wired end-to-end into the canonical
        // M2.8 history and remains deterministic. Keep the window deliberately short: long-horizon iodine/xenon
        // integration is covered by Simulation tests and must not make this wiring test depend on unrelated M3
        // simplified water/steam envelope endurance.
        ControlRoomSnapshot? leftFinal = null;
        ControlRoomSnapshot? rightFinal = null;
        for (var step = 0; step < 10; step++)
        {
            leftFinal = left.Step(ControlRoomRunState.Paused);
            rightFinal = right.Step(ControlRoomRunState.Paused);
        }

        var leftSnapshot = Assert.IsType<ControlRoomSnapshot>(leftFinal);
        var rightSnapshot = Assert.IsType<ControlRoomSnapshot>(rightFinal);
        Assert.Equal(
            ControlRoomSnapshotFingerprint.Compute(leftSnapshot),
            ControlRoomSnapshotFingerprint.Compute(rightSnapshot));
        Assert.True(initialXenon.HasValue);
        Assert.True(leftSnapshot.ReactorCore.XenonReactivity.NumericValue.HasValue);
        Assert.True(leftSnapshot.ReactorCore.XenonReactivity.NumericValue.Value < initialXenon.Value);
    }

    [Fact]
    public void ScenarioPack_UsesVersionedSeedsAndOnlyTypedOperatorSeams()
    {
        Assert.Equal(2, AdvancedXenonScenarioPack.All.Count);
        Assert.All(AdvancedXenonScenarioPack.All, scenario =>
        {
            Assert.Empty(scenario.Faults);
            Assert.Contains(ControlRoomCommandKind.ControlRodHold, scenario.AllowedOperatorActions);
            Assert.Contains(ControlRoomCommandKind.ReactorScram, scenario.AllowedOperatorActions);
            Assert.DoesNotContain(ControlRoomCommandKind.GeneratorBreakerClose, scenario.AllowedOperatorActions);
        });

        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new XenonRestartInitialConditionFactory(),
            new LowPowerXenonInitialConditionFactory(),
        });
        var session = new ScenarioSessionFactory(registry).Load(AdvancedXenonScenarioPack.LowPowerPoisoningChallenge);

        Assert.Equal(AdvancedXenonScenarioPack.LowPowerInitialCondition, session.InitialCondition.Reference);
        var xenonPcm = session.Coordinator.Current.ReactorCore.XenonReactivity.NumericValue;
        Assert.True(xenonPcm.HasValue);
        Assert.True(xenonPcm.Value < 0d);
    }
}
