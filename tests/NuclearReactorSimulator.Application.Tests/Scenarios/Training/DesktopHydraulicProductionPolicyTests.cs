using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class DesktopHydraulicProductionPolicyTests
{
    [Fact]
    public void ExactV9_IsAuthoritativeWhileHistoricalV4V3AndV2FailClosedRemainExplicitlySelectable()
    {
        Assert.Equal(
            DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.ExplicitCommittedState,
            DesktopHydraulicProductionPolicySelector.ExplicitRollbackPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit,
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate,
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);

        var authoritative = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var historicalV4 = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy);
        var historicalV3 = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var killed = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference, authoritative.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 9), authoritative.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate, authoritative.EffectivePolicy);
        Assert.False(authoritative.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, historicalV4.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 4), historicalV4.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, historicalV4.EffectivePolicy);

        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, historicalV3.InitialCondition);
        Assert.Equal(3, historicalV3.InitialCondition.Version);

        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killed.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 2), killed.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, killed.EffectivePolicy);
        Assert.True(killed.ExplicitKillApplied);

        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario,
            DesktopIntegratedOperationsProductionProgram.Scenario);
        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario,
            DesktopIntegratedOperationsProductionProgram.ResolveScenario(
                DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy));
        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario,
            DesktopIntegratedOperationsProductionProgram.ResolveScenario(
                DesktopHydraulicProductionPolicySelector.I5RepairedProductionPolicy));

        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId).ScenarioId);
        Assert.Equal(
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId).ScenarioId);

        Assert.Equal(
            DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.InitialCondition);
        Assert.Equal(
            DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.InitialCondition);
        Assert.Equal(
            DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario.InitialCondition);
        Assert.Equal(
            "integrated-normal-operations-training-i5-repaired-v4-production",
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.ScenarioId);
        Assert.Equal(
            "integrated-normal-operations-training-m10-final-v9-activation-candidate",
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId);
        Assert.Equal(
            "integrated-normal-operations-training-m10-final-v9-production",
            DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario.ScenarioId);
    }

    [Fact]
    public void QualifiedV9CandidatePolicy_ExactVersionFactoriesResolveV2V3V4V9WithoutReinterpretation()
    {
        var explicitFactory = new DesktopSustainedGenerationInitialConditionFactory();
        var historicalV3Factory = new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory();
        var repairedV4Factory = new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory();
        var qualifiedV9Factory = new DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            explicitFactory,
            historicalV3Factory,
            repairedV4Factory,
            qualifiedV9Factory,
        });

        Assert.Same(explicitFactory, registry.Resolve(DesktopSustainedGenerationInitialConditionFactory.Reference));
        Assert.Same(historicalV3Factory, registry.Resolve(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference));
        Assert.Same(repairedV4Factory, registry.Resolve(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference));
        Assert.Same(qualifiedV9Factory, registry.Resolve(DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference));

        var explicitEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(explicitFactory.CreateRuntimeEngine());
        var v3Engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(historicalV3Factory.CreateRuntimeEngine());
        var v4Engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(repairedV4Factory.CreateRuntimeEngine());
        var v9Engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(qualifiedV9Factory.CreateRuntimeEngine());

        Assert.Equal(TimeSpan.FromMilliseconds(10d), explicitEngine.FixedDeltaTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), v3Engine.FixedDeltaTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), v4Engine.FixedDeltaTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), v9Engine.FixedDeltaTime);
        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            explicitEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            v3Engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            v4Engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            v9Engine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
    }

    [Fact]
    public void ProductionTrainingProgram_RecognizesHistoricalProductionAndQualifiedV9CandidateIdentities()
    {
        var ids = new[]
        {
            DesktopIntegratedOperationsProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId,
            DesktopIntegratedOperationsI5RepairedActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.ScenarioId,
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.M10FinalExactV9ProductionScenario.ScenarioId,
        };

        Assert.All(ids, static id => Assert.True(DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(id)));
        Assert.All(ids, static id => Assert.Equal(id, DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(id).ScenarioId));
    }

    [Fact]
    public void QualifiedV9CandidatePolicy_RejectsUnknownPolicyValue()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopHydraulicProductionPolicySelector.Resolve((DesktopHydraulicProductionPolicy)999));
}
