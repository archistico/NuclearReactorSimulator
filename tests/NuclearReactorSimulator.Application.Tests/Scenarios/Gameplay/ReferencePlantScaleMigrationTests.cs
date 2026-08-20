using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>
/// M10.9.4.1-E.2 coordinated scale, compatibility, command-clamp and HMI evidence.
/// </summary>
public sealed class ReferencePlantScaleMigrationTests
{
    [Fact(Explicit = true)]
    [Trait("Category", "ReferencePlantScaleAudit")]
    public void CurrentProductionProfilesOwnTenMegawattBidirectionalContract()
    {
        var productionDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        AssertCurrentScale(
            DesktopHydraulicProductionPolicySelector.CreateFactory(productionDecision).CreateRuntimeEngine(),
            expectedRequestMegawatts: 5d);
        AssertCurrentScale(
            new GridSynchronizationCorrectedInitialConditionFactory().CreateRuntimeEngine(),
            expectedRequestMegawatts: 0d);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ReferencePlantScaleAudit")]
    public void LegacyColdShutdownProfileRetainsOneGigawattGenerationOnlyDefaults()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new ColdShutdownInitialConditionFactory().CreateRuntimeEngine());
        var generator = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var scale = Assert.Single(presentation.Electrical.Generators).ElectricalOutput.InstrumentScale!;

        Assert.Equal(1_000d, generator.MaximumElectricalPower.Megawatts, 12);
        Assert.Null(generator.GridCoupling);
        Assert.Equal(0d, scale.Minimum, 12);
        Assert.Equal(1_000d, scale.Maximum, 12);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "ReferencePlantScaleAudit")]
    public void CurrentProduction_LoadCommandsClampAtTenMegawattsAndSignedHmiRangeTracksDefinition()
    {
        var productionDecision = DesktopHydraulicProductionPolicySelector.Resolve(
            DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy);
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            DesktopHydraulicProductionPolicySelector.CreateFactory(productionDecision).CreateRuntimeEngine());
        var initial = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var generator = Assert.Single(initial.Electrical.Generators);

        AssertSignedScale(generator.ElectricalOutput.InstrumentScale);
        AssertSignedScale(initial.Electrical.GrossElectricalOutput.InstrumentScale);
        Assert.Equal(5d, generator.RequestedElectricalPower.NumericValue!.Value, 12);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        var atNameplate = engine.Step(ControlRoomRunState.Paused);
        Assert.Equal(10d, Assert.Single(atNameplate.Electrical.Generators).RequestedElectricalPower.NumericValue!.Value, 12);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        var clamped = engine.Step(ControlRoomRunState.Paused);
        Assert.Equal(10d, Assert.Single(clamped.Electrical.Generators).RequestedElectricalPower.NumericValue!.Value, 12);
    }

    private static void AssertCurrentScale(IControlRoomRuntimeEngine runtime, double expectedRequestMegawatts)
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(runtime);
        var plant = engine.CurrentState.PlantDefinition;
        var rotor = Assert.Single(plant.TurbineExpansionSystem.Rotors);
        var generator = Assert.Single(plant.GeneratorGridSystem.Generators);
        var coupling = Assert.IsType<SynchronousGridCouplingDefinition>(generator.GridCoupling);
        var droop = Assert.IsType<TurbineGovernorDroopDefinition>(
            engine.CurrentState.TurbineSecondaryControlState.Definition.GovernorDroop);
        var generatorInput = Assert.Single(engine.PersistentInputs.PlantInputs.GeneratorGridInputs.GeneratorInputs);
        var presentation = engine.CreatePresentationSnapshot(ControlRoomRunState.Paused);

        Assert.Equal(10d, generator.MaximumElectricalPower.Megawatts, 12);
        Assert.Equal(1_000d, rotor.MomentOfInertia.KilogramSquareMetres, 12);
        Assert.Equal(3_000d, rotor.RatedAngularSpeed.RevolutionsPerMinute, 12);
        Assert.Equal(4.93480220054468d,
            rotor.MomentOfInertia.KineticEnergyAt(rotor.RatedAngularSpeed).Megajoules / generator.MaximumElectricalPower.Megawatts,
            12);
        Assert.Equal(1.5d, droop.FullLoadSpeedReferenceRise.RevolutionsPerMinute, 12);
        Assert.Equal(SynchronousGridPowerFlowMode.Bidirectional, coupling.PowerFlowMode);
        Assert.Equal(0.5d, coupling.MaximumSynchronizingCorrectionPower.Megawatts, 12);
        Assert.Equal(2d, coupling.FrequencyDampingPowerAtOneHertzSlip.Megawatts, 12);
        Assert.Equal(expectedRequestMegawatts, generatorInput.RequestedElectricalPower.Megawatts, 12);
        AssertSignedScale(Assert.Single(presentation.Electrical.Generators).ElectricalOutput.InstrumentScale);
        AssertSignedScale(presentation.Electrical.GrossElectricalOutput.InstrumentScale);
    }

    private static void AssertSignedScale(ControlRoomInstrumentScaleSnapshot? scale)
    {
        var actual = Assert.IsType<ControlRoomInstrumentScaleSnapshot>(scale);
        Assert.Equal(-10d, actual.Minimum, 12);
        Assert.Equal(10d, actual.Maximum, 12);
    }
}
