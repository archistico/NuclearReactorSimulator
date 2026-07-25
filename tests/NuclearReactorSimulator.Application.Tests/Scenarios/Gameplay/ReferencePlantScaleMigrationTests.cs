using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// Compatibility replacement for an obsolete local Phase-E draft that referenced the removed
/// SynchronousGridCouplingDefinition.PowerFlowMode member. The accepted scale migration and
/// bidirectional generator/grid power-flow contract remain deferred to Phase E.
/// </summary>
public sealed class ReferencePlantScaleMigrationTests
{
    [Fact(Explicit = true)]
    [Trait("Category", "ReferencePlantScaleAudit")]
    public void CurrentV2_GridCouplingRetainsThePresentCorrectionOnlyContractPendingPhaseE()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var generator = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var coupling = Assert.IsType<SynchronousGridCouplingDefinition>(generator.GridCoupling);

        Assert.Equal(0.5d, coupling.MaximumSynchronizingCorrectionPower.Megawatts, 12);
        Assert.Equal(2d, coupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts, 12);
    }
}
