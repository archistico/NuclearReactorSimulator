using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class DesktopHydraulicProductionPolicyTests
{
    [Fact]
    public void I5RepairedPolicy_UsesExactVersionFourByDefaultAndExactVersionTwoForFailClosedKill()
    {
        Assert.Equal(
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.ExplicitCommittedState,
            DesktopHydraulicProductionPolicySelector.ExplicitRollbackPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate,
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);

        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var killedDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);
        var historicalV3Decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);

        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, defaultDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 4), defaultDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, defaultDecision.EffectivePolicy);
        Assert.False(defaultDecision.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killedDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 2), killedDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, killedDecision.EffectivePolicy);
        Assert.True(killedDecision.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, historicalV3Decision.InitialCondition);
        Assert.Equal(3, historicalV3Decision.InitialCondition.Version);

        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario,
            DesktopIntegratedOperationsProductionProgram.Scenario);
        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId).ScenarioId);

        Assert.Equal(
            DesktopSustainedGenerationInitialConditionFactory.Reference,
            DesktopIntegratedOperationsProgram.Scenario.InitialCondition);
        Assert.Equal(
            DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.InitialCondition);
        Assert.Equal(
            DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.InitialCondition);
        Assert.Equal(
            DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.InitialCondition);
        Assert.Equal(
            "integrated-normal-operations-training-i5-repaired-v4-production",
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.ScenarioId);
    }

    [Fact]
    public void I5RepairedPolicy_ExactVersionFactoriesResolveV2V3V4WithoutReinterpretation()
    {
        var explicitFactory = new DesktopSustainedGenerationInitialConditionFactory();
        var historicalV3Factory = new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory();
        var repairedV4Factory = new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            explicitFactory,
            historicalV3Factory,
            repairedV4Factory,
        });

        Assert.Same(explicitFactory, registry.Resolve(DesktopSustainedGenerationInitialConditionFactory.Reference));
        Assert.Same(historicalV3Factory, registry.Resolve(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference));
        Assert.Same(repairedV4Factory, registry.Resolve(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference));

        var explicitEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(explicitFactory.CreateRuntimeEngine());
        var v3Engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(historicalV3Factory.CreateRuntimeEngine());
        var v4Engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(repairedV4Factory.CreateRuntimeEngine());

        Assert.Equal(TimeSpan.FromMilliseconds(10d), explicitEngine.FixedDeltaTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), v3Engine.FixedDeltaTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), v4Engine.FixedDeltaTime);
        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            explicitEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            v3Engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            v4Engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
    }

    [Fact]
    public void I5RepairedPolicy_ProductionTrainingProgramRecognizesHistoricalCandidateAndProductionIdentities()
    {
        var ids = new[]
        {
            DesktopIntegratedOperationsProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId,
            DesktopIntegratedOperationsI5RepairedActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.ScenarioId,
        };

        Assert.All(ids, static id => Assert.True(DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(id)));
        Assert.All(ids, static id => Assert.Equal(id, DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(id).ScenarioId));
    }

    [Fact]
    public void I5RepairedPolicy_RejectsUnknownPolicyValue()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopHydraulicProductionPolicySelector.Resolve((DesktopHydraulicProductionPolicy)999));
}
