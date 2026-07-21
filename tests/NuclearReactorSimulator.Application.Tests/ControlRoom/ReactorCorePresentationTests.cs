using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class ReactorCorePresentationTests
{
    [Fact]
    public void UnavailablePanel_FailsClosedWithoutSyntheticPlantData()
    {
        var panel = ReactorCorePanelSnapshot.Unavailable;

        Assert.Equal(ControlRoomVisualState.Unavailable, panel.ReactorThermalPower.State);
        Assert.Equal(ControlRoomVisualState.Unavailable, panel.ReactorPeriod.State);
        Assert.Equal(ControlRoomVisualState.Unavailable, panel.XenonReactivity.State);
        Assert.Empty(panel.Zones);
        Assert.Empty(panel.Rods);
        Assert.Empty(panel.RodTargets);
        Assert.False(panel.ReactorScramActive);
        Assert.False(panel.RodWithdrawalInhibited);
    }

    [Fact]
    public void TargetedRodCommand_CarriesOperatorIntentWithoutPhysicalState()
    {
        var command = new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodWithdraw,
            "group-a",
            ControlRoomCommandTargetKind.ControlRodGroup);

        Assert.Equal(ControlRoomCommandKind.ControlRodWithdraw, command.Kind);
        Assert.Equal("group-a", command.TargetId);
        Assert.Equal(ControlRoomCommandTargetKind.ControlRodGroup, command.TargetKind);
    }

    [Fact]
    public void ReactorPanelPresentationTypes_DoNotExposeSimulationOrDomainPhysicsTypes()
    {
        var presentationTypes = new[]
        {
            typeof(ReactorCorePanelSnapshot),
            typeof(ReactorCoreZonePresentationSnapshot),
            typeof(ReactorRodPresentationSnapshot),
            typeof(ReactorRodTargetPresentationSnapshot),
            typeof(ControlRoomValueSnapshot),
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
