using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

public sealed class ControlRoomComputerControlXamlContractTests
{
    [Fact]
    public void TerminalShell_HasExactlyEightFixedNamedPageButtonsAndFixedStatusLine()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ControlRoomComputerControl.axaml"));
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["F1 GUIDANCE"] = "{Binding SelectGuidancePageCommand}",
            ["F2 INFO"] = "{Binding SelectInfoPageCommand}",
            ["F3 ALARMS"] = "{Binding SelectAlarmsPageCommand}",
            ["F4 COMMANDS"] = "{Binding SelectCommandsPageCommand}",
            ["F5 MODES"] = "{Binding SelectModesPageCommand}",
            ["F6 DIAGNOSTICS"] = "{Binding SelectDiagnosticsPageCommand}",
            ["F7 LOG"] = "{Binding SelectLogPageCommand}",
            ["F8 SESSION"] = "{Binding SelectSessionPageCommand}",
        };

        var buttons = document.Descendants().Where(static element => element.Name.LocalName == "Button").ToArray();
        var pageButtons = buttons.Where(element => expected.ContainsKey((string?)element.Attribute("Content") ?? string.Empty)).ToArray();
        Assert.Equal(8, pageButtons.Length);
        foreach (var pair in expected)
        {
            var button = Assert.Single(pageButtons, element => (string?)element.Attribute("Content") == pair.Key);
            Assert.Equal(pair.Value, (string?)button.Attribute("Command"));
        }

        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "TextBlock" && (string?)element.Attribute("Text") == "{Binding StatusLineText}");
        Assert.DoesNotContain(
            document.Descendants(),
            static element => element.Name.LocalName is "TextBox" or "AutoCompleteBox");

        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "TextBlock" && (string?)element.Attribute("Text") == "{Binding SelectedPageContentText}");
        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "TextBlock" && ((string?)element.Attribute("Text"))?.Contains("M10.7 SESSION / CHECKPOINT / REPLAY / SAVE WORKSPACE", StringComparison.Ordinal) == true);
        Assert.Contains(
            buttons,
            static element => (string?)element.Attribute("Content") == "EXECUTE SELECTED [ENTER]" &&
                              (string?)element.Attribute("Command") == "{Binding ExecuteSelectedCommandCommand}");
        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "ListBox" &&
                              (string?)element.Attribute("ItemsSource") == "{Binding CommandEntries}" &&
                              (string?)element.Attribute("SelectedItem") == "{Binding SelectedCommand, Mode=TwoWay}");
        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "KeyBinding" &&
                              (string?)element.Attribute("Gesture") == "Enter" &&
                              (string?)element.Attribute("Command") == "{Binding ExecuteSelectedCommandCommand}");
        Assert.Contains(
            buttons,
            static element => (string?)element.Attribute("Content") == "MANUAL" &&
                              (string?)element.Attribute("Command") == "{Binding SetPlantManualCommand}");
        Assert.Contains(
            buttons,
            static element => (string?)element.Attribute("Content") == "ASSISTED" &&
                              (string?)element.Attribute("Command") == "{Binding SetPlantAssistedCommand}");
        Assert.Contains(
            buttons,
            static element => (string?)element.Attribute("Content") == "SUPERVISORY" &&
                              (string?)element.Attribute("Command") == "{Binding SetPlantSupervisoryCommand}");
        Assert.Contains(
            buttons,
            static element => (string?)element.Attribute("Content") == "HOLD CURRENT OPERATING POINT" &&
                              (string?)element.Attribute("Command") == "{Binding HoldCurrentOperatingPointCommand}");
        Assert.Contains(
            buttons,
            static element => (string?)element.Attribute("Content") == "GUIDED" &&
                              (string?)element.Attribute("Command") == "{Binding SetTrainingGuidedCommand}");
        Assert.Contains(
            buttons,
            static element => (string?)element.Attribute("Content") == "CREATE CHECKPOINT" &&
                              (string?)element.Attribute("Command") == "{Binding CreateSessionCheckpointCommand}");
        Assert.Contains(
            buttons,
            static element => (string?)element.Attribute("Content") == "VERIFY REPLAY" &&
                              (string?)element.Attribute("Command") == "{Binding VerifySessionReplayCommand}");
        Assert.Contains(buttons, static element => (string?)element.Attribute("Content") == "START RECORDED SESSION");
        Assert.Contains(buttons, static element => (string?)element.Attribute("Content") == "SAVE ARCHIVE");
        Assert.Contains(buttons, static element => (string?)element.Attribute("Content") == "LOAD ARCHIVE");
        Assert.Contains(buttons, static element => (string?)element.Attribute("Content") == "RESTORE SELECTED");
    }

    [Fact]
    public void TerminalShell_UsesMonospaceHudPresentationWithoutOwningPhysicalControls()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ControlRoomComputerControl.axaml"));

        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "TextBlock" && (string?)element.Attribute("FontFamily") == "Consolas");
        Assert.DoesNotContain(
            document.Descendants(),
            static element => element.Name.LocalName == "ControlRoomPushButton");
    }
}
