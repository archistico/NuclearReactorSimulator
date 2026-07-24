using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Electrical;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class ReferencePlantScaleMigrationTests
{
    [Fact]
    public void CurrentV2_UsesTenMWeNameplateFiftyPercentReferencePointAndBidirectionalCoupling()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var generator = Assert.Single(engine.CurrentState.PlantDefinition.GeneratorGridSystem.Generators);
        var input = Assert.Single(engine.PersistentInputs.PlantInputs.GeneratorGridInputs.GeneratorInputs);
        var droop = Assert.IsType<TurbineGovernorDroopDefinition>(
            engine.CurrentState.TurbineSecondaryControlState.Definition.GovernorDroop);
        var coupling = Assert.IsType<SynchronousGridCouplingDefinition>(generator.GridCoupling);

        Assert.Equal(10d, generator.MaximumElectricalPower.Megawatts, 12);
        Assert.Equal(5d, input.RequestedElectricalPower.Megawatts, 12);
        Assert.Equal(0.5d, input.RequestedElectricalPower.Watts / generator.MaximumElectricalPower.Watts, 12);
        Assert.Equal(1.5d, droop.FullLoadSpeedReferenceRise.RevolutionsPerMinute, 12);
        Assert.Equal(0.75d, droop.FullLoadSpeedReferenceRise.RevolutionsPerMinute * 0.5d, 12);
        Assert.Equal(SynchronousGridPowerFlowMode.Bidirectional, coupling.PowerFlowMode);
    }

    [Fact]
    public void CurrentV2_ElectricalHmiShowsSignedTenMWeRangeAndLoadRaiseClampsAtNameplate()
    {
        var engine = Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());
        var coordinator = new ControlRoomRuntimeCoordinator(engine);
        var initial = coordinator.Current;
        var generator = Assert.Single(initial.Electrical.Generators);

        Assert.NotNull(generator.ElectricalOutput.InstrumentScale);
        Assert.Equal(-10d, generator.ElectricalOutput.InstrumentScale!.Minimum, 12);
        Assert.Equal(10d, generator.ElectricalOutput.InstrumentScale.Maximum, 12);

        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        engine.Step(ControlRoomRunState.Running);
        engine.QueueOperatorCommand(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            generator.GeneratorId,
            ControlRoomCommandTargetKind.Generator));
        engine.Step(ControlRoomRunState.Running);

        var requested = Assert.Single(
            engine.PersistentInputs.PlantInputs.GeneratorGridInputs.GeneratorInputs).RequestedElectricalPower;
        Assert.Equal(10d, requested.Megawatts, 12);
    }
}
