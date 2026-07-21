using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class TurbineElectricalPresentationTests
{
    [Fact]
    public void UnavailablePanels_FailClosedWithoutSyntheticEquipment()
    {
        var turbine = TurbineSecondaryPanelSnapshot.Unavailable;
        var electrical = ElectricalPanelSnapshot.Unavailable;

        Assert.Empty(turbine.SteamLines);
        Assert.Empty(turbine.AdmissionTrains);
        Assert.Empty(turbine.Rotors);
        Assert.Empty(turbine.Condensers);
        Assert.Equal(ControlRoomVisualState.Unavailable, turbine.TotalTurbineShaftPower.State);
        Assert.False(turbine.TurbineTripActive);

        Assert.Empty(electrical.Generators);
        Assert.Equal(ControlRoomVisualState.Unavailable, electrical.GrossElectricalOutput.State);
        Assert.Equal(ControlRoomVisualState.Unavailable, electrical.Grid.Frequency.State);
        Assert.False(electrical.GeneratorTripActive);
    }

    [Fact]
    public void BreakerCommand_CarriesTypedCanonicalTargetIntentOnly()
    {
        var command = new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorBreakerClose,
            "generator-breaker-a",
            ControlRoomCommandTargetKind.Breaker);

        Assert.Equal(ControlRoomCommandKind.GeneratorBreakerClose, command.Kind);
        Assert.Equal("generator-breaker-a", command.TargetId);
        Assert.Equal(ControlRoomCommandTargetKind.Breaker, command.TargetKind);
    }


    [Fact]
    public void TurbineSpeedAndGeneratorLoadCommands_RemainTypedOperatorIntents()
    {
        var speed = new ControlRoomCommand(
            ControlRoomCommandKind.TurbineSpeedRaise,
            "rotor-a",
            ControlRoomCommandTargetKind.TurbineRotor);
        var load = new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            "generator-a",
            ControlRoomCommandTargetKind.Generator);

        Assert.Equal(ControlRoomCommandTargetKind.TurbineRotor, speed.TargetKind);
        Assert.Equal("rotor-a", speed.TargetId);
        Assert.Equal(ControlRoomCommandTargetKind.Generator, load.TargetKind);
        Assert.Equal("generator-a", load.TargetId);
    }

    [Fact]
    public void TurbineAndElectricalPresentationTypes_DoNotExposeSimulationOrDomainPhysicsTypes()
    {
        var presentationTypes = new[]
        {
            typeof(TurbineSecondaryPanelSnapshot),
            typeof(MainSteamLinePresentationSnapshot),
            typeof(TurbineAdmissionTrainPresentationSnapshot),
            typeof(TurbineRotorPresentationSnapshot),
            typeof(TurbineStageGroupPresentationSnapshot),
            typeof(CondenserPresentationSnapshot),
            typeof(SecondaryPumpPresentationSnapshot),
            typeof(FeedwaterTrainPresentationSnapshot),
            typeof(ElectricalPanelSnapshot),
            typeof(ElectricalGridPresentationSnapshot),
            typeof(GeneratorPresentationSnapshot),
        };

        foreach (var presentationType in presentationTypes)
        {
            Assert.All(
                presentationType.GetProperties(),
                static property =>
                {
                    var ns = property.PropertyType.Namespace;
                    Assert.False(ns?.StartsWith("NuclearReactorSimulator.Simulation", StringComparison.Ordinal) == true);
                    Assert.False(ns?.StartsWith("NuclearReactorSimulator.Domain.Physics", StringComparison.Ordinal) == true);
                });
        }
    }
}
