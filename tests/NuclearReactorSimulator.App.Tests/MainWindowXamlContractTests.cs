using System.Xml.Linq;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

public sealed class MainWindowXamlContractTests
{
    [Fact]
    public void AllOperationalPushButtons_BindToExpectedTypedCommandsAndVisualStates()
    {
        var document = LoadMainWindow();
        var expected = new Dictionary<string, (string Command, string State)>(StringComparer.Ordinal)
        {
            ["START / RUN"] = ("{Binding PumpStartCommand}", "{Binding PumpCommandState}"),
            ["STOP"] = ("{Binding PumpStopCommand}", "{Binding PumpCommandState}"),
            ["TURBINE TRIP"] = ("{Binding TurbineTripCommand}", "{Binding TurbineTripCommandState}"),
            ["SPEED LOWER"] = ("{Binding TurbineSpeedLowerCommand}", "{Binding TurbineSpeedCommandState}"),
            ["SPEED RAISE"] = ("{Binding TurbineSpeedRaiseCommand}", "{Binding TurbineSpeedCommandState}"),
            ["LOAD LOWER"] = ("{Binding GeneratorLoadLowerCommand}", "{Binding GeneratorLoadCommandState}"),
            ["LOAD RAISE"] = ("{Binding GeneratorLoadRaiseCommand}", "{Binding GeneratorLoadCommandState}"),
            ["CLOSE BREAKER"] = ("{Binding GeneratorBreakerCloseCommand}", "{Binding BreakerCloseCommandState}"),
            ["OPEN BREAKER"] = ("{Binding GeneratorBreakerOpenCommand}", "{Binding BreakerOpenCommandState}"),
            ["GENERATOR TRIP"] = ("{Binding GeneratorTripCommand}", "{Binding GeneratorTripCommandState}"),
            ["INSERT"] = ("{Binding RodInsertCommand}", "{Binding RodCommandState}"),
            ["HOLD"] = ("{Binding RodHoldCommand}", "{Binding RodCommandState}"),
            ["WITHDRAW"] = ("{Binding RodWithdrawCommand}", "{Binding RodWithdrawCommandState}"),
            ["SCRAM"] = ("{Binding ReactorScramCommand}", "{Binding ScramCommandState}"),
            ["PROTECTION RESET"] = ("{Binding ProtectionResetCommand}", "{Binding ReactorCommandState}"),
            ["ACKNOWLEDGE"] = ("{Binding AlarmAcknowledgeCommand}", "{Binding AlarmAcknowledgeCommandState}"),
            ["RESET"] = ("{Binding AlarmResetCommand}", "{Binding AlarmResetCommandState}"),
            ["ACK ALL"] = ("{Binding AlarmAcknowledgeAllCommand}", "{Binding AlarmAcknowledgeAllCommandState}"),
            ["RESET ALL"] = ("{Binding AlarmResetAllCommand}", "{Binding AlarmResetAllCommandState}"),
        };

        var buttons = document.Descendants()
            .Where(static element => element.Name.LocalName == "ControlRoomPushButton")
            .ToArray();

        foreach (var pair in expected)
        {
            var button = Assert.Single(buttons, element => (string?)element.Attribute("Label") == pair.Key);
            Assert.Equal(pair.Value.Command, (string?)button.Attribute("Command"));
            Assert.Equal(pair.Value.State, (string?)button.Attribute("State"));
        }
    }

    [Fact]
    public void ShellRunPauseSingleStepButtons_RemainBoundToHostCommands()
    {
        var document = LoadMainWindow();
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Run"] = "{Binding RunCommand}",
            ["Pause"] = "{Binding PauseCommand}",
            ["Single step"] = "{Binding SingleStepCommand}",
        };

        foreach (var pair in expected)
        {
            var button = Assert.Single(
                document.Descendants(),
                element => element.Name.LocalName == "Button" && (string?)element.Attribute("Content") == pair.Key);
            Assert.Equal(pair.Value, (string?)button.Attribute("Command"));
        }

        var reset = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "Button" && (string?)element.Attribute("Content") == "Reset session");
        Assert.Equal("ResetSession_Click", (string?)reset.Attribute("Click"));
    }

    [Fact]
    public void TargetSelectors_AreTwoWayAndUseDedicatedAvailabilityStates()
    {
        var document = LoadMainWindow();
        var expected = new Dictionary<string, (string SelectedIndex, string State)>(StringComparer.Ordinal)
        {
            ["PUMP TARGET"] = ("{Binding SelectedPumpIndex, Mode=TwoWay}", "{Binding PumpCommandState}"),
            ["GENERATOR TARGET"] = ("{Binding SelectedGeneratorIndex, Mode=TwoWay}", "{Binding GeneratorSelectionState}"),
            ["ROD TARGET"] = ("{Binding SelectedRodIndex, Mode=TwoWay}", "{Binding RodCommandState}"),
            ["ALARM TARGET"] = ("{Binding SelectedAlarmIndex, Mode=TwoWay}", "{Binding AlarmSelectionState}"),
        };

        foreach (var pair in expected)
        {
            var selector = Assert.Single(
                document.Descendants(),
                element => element.Name.LocalName == "ControlRoomSelector" && (string?)element.Attribute("Label") == pair.Key);
            Assert.Equal(pair.Value.SelectedIndex, (string?)selector.Attribute("SelectedIndex"));
            Assert.Equal(pair.Value.State, (string?)selector.Attribute("State"));
        }
    }

    [Fact]
    public void MeasuredAndModelLabels_RemainExplicitOnKeyInstrumentation()
    {
        var document = LoadMainWindow();
        var labels = document.Descendants()
            .Select(static element => (string?)element.Attribute("Label"))
            .Where(static label => !string.IsNullOrWhiteSpace(label))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("TOTAL MCP FLOW · MEASURED", labels);
        Assert.Contains("SUCTION HEADER · MODEL", labels);
        Assert.Contains("ROTOR SPEED · MEASURED", labels);
        Assert.Contains("GRID FREQUENCY · MODEL", labels);
        Assert.Contains("ELECTRICAL OUTPUT · MEASURED", labels);
    }

    [Fact]
    public void WorkspaceNavigation_RemainsTwoWayBoundToCatalogSelection()
    {
        var document = LoadMainWindow();
        var listBox = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "ListBox"
                && (string?)element.Attribute("ItemsSource") == "{Binding Workspaces}");

        Assert.Equal("{Binding SelectedWorkspace, Mode=TwoWay}", (string?)listBox.Attribute("SelectedItem"));
    }


    [Fact]
    public void RuntimeProgressIndicator_BindsToRunStateAndLogicalStepBesideHostControls()
    {
        var document = LoadMainWindow();
        var progress = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "ProgressBar"
                && (string?)element.Attribute("IsIndeterminate") == "{Binding IsRuntimeRunning}");

        Assert.Equal("90", (string?)progress.Attribute("Width"));
        Assert.Contains(
            document.Descendants(),
            static element => element.Name.LocalName == "TextBlock"
                && (string?)element.Attribute("Text") == "{Binding RuntimeProgressText}");
    }

    [Fact]
    public void MainWorkspaceScroll_PreservesUserValidatedSingleAxisLayoutWithoutSyntheticMinimumWidth()
    {
        var document = LoadMainWindow();
        var root = Assert.IsType<XElement>(document.Root);
        Assert.Equal("1340", (string?)root.Attribute("MinWidth"));

        var centerScroll = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "ScrollViewer"
                && element.Attributes().Any(attribute => attribute.Name.LocalName == "Name" && attribute.Value == "MainWorkspaceScroll"));

        Assert.Null(centerScroll.Attribute("Padding"));
        Assert.Equal("True", (string?)centerScroll.Attribute("ClipToBounds"));
        Assert.Equal("Auto", (string?)centerScroll.Attribute("VerticalScrollBarVisibility"));
        Assert.Equal("Disabled", (string?)centerScroll.Attribute("HorizontalScrollBarVisibility"));

        var paddedContent = Assert.Single(centerScroll.Elements(), static element => element.Name.LocalName == "Border");
        Assert.Equal("28", (string?)paddedContent.Attribute("Padding"));
        Assert.Null(paddedContent.Attribute("MinWidth"));
        Assert.Null(paddedContent.Attribute("HorizontalAlignment"));

        Assert.Contains(
            paddedContent.Descendants(),
            static element => element.Name.LocalName == "Border" && (string?)element.Attribute("Height") == "40");

        var centerHost = Assert.IsType<XElement>(centerScroll.Parent);
        Assert.Equal("Border", centerHost.Name.LocalName);
        Assert.Equal("1", (string?)centerHost.Attribute("Grid.Column"));
        Assert.Equal("True", (string?)centerHost.Attribute("ClipToBounds"));

        var centerGrid = Assert.IsType<XElement>(centerHost.Parent);
        Assert.Equal("Grid", centerGrid.Name.LocalName);
        Assert.Equal("1", (string?)centerGrid.Attribute("Grid.Row"));
        Assert.Equal("240,*,320", (string?)centerGrid.Attribute("ColumnDefinitions"));
        Assert.Equal("True", (string?)centerGrid.Attribute("ClipToBounds"));
    }

    [Fact]
    public void OperatorComputerWorkspace_UsesDedicatedTerminalControlAndGlobalF1ToF8PageBindings()
    {
        var document = LoadMainWindow();

        var computer = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "ControlRoomComputerControl");
        Assert.Equal("{Binding OperatorComputer}", (string?)computer.Attribute("DataContext"));

        var computerHost = Assert.IsType<XElement>(computer.Parent);
        Assert.Equal("Border", computerHost.Name.LocalName);
        Assert.Equal("{Binding IsOperatorComputerWorkspaceSelected}", (string?)computerHost.Attribute("IsVisible"));

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["F1"] = "{Binding OpenOperatorComputerGuidancePageCommand}",
            ["F2"] = "{Binding OpenOperatorComputerInfoPageCommand}",
            ["F3"] = "{Binding OpenOperatorComputerAlarmsPageCommand}",
            ["F4"] = "{Binding OpenOperatorComputerCommandsPageCommand}",
            ["F5"] = "{Binding OpenOperatorComputerModesPageCommand}",
            ["F6"] = "{Binding OpenOperatorComputerDiagnosticsPageCommand}",
            ["F7"] = "{Binding OpenOperatorComputerLogPageCommand}",
            ["F8"] = "{Binding OpenOperatorComputerSessionPageCommand}",
        };

        foreach (var pair in expected)
        {
            var binding = Assert.Single(
                document.Descendants(),
                element => element.Name.LocalName == "KeyBinding" && (string?)element.Attribute("Gesture") == pair.Key);
            Assert.Equal(pair.Value, (string?)binding.Attribute("Command"));
        }
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));
}
