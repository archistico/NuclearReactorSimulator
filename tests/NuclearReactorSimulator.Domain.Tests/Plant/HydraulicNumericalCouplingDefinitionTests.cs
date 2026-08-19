using NuclearReactorSimulator.Domain.Plant;
using Xunit;

namespace NuclearReactorSimulator.Domain.Tests.Plant;

public sealed class HydraulicNumericalCouplingDefinitionTests
{
    [Fact]
    public void ExplicitCommittedState_IsStableBackwardCompatibleDefault()
    {
        var definition = PlantCompositionTests.Plant();

        Assert.Same(HydraulicNumericalCouplingDefinition.ExplicitCommittedState, definition.HydraulicNumericalCoupling);
        Assert.Equal(HydraulicNumericalCouplingMode.ExplicitCommittedState, definition.HydraulicNumericalCoupling.Mode);
    }

    [Fact]
    public void DeterministicHybridSemiImplicit_PreservesConfiguredNumericalGate()
    {
        var coupling = HydraulicNumericalCouplingDefinition.CreateDeterministicHybridSemiImplicit(
            0.060d,
            40d,
            72,
            0.15d,
            1e-5d,
            1e-2d);

        Assert.Equal(HydraulicNumericalCouplingMode.DeterministicHybridSemiImplicit, coupling.Mode);
        Assert.Equal(0.060d, coupling.PredictedSubcooledPressureChangeTriggerFraction, 12);
        Assert.Equal(40d, coupling.PredictedHydraulicFlowChangeTriggerKilogramsPerSecond, 12);
        Assert.Equal(72, coupling.MaximumCorrectorIterations);
        Assert.Equal(0.15d, coupling.CorrectorRelaxationFactor, 12);
        Assert.Equal(1e-5d, coupling.CorrectorRelativePressureTolerance, 12);
        Assert.Equal(1e-2d, coupling.CorrectorAbsoluteFlowToleranceKilogramsPerSecond, 12);
    }

    [Fact]
    public void H19QualifiedFourNodeShadowIntegration_IsFrozenOptInDefinition()
    {
        var coupling = HydraulicNumericalCouplingDefinition.H19QualifiedFourNodeBranchContinuityShadowIntegrated;

        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityShadowIntegrated, coupling.Mode);
        Assert.Equal(0.060d, coupling.PredictedSubcooledPressureChangeTriggerFraction, 12);
        Assert.Equal(40d, coupling.PredictedHydraulicFlowChangeTriggerKilogramsPerSecond, 12);
        Assert.Equal(24, coupling.MaximumCorrectorIterations);
        Assert.Equal(1d, coupling.CorrectorRelaxationFactor, 12);
        Assert.Equal(1e-5d, coupling.CorrectorRelativePressureTolerance, 12);
        Assert.Equal(1e-2d, coupling.CorrectorAbsoluteFlowToleranceKilogramsPerSecond, 12);
    }

    [Fact]
    public void H22FourNodeCorrectedCommit_IsFrozenSeparatelyOptInDefinition()
    {
        var coupling = HydraulicNumericalCouplingDefinition.H22FourNodeBranchContinuityCorrectedCommitOptIn;

        Assert.Equal(HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn, coupling.Mode);
        Assert.Equal(0.060d, coupling.PredictedSubcooledPressureChangeTriggerFraction, 12);
        Assert.Equal(40d, coupling.PredictedHydraulicFlowChangeTriggerKilogramsPerSecond, 12);
        Assert.Equal(24, coupling.MaximumCorrectorIterations);
        Assert.Equal(1d, coupling.CorrectorRelaxationFactor, 12);
        Assert.Equal(1e-5d, coupling.CorrectorRelativePressureTolerance, 12);
        Assert.Equal(1e-2d, coupling.CorrectorAbsoluteFlowToleranceKilogramsPerSecond, 12);
    }

    [Fact]
    public void DeterministicHybridSemiImplicit_RejectsNonPositiveOrNonFiniteControls()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HydraulicNumericalCouplingDefinition.CreateDeterministicHybridSemiImplicit(0d, 40d, 72, 0.15d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => HydraulicNumericalCouplingDefinition.CreateDeterministicHybridSemiImplicit(0.06d, double.NaN, 72, 0.15d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => HydraulicNumericalCouplingDefinition.CreateDeterministicHybridSemiImplicit(0.06d, 40d, 1, 0.15d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => HydraulicNumericalCouplingDefinition.CreateDeterministicHybridSemiImplicit(0.06d, 40d, 72, 0d, 1e-5d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => HydraulicNumericalCouplingDefinition.CreateDeterministicHybridSemiImplicit(0.06d, 40d, 72, 0.15d, 0d, 1e-2d));
        Assert.Throws<ArgumentOutOfRangeException>(() => HydraulicNumericalCouplingDefinition.CreateDeterministicHybridSemiImplicit(0.06d, 40d, 72, 0.15d, 1e-5d, double.PositiveInfinity));
    }
}
