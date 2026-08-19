using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class DesktopHydraulicProductionPolicyTests
{
    [Fact]
    public void H30RequalificationPolicy_UsesExactVersionThreeByDefaultAndExactVersionTwoForFailClosedKill()
    {
        Assert.Equal(
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.ExplicitCommittedState,
            DesktopHydraulicProductionPolicySelector.ExplicitRollbackPolicy);

        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var killedDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, defaultDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 3), defaultDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, defaultDecision.EffectivePolicy);
        Assert.False(defaultDecision.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killedDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 2), killedDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, killedDecision.EffectivePolicy);
        Assert.True(killedDecision.ExplicitKillApplied);

        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario,
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
            "integrated-normal-operations-training-h29-activation-candidate",
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId);
        Assert.Equal(
            "integrated-normal-operations-training-h30-rq1-production",
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId);
        Assert.NotEqual(
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId);
    }

    [Fact]
    public void H30RequalificationPolicy_ExactVersionFactoriesResolveRollbackAndCorrectedDefaultWithoutReinterpretation()
    {
        var explicitFactory = new DesktopSustainedGenerationInitialConditionFactory();
        var correctedFactory = new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            explicitFactory,
            correctedFactory,
        });

        Assert.Same(explicitFactory, registry.Resolve(DesktopSustainedGenerationInitialConditionFactory.Reference));
        Assert.Same(correctedFactory, registry.Resolve(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference));
        Assert.Throws<KeyNotFoundException>(() => registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 4)));

        var explicitEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(explicitFactory.CreateRuntimeEngine());
        var correctedEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(correctedFactory.CreateRuntimeEngine());

        Assert.Equal(TimeSpan.FromMilliseconds(10d), explicitEngine.FixedDeltaTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), correctedEngine.FixedDeltaTime);
        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            explicitEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            correctedEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
    }

    [Fact]
    public void H30RequalificationPolicy_ProductionTrainingProgramRecognizesHistoricalAndActivatedScenarioIdentities()
    {
        Assert.True(DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(
            DesktopIntegratedOperationsProgram.Scenario.ScenarioId));
        Assert.True(DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId));
        Assert.True(DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId));

        Assert.Equal(
            DesktopIntegratedOperationsProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsProgram.Scenario.ScenarioId).ScenarioId);
        Assert.Equal(
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.ScenarioId).ScenarioId);
        Assert.Equal(
            DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(
                DesktopIntegratedOperationsProductionProgram.CorrectedProductionScenario.ScenarioId).ScenarioId);
    }

    [Fact]
    public void H30RequalificationPolicy_RejectsUnknownPolicyValue()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopHydraulicProductionPolicySelector.Resolve((DesktopHydraulicProductionPolicy)999));
}
