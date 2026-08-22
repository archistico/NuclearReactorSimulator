using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Packs;

public sealed class M10982Hotfix1ProductionMissionPackTests
{
    [Fact]
    public void BoundedDemandV2_RebindsOnlyScenarioIdentityAndPreservesHistoricalV1()
    {
        var historical = InitialOperationalChallengePack.BoundedDemandFollowing;
        var production = ProductionOperationalChallengePack.BoundedDemandFollowing;

        Assert.Equal("bounded-demand-following-5-10-5@1", historical.ExactId);
        Assert.Equal("power-manoeuvring-normal-shutdown", historical.Scenario.ScenarioId);
        Assert.Equal(new InitialConditionReference("stable-low-load-parallel-operation", 1), historical.Scenario.InitialCondition);

        Assert.Equal("bounded-demand-following-5-10-5@2", production.ExactId);
        Assert.Equal("bounded-demand-following-5-10-5@2", production.Challenge.ExactId);
        Assert.Equal(DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario, production.Scenario);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 4), production.Scenario.InitialCondition);
        Assert.Equal(historical.Challenge.ObjectiveId, production.Challenge.ObjectiveId);
        Assert.Equal(historical.Challenge.ExternalDemandProfile?.ExactId, production.Challenge.ExternalDemandProfile?.ExactId);
        Assert.Same(historical.ScoringPolicy, production.ScoringPolicy);
        Assert.Same(historical.ConditionEvaluator, production.ConditionEvaluator);
        Assert.Equal(historical.ScoreEvidenceBindings, production.ScoreEvidenceBindings);
    }

    [Fact]
    public void BoundedDemandV2_ProductionRuntimeCrossesHistoricalControlOutFailureRegion()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        var factory = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory(),
        }));
        var session = factory.Load(pack.Scenario);
        using var source = new MissionPerformanceLiveSnapshotSource(session, pack, TrainingGuidanceMode.Guided);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var executed = 0;
        for (var batch = 0; batch < 10; batch++)
        {
            executed += session.Coordinator.AdvanceRunning(100, publicationStride: 100).ExecutedStepCount;
        }

        Assert.Equal(1_000, executed);
        Assert.Equal(1_000, session.Coordinator.Current.LogicalStep);
        Assert.Equal(pack.ExactId, source.Current.PackExactId);
        Assert.Equal(pack.Scenario.ScenarioId, source.Current.ScenarioId);
        Assert.False(session.Coordinator.Current.ReactorScramActive);
        Assert.False(session.Coordinator.Current.TurbineTripActive);
        Assert.False(session.Coordinator.Current.GeneratorTripActive);
    }
}
