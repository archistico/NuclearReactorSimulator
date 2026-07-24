using System.Xml.Linq;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.App.Tests;

public sealed class OperatorExperienceM1093PlantMimicTests
{
    [Fact]
    public void Overview_HostsInteractivePlantMimicWithTwoWaySelectionAndSubsystemDrillDown()
    {
        var document = LoadMainWindow();
        var overview = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "StackPanel"
                && (string?)element.Attribute("IsVisible") == "{Binding IsOverviewWorkspaceSelected}");
        var schematicPanel = Assert.Single(overview.Elements().Take(1));
        Assert.Equal("Border", schematicPanel.Name.LocalName);
        Assert.Equal("schematicPanel", (string?)schematicPanel.Attribute("Classes"));

        var mimic = Assert.Single(
            schematicPanel.Descendants(),
            static element => element.Name.LocalName == "ControlRoomPlantMimicControl");

        Assert.Equal("{Binding PlantMimic}", (string?)mimic.Attribute("Snapshot"));
        Assert.Equal("{Binding SelectedMimicElementId, Mode=TwoWay}", (string?)mimic.Attribute("SelectedElementId"));

        var drillDown = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "Button"
                && (string?)element.Attribute("Content") == "OPEN SUBSYSTEM");
        Assert.Equal("{Binding OpenSelectedMimicSubsystemCommand}", (string?)drillDown.Attribute("Command"));
    }

    [Fact]
    public void Overview_ExplainsFlowMediaAndSelectedEquipmentPorts()
    {
        var document = LoadMainWindow();
        var texts = document.Descendants()
            .Select(static element => (string?)element.Attribute("Text"))
            .Where(static text => text is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("WHOLE-PLANT ENGINEERING SCHEMATIC", texts);
        Assert.Contains("{Binding PlantMimic.PathSummaryText}", texts);
        Assert.Contains("{Binding SelectedMimicInputText}", texts);
        Assert.Contains("{Binding SelectedMimicOutputText}", texts);
        Assert.Contains("{Binding SelectedMimicDetailText}", texts);
    }

    [Fact]
    public void SelectedMimicElement_DrillsDownByNavigationOnlyToExistingWorkspace()
    {
        var viewModel = new MainWindowViewModel(
            ApplicationDescriptor.Current,
            new InMemoryControlRoomSnapshotSource(ControlRoomSnapshot.ShellOnly),
            new NoOpDispatcher());

        viewModel.SelectedMimicElementId = "generator";
        viewModel.OpenSelectedMimicSubsystemCommand.Execute(null);

        Assert.Equal(ControlRoomWorkspaceId.Electrical, viewModel.SelectedWorkspace.Id);
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));

    private sealed class NoOpDispatcher : IControlRoomCommandDispatcher
    {
        public void Dispatch(ControlRoomCommand command)
        {
        }
    }
}
