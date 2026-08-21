using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

public sealed class OperatorExperienceM1091HmiShellTests
{
    [Fact]
    public void Shell_UsesSituationStripPersistentTopAlarmZoneCompactNavigationWorkspaceAndInspector()
    {
        var document = LoadMainWindow();
        var rootGrid = Assert.Single(document.Root!.Elements(), static element => element.Name.LocalName == "Grid");
        Assert.Equal("Auto,*", (string?)rootGrid.Attribute("RowDefinitions"));

        var bodyGrid = Assert.Single(
            rootGrid.Elements(),
            static element => element.Name.LocalName == "Grid" && (string?)element.Attribute("Grid.Row") == "1");
        Assert.Equal("172,*,260", (string?)bodyGrid.Attribute("ColumnDefinitions"));

        Assert.Contains(
            bodyGrid.Descendants(),
            static element => element.Name.LocalName == "TextBlock" && (string?)element.Attribute("Text") == "CONTEXT INSPECTOR");
        Assert.Contains(
            bodyGrid.Descendants(),
            static element => element.Name.LocalName == "TextBlock" && (string?)element.Attribute("Text") == "SYSTEMS");

        var alarmZone = Assert.Single(
            rootGrid.Descendants(),
            static element => element.Name.LocalName == "ControlRoomAlarmBanner");
        Assert.Equal("2", (string?)alarmZone.Attribute("Grid.Row"));
        Assert.Equal("{Binding AlarmEvents}", (string?)alarmZone.Attribute("Snapshot"));
        Assert.Equal(
            "{Binding OpenOperatorComputerAlarmsPageCommand}",
            (string?)alarmZone.Attribute("OpenAlarmsCommand"));
    }

    [Fact]
    public void SituationStrip_ExposesOperationalStatusWithoutInventingFutureDemandCapability()
    {
        var document = LoadMainWindow();
        var rootGrid = Assert.Single(document.Root!.Elements(), static element => element.Name.LocalName == "Grid");
        var situationStrip = Assert.Single(
            rootGrid.Elements(),
            static element => element.Name.LocalName == "Border" && (string?)element.Attribute("Grid.Row") == "0");
        var textValues = situationStrip.Descendants()
            .Select(static element => (string?)element.Attribute("Text"))
            .Where(static text => text is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("{Binding RuntimeState}", textValues);
        Assert.Contains("{Binding RuntimeProgressText}", textValues);
        Assert.Contains("{Binding ElectricalOutputText}", textValues);
        Assert.Contains("{Binding TrainingScoreText}", textValues);
        Assert.Contains("{Binding UnacknowledgedAlarmCountText}", textValues);
        Assert.Contains("{Binding ProtectionStateText}", textValues);
        Assert.Contains("{Binding GuidanceModeShortText}", textValues);
        Assert.Contains("{Binding ControlAuthorityText}", textValues);
        Assert.DoesNotContain("GRID DEMAND", textValues);
    }

    [Fact]
    public void Inspector_ExplainsConditionNextActionContextAndCommandFeedback()
    {
        var document = LoadMainWindow();
        var bindings = new[]
        {
            "{Binding OperatorCurrentConditionText}",
            "{Binding OperatorNextActionText}",
            "{Binding SelectedWorkspaceContextText}",
            "{Binding CommandStatus}",
        };

        foreach (var binding in bindings)
        {
            Assert.Contains(
                document.Descendants(),
                element => element.Name.LocalName == "TextBlock" && (string?)element.Attribute("Text") == binding);
        }
    }

    [Fact]
    public void OperatorFacingHeadings_DoNotExposeImplementationMilestonePrefixes()
    {
        var document = LoadMainWindow();
        var literalTexts = document.Descendants()
            .Select(static element => (string?)element.Attribute("Text"))
            .Where(static text => !string.IsNullOrWhiteSpace(text) && !text.StartsWith("{Binding", StringComparison.Ordinal))
            .Select(static text => text!);

        Assert.DoesNotContain(literalTexts, static text => text!.StartsWith("M6.", StringComparison.Ordinal));
        Assert.DoesNotContain(literalTexts, static text => text!.StartsWith("M8.", StringComparison.Ordinal));
        Assert.Contains("INFORMATION TRUST", literalTexts);
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));
}
