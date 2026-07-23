using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

public sealed class OperatorComputerM108IntegratedUiTests
{
    [Fact]
    public void Terminal_UsesFixedHeaderStatusContentAndFooterRows()
    {
        var document = LoadTerminal();
        var rootBorder = Assert.Single(
            document.Root!.Elements(),
            static element => element.Name.LocalName == "Border");
        var rootGrid = Assert.Single(
            rootBorder.Elements(),
            static element => element.Name.LocalName == "Grid");

        Assert.Equal("Auto,Auto,*,Auto", (string?)rootGrid.Attribute("RowDefinitions"));

        var mainScroll = Assert.Single(
            rootGrid.Elements(),
            static element => element.Name.LocalName == "ScrollViewer");
        Assert.Equal("2", (string?)mainScroll.Attribute("Grid.Row"));
        Assert.Equal("Auto", (string?)mainScroll.Attribute("VerticalScrollBarVisibility"));
        Assert.Equal("Disabled", (string?)mainScroll.Attribute("HorizontalScrollBarVisibility"));

        var fixedFooter = Assert.Single(
            rootGrid.Elements(),
            static element => element.Name.LocalName == "Border"
                && (string?)element.Attribute("Grid.Row") == "3");
        Assert.Contains(
            fixedFooter.Descendants(),
            static element => element.Name.LocalName == "TextBlock"
                && (string?)element.Attribute("Text") == "{Binding StatusLineText}");
    }

    [Fact]
    public void TerminalNavigation_UsesFourByTwoFixedPagesAndPersistentSelectedIndicators()
    {
        var document = LoadTerminal();
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["F1 GUIDANCE"] = "{Binding IsGuidancePageSelected}",
            ["F2 INFO"] = "{Binding IsInfoPageSelected}",
            ["F3 ALARMS"] = "{Binding IsAlarmsPageSelected}",
            ["F4 COMMANDS"] = "{Binding IsCommandsPageSelected}",
            ["F5 MODES"] = "{Binding IsModesPageSelected}",
            ["F6 DIAGNOSTICS"] = "{Binding IsDiagnosticsPageSelected}",
            ["F7 LOG"] = "{Binding IsLogPageSelected}",
            ["F8 SESSION"] = "{Binding IsSessionPageSelected}",
        };

        foreach (var pair in expected)
        {
            var button = Assert.Single(
                document.Descendants(),
                element => element.Name.LocalName == "Button"
                    && (string?)element.Attribute("Content") == pair.Key);
            var host = Assert.IsType<XElement>(button.Parent);
            Assert.Equal("Grid", host.Name.LocalName);
            Assert.Equal("Auto,3", (string?)host.Attribute("RowDefinitions"));
            Assert.Contains(
                host.Elements(),
                element => element.Name.LocalName == "Border"
                    && (string?)element.Attribute("IsVisible") == pair.Value);
        }

        var navigationGrid = Assert.IsType<XElement>(
            document.Descendants()
                .Single(element => element.Name.LocalName == "Button"
                    && (string?)element.Attribute("Content") == "F1 GUIDANCE")
                .Parent!
                .Parent!);
        Assert.Equal("*,*,*,*", (string?)navigationGrid.Attribute("ColumnDefinitions"));
        Assert.Equal("Auto,Auto", (string?)navigationGrid.Attribute("RowDefinitions"));
    }

    [Fact]
    public void Terminal_AlwaysShowsRuntimeStepAlarmSignalAndProtectionSummaryAboveScrollablePageContent()
    {
        var document = LoadTerminal();
        var bindings = new[]
        {
            "{Binding RuntimeStateText}",
            "{Binding LogicalStepText}",
            "{Binding AlarmStatusText}",
            "{Binding SignalStatusText}",
            "{Binding ProtectionStatusText}",
        };

        var rootGrid = document.Root!
            .Elements()
            .Single(static element => element.Name.LocalName == "Border")
            .Elements()
            .Single(static element => element.Name.LocalName == "Grid");
        var fixedSummary = Assert.Single(
            rootGrid.Elements(),
            static element => element.Name.LocalName == "Border"
                && (string?)element.Attribute("Grid.Row") == "1");

        foreach (var binding in bindings)
        {
            Assert.Contains(
                fixedSummary.Descendants(),
                element => element.Name.LocalName == "TextBlock"
                    && (string?)element.Attribute("Text") == binding);
        }
    }

    [Fact]
    public void CommandsAndSession_RemoveOldRigidListHeightsAndExposeKeyboardGuidance()
    {
        var document = LoadTerminal();
        var commandList = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "ListBox"
                && (string?)element.Attribute("ItemsSource") == "{Binding CommandEntries}");
        Assert.Null(commandList.Attribute("Height"));
        Assert.Equal("260", (string?)commandList.Attribute("MinHeight"));
        Assert.Equal("420", (string?)commandList.Attribute("MaxHeight"));

        var checkpointList = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "ListBox"
                && (string?)element.Attribute("ItemsSource") == "{Binding SessionCheckpoints}");
        Assert.Null(checkpointList.Attribute("Height"));
        Assert.Equal("220", (string?)checkpointList.Attribute("MinHeight"));
        Assert.Equal("360", (string?)checkpointList.Attribute("MaxHeight"));

        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "TextBlock"
                && ((string?)element.Attribute("Text"))?.Contains("F1–F8 page access", StringComparison.Ordinal) == true);
        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "TextBlock"
                && ((string?)element.Attribute("Text"))?.Contains("TAB FOCUS", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Terminal_HasNoFreeFormTextCommandSurface()
    {
        var document = LoadTerminal();
        Assert.DoesNotContain(
            document.Descendants(),
            static element => element.Name.LocalName is "TextBox" or "AutoCompleteBox");
    }

    private static XDocument LoadTerminal()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ControlRoomComputerControl.axaml"));
}
