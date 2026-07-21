using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class PrimaryCircuitPresentationTests
{
    [Fact]
    public void UnavailablePanel_FailsClosedWithoutSyntheticTopology()
    {
        var panel = PrimaryCircuitPanelSnapshot.Unavailable;

        Assert.Empty(panel.Loops);
        Assert.Empty(panel.SteamDrums);
        Assert.Empty(panel.Valves);
        Assert.Empty(panel.Pumps);
        Assert.Equal(ControlRoomVisualState.Unavailable, panel.TotalPrimaryMass.State);
        Assert.Equal(ControlRoomVisualState.Unavailable, panel.TotalFeedwaterFlow.State);
        Assert.Equal(ControlRoomVisualState.Unavailable, panel.TotalSteamExportFlow.State);
    }

    [Fact]
    public void PumpCommand_CarriesTypedOperatorIntentOnly()
    {
        var command = new ControlRoomCommand(
            ControlRoomCommandKind.MainCirculationPumpStart,
            "mcp-a",
            ControlRoomCommandTargetKind.Pump);

        Assert.Equal(ControlRoomCommandKind.MainCirculationPumpStart, command.Kind);
        Assert.Equal("mcp-a", command.TargetId);
        Assert.Equal(ControlRoomCommandTargetKind.Pump, command.TargetKind);
    }

    [Fact]
    public void PrimaryCircuitPresentationTypes_DoNotExposeSimulationOrDomainPhysicsTypes()
    {
        var presentationTypes = new[]
        {
            typeof(PrimaryCircuitPanelSnapshot),
            typeof(PrimaryCircuitLoopPresentationSnapshot),
            typeof(PrimaryCircuitPumpPresentationSnapshot),
            typeof(PrimaryCircuitBranchPresentationSnapshot),
            typeof(PrimaryCircuitSteamDrumPresentationSnapshot),
            typeof(PrimaryCircuitValvePresentationSnapshot),
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
