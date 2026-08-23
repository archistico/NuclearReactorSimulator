using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class DesktopHydraulicProductionPolicyTests
{
    [Fact]
    public void I5RepairedPolicy_RemainsAuthoritativeWhileQualifiedV9IsExplicitCandidateAndV2IsFailClosedKill()
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
        Assert.Equal(
            DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate,
            DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy);

        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var candidateDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy);
        var killedCandidateDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy,
            explicitKillRequested: true);
        var historicalV3Decision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);

        Assert.Equal(DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference, defaultDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 4), defaultDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit, defaultDecision.EffectivePolicy);
        Assert.False(defaultDecision.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference, candidateDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 9), candidateDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate, candidateDecision.EffectivePolicy);
        Assert.False(candidateDecision.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killedCandidateDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 2), killedCandidateDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, killedCandidateDecision.EffectivePolicy);
        Assert.True(killedCandidateDecision.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, historicalV3Decision.InitialCondition);
        Assert.Equal(3, historicalV3Decision.InitialCondition.Version);

        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario,
            DesktopIntegratedOperationsProductionProgram.Scenario);
        Assert.Equal(
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario,
            DesktopIntegratedOperationsProductionProgram.ResolveScenario(
                DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy));
        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsProductionProgram.Scenario.ScenarioId).ScenarioId);
        Assert.Equal(
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId).ScenarioId);

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
            DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.InitialCondition);
        Assert.Equal(
            "integrated-normal-operations-training-i5-repaired-v4-production",
            DesktopIntegratedOperationsProductionProgram.RepairedProductionScenario.ScenarioId);
        Assert.Equal(
            "integrated-normal-operations-training-m10-final-v9-activation-candidate",
            DesktopIntegratedOperationsM10FinalV9ActivationCandidateProgram.Scenario.ScenarioId);
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
        };

        Assert.All(ids, static id => Assert.True(DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(id)));
        Assert.All(ids, static id => Assert.Equal(id, DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(id).ScenarioId));
    }

    [Fact]
    public void QualifiedV9CandidatePolicy_RejectsUnknownPolicyValue()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopHydraulicProductionPolicySelector.Resolve((DesktopHydraulicProductionPolicy)999));
}
