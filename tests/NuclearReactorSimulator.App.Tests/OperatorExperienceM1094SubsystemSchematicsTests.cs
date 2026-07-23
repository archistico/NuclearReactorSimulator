using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.App.Tests;

public sealed class OperatorExperienceM1094SubsystemSchematicsTests
{
    [Fact]
    public void MainWindow_HostsAllFiveEngineeringSchematics()
    {
        var document = LoadMainWindow();
        var snapshots = document.Descendants()
            .Where(static element => element.Name.LocalName == "ControlRoomSubsystemSchematicControl")
            .Select(static element => (string?)element.Attribute("Snapshot"))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(5, snapshots.Count);
        Assert.Contains("{Binding ReactorCoreSchematic}", snapshots);
        Assert.Contains("{Binding PrimarySteamDrumSchematic}", snapshots);
        Assert.Contains("{Binding TurbineSecondarySchematic}", snapshots);
        Assert.Contains("{Binding GeneratorGridSchematic}", snapshots);
        Assert.Contains("{Binding InstrumentationProtectionSchematic}", snapshots);
    }

    [Fact]
    public void TurbineAndElectricalWorkspaces_ExplainShaftColorAndZeroMWePowerPath()
    {
        var texts = LoadMainWindow().Descendants()
            .Select(static element => (string?)element.Attribute("Text"))
            .Where(static text => text is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("AMBER SHAFT = MECHANICAL ENERGY PATH · it is a medium/type color, not a warning. Alarm/trip severity is shown by equipment state and protection annunciation.", texts);
        Assert.Contains("PROCESS COLORS = MEDIUM / ENERGY TYPE · NOT ALARM SEVERITY", texts);
        Assert.Contains("POWER-PATH DIAGNOSTIC", texts);
        Assert.Contains("{Binding GeneratorPowerPathDiagnosticText}", texts);
        Assert.Contains("GENERATOR / GRID ENGINEERING SCHEMATIC", texts);
    }


    [Fact]
    public void TurbineWorkspace_UsesEffectiveStageSteamFlowInsteadOfLegacyZeroBoundarySeam()
    {
        var steamAdmission = LoadMainWindow().Descendants()
            .Single(element => string.Equals((string?)element.Attribute("Label"), "STEAM ADMISSION · MODEL DIAGNOSTIC", StringComparison.Ordinal));

        Assert.Equal("{Binding TurbineSecondary.EffectiveTurbineSteamFlow.ValueText}", (string?)steamAdmission.Attribute("ValueText"));
        Assert.Equal("{Binding TurbineSecondary.EffectiveTurbineSteamFlow.Unit}", (string?)steamAdmission.Attribute("Unit"));
        Assert.Equal("{Binding TurbineSecondary.EffectiveTurbineSteamFlow.State}", (string?)steamAdmission.Attribute("State"));
    }

    [Fact]
    public void HistoricalMeasuredAndModelLabels_RemainExplicit()
    {
        var labels = LoadMainWindow().Descendants()
            .Select(static element => (string?)element.Attribute("Label"))
            .Where(static label => !string.IsNullOrWhiteSpace(label))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("TOTAL MCP FLOW · MEASURED", labels);
        Assert.Contains("SUCTION HEADER · MODEL", labels);
        Assert.Contains("ROTOR SPEED · MEASURED", labels);
        Assert.Contains("GRID FREQUENCY · MODEL", labels);
        Assert.Contains("ELECTRICAL OUTPUT · MEASURED", labels);
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));
}
