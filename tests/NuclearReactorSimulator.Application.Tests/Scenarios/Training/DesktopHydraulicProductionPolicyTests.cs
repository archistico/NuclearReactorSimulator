using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Training;

public sealed class DesktopHydraulicProductionPolicyTests
{
    [Fact]
    public void H29Policy_PreservesExplicitAuthoritativeDefaultAndAddsExactVersionThreeCandidate()
    {
        Assert.Equal(
            DesktopHydraulicProductionPolicy.ExplicitCommittedState,
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        Assert.Equal(
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate,
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);

        var defaultDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var candidateDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy);
        var killedDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.H29ActivationCandidatePolicy,
            explicitKillRequested: true);

        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, defaultDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 2), defaultDecision.InitialCondition);
        Assert.False(defaultDecision.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference, candidateDecision.InitialCondition);
        Assert.Equal(new InitialConditionReference("integrated-operations-desktop-stable", 3), candidateDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate, candidateDecision.EffectivePolicy);
        Assert.False(candidateDecision.ExplicitKillApplied);

        Assert.Equal(DesktopSustainedGenerationInitialConditionFactory.Reference, killedDecision.InitialCondition);
        Assert.Equal(DesktopHydraulicProductionPolicy.ExplicitCommittedState, killedDecision.EffectivePolicy);
        Assert.True(killedDecision.ExplicitKillApplied);

        Assert.Equal(
            DesktopSustainedGenerationInitialConditionFactory.Reference,
            DesktopIntegratedOperationsProgram.Scenario.InitialCondition);
        Assert.Equal(
            DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
            DesktopIntegratedOperationsH29ActivationCandidateProgram.Scenario.InitialCondition);
    }

    [Fact]
    public void H29Policy_ExactVersionFactoriesResolveExplicitRollbackAndCorrectedCandidateWithoutReinterpretation()
    {
        var explicitFactory = new DesktopSustainedGenerationInitialConditionFactory();
        var candidateFactory = new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory();
        var registry = new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            explicitFactory,
            candidateFactory,
        });

        Assert.Same(explicitFactory, registry.Resolve(DesktopSustainedGenerationInitialConditionFactory.Reference));
        Assert.Same(candidateFactory, registry.Resolve(DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference));
        Assert.Throws<KeyNotFoundException>(() => registry.Resolve(new InitialConditionReference("integrated-operations-desktop-stable", 4)));

        var explicitEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(explicitFactory.CreateRuntimeEngine());
        var candidateEngine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(candidateFactory.CreateRuntimeEngine());

        Assert.Equal(TimeSpan.FromMilliseconds(10d), explicitEngine.FixedDeltaTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10d), candidateEngine.FixedDeltaTime);
        Assert.Equal(
            HydraulicNumericalCouplingMode.ExplicitCommittedState,
            explicitEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
        Assert.Equal(
            HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            candidateEngine.CurrentState.PlantDefinition.PlantDefinition.HydraulicNumericalCoupling.Mode);
    }

    [Fact]
    public void H29Policy_RejectsUnknownPolicyValue()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopHydraulicProductionPolicySelector.Resolve((DesktopHydraulicProductionPolicy)999));
}
