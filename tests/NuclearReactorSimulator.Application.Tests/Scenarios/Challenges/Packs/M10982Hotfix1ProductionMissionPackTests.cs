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
    public void BoundedDemandProductionVersions_PreserveV1AndV2WhileCurrentV3RebindsOnlyExactScenarioIdentity()
    {
        var historicalV1 = InitialOperationalChallengePack.BoundedDemandFollowing;
        var historicalV2 = ProductionOperationalChallengePack.BoundedDemandFollowingV2;
        var productionV3 = ProductionOperationalChallengePack.BoundedDemandFollowing;

        Assert.Equal("bounded-demand-following-5-10-5@1", historicalV1.ExactId);
        Assert.Equal("power-manoeuvring-normal-shutdown", historicalV1.Scenario.ScenarioId);
        Assert.Equal(new InitialConditionReference("stable-low-load-parallel-operation", 1), historicalV1.Scenario.InitialCondition);

        Assert.Equal("bounded-demand-following-5-10-5@2", historicalV2.ExactId);
        Assert.Equal(DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario, historicalV2.Scenario);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 4), historicalV2.Scenario.InitialCondition);

        Assert.Equal("bounded-demand-following-5-10-5@3", productionV3.ExactId);
        Assert.Equal("bounded-demand-following-5-10-5@3", productionV3.Challenge.ExactId);
        Assert.Equal(DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario, productionV3.Scenario);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 9), productionV3.Scenario.InitialCondition);

        Assert.Equal(historicalV1.Challenge.ObjectiveId, historicalV2.Challenge.ObjectiveId);
        Assert.Equal(historicalV2.Challenge.ObjectiveId, productionV3.Challenge.ObjectiveId);
        Assert.Equal(historicalV1.Challenge.ExternalDemandProfile?.ExactId, historicalV2.Challenge.ExternalDemandProfile?.ExactId);
        Assert.Equal(historicalV2.Challenge.ExternalDemandProfile?.ExactId, productionV3.Challenge.ExternalDemandProfile?.ExactId);
        Assert.Same(historicalV1.ScoringPolicy, historicalV2.ScoringPolicy);
        Assert.Same(historicalV2.ScoringPolicy, productionV3.ScoringPolicy);
        Assert.Same(historicalV1.ConditionEvaluator, historicalV2.ConditionEvaluator);
        Assert.Same(historicalV2.ConditionEvaluator, productionV3.ConditionEvaluator);
        Assert.Equal(historicalV1.ScoreEvidenceBindings, historicalV2.ScoreEvidenceBindings);
        Assert.Equal(historicalV2.ScoreEvidenceBindings, productionV3.ScoreEvidenceBindings);
    }

    [Fact]
    public void BoundedDemandV3_AuthoritativeProductionRuntimeIsHealthyAndMissionBound()
    {
        var pack = ProductionOperationalChallengePack.BoundedDemandFollowing;
        var factory = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory(),
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
        Assert.Equal("bounded-demand-following-5-10-5@3", source.Current.PackExactId);
        Assert.Equal(pack.Scenario.ScenarioId, source.Current.ScenarioId);
        Assert.False(session.Coordinator.Current.ReactorScramActive);
        Assert.False(session.Coordinator.Current.TurbineTripActive);
        Assert.False(session.Coordinator.Current.GeneratorTripActive);
    }
}
