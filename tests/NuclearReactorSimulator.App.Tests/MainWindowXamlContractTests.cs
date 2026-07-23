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
            ["SPEED LOWER"] = ("{Binding TurbineSpeedLowerCommand}", "{Binding TurbineSpeedCommandState}"),
            ["SPEED RAISE"] = ("{Binding TurbineSpeedRaiseCommand}", "{Binding TurbineSpeedCommandState}"),
            ["LOAD LOWER"] = ("{Binding GeneratorLoadLowerCommand}", "{Binding GeneratorLoadCommandState}"),
            ["LOAD RAISE"] = ("{Binding GeneratorLoadRaiseCommand}", "{Binding GeneratorLoadCommandState}"),
            ["CLOSE BREAKER"] = ("{Binding GeneratorBreakerCloseCommand}", "{Binding BreakerCloseCommandState}"),
            ["OPEN BREAKER"] = ("{Binding GeneratorBreakerOpenCommand}", "{Binding BreakerOpenCommandState}"),
            ["INSERT"] = ("{Binding RodInsertCommand}", "{Binding RodCommandState}"),
            ["HOLD"] = ("{Binding RodHoldCommand}", "{Binding RodCommandState}"),
            ["WITHDRAW"] = ("{Binding RodWithdrawCommand}", "{Binding RodWithdrawCommandState}"),
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
    public void PersistentOperationalButtons_BindActualStateSeparatelyFromCommandAvailability()
    {
        var document = LoadMainWindow();
        var buttons = document.Descendants()
            .Where(static element => element.Name.LocalName == "ControlRoomPushButton")
            .ToArray();

        AssertPersistent(buttons, "START / RUN", "{Binding PumpStartCommandActive}", "{Binding PumpStartCommandEnabled}");
        AssertPersistent(buttons, "STOP", "{Binding PumpStopCommandActive}", "{Binding PumpStopCommandEnabled}");
        AssertPersistent(buttons, "INSERT", "{Binding RodInsertCommandActive}", "{Binding RodInsertCommandEnabled}");
        AssertPersistent(buttons, "HOLD", "{Binding RodHoldCommandActive}", "{Binding RodHoldCommandEnabled}");
        AssertPersistent(buttons, "WITHDRAW", "{Binding RodWithdrawCommandActive}", "{Binding RodWithdrawCommandEnabled}");
        AssertPersistent(buttons, "CLOSE BREAKER", "{Binding BreakerCloseCommandActive}", "{Binding BreakerCloseCommandEnabled}");
        AssertPersistent(buttons, "OPEN BREAKER", "{Binding BreakerOpenCommandActive}", "{Binding BreakerOpenCommandEnabled}");

        foreach (var label in new[] { "SPEED LOWER", "SPEED RAISE", "LOAD LOWER", "LOAD RAISE" })
        {
            var button = Assert.Single(buttons, element => (string?)element.Attribute("Label") == label);
            Assert.Null(button.Attribute("IsActive"));
        }
    }

    [Fact]
    public void ProtectionTripButtons_SeparateLatchedVisualStateFromCommandAvailability()
    {
        var document = LoadMainWindow();
        var buttons = document.Descendants()
            .Where(static element => element.Name.LocalName == "ControlRoomPushButton")
            .ToArray();

        var turbineTrip = Assert.Single(buttons, static element => (string?)element.Attribute("Command") == "{Binding TurbineTripCommand}");
        Assert.Equal("{Binding TurbineTripCommandLabel}", (string?)turbineTrip.Attribute("Label"));
        Assert.Equal("{Binding TurbineTripCommandState}", (string?)turbineTrip.Attribute("State"));
        Assert.Equal("{Binding TurbineTripCommandEnabled}", (string?)turbineTrip.Attribute("IsCommandEnabled"));

        var generatorTrip = Assert.Single(buttons, static element => (string?)element.Attribute("Command") == "{Binding GeneratorTripCommand}");
        Assert.Equal("{Binding GeneratorTripCommandLabel}", (string?)generatorTrip.Attribute("Label"));
        Assert.Equal("{Binding GeneratorTripCommandState}", (string?)generatorTrip.Attribute("State"));
        Assert.Equal("{Binding GeneratorTripCommandEnabled}", (string?)generatorTrip.Attribute("IsCommandEnabled"));

        var scram = Assert.Single(buttons, static element => (string?)element.Attribute("Command") == "{Binding ReactorScramCommand}");
        Assert.Equal("{Binding ScramCommandLabel}", (string?)scram.Attribute("Label"));
        Assert.Equal("{Binding ScramCommandEnabled}", (string?)scram.Attribute("IsCommandEnabled"));

        var resets = buttons.Where(static element => (string?)element.Attribute("Command") == "{Binding ProtectionResetCommand}").ToArray();
        Assert.Equal(3, resets.Length);
        Assert.All(resets, static reset =>
        {
            Assert.Equal("{Binding ProtectionResetCommandState}", (string?)reset.Attribute("State"));
            Assert.Equal("{Binding ProtectionResetCommandEnabled}", (string?)reset.Attribute("IsCommandEnabled"));
        });
    }

    [Fact]
    public void ElectricalWorkspace_UsesParalleledAwareSynchronizationPresentation()
    {
        var document = LoadMainWindow();

        var synchronizationLamp = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "ControlRoomIndicatorLamp"
                && (string?)element.Attribute("Label") == "{Binding SynchronizationLabel}");
        Assert.Equal("{Binding DisplaySynchronizationState}", (string?)synchronizationLamp.Attribute("State"));

        Assert.Contains(
            document.Descendants(),
            static element => (string?)element.Attribute("Text") == "{Binding SelectedGeneratorSynchronizationDetailText}");
    }

    [Fact]
    public void Overview_ExposesCanonicalNextActionAndStartupToPowerCommandMap()
    {
        var document = LoadMainWindow();
        var textBindings = document.Descendants()
            .Select(static element => (string?)element.Attribute("Text"))
            .Where(static text => text is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("{Binding OperatorCurrentConditionText}", textBindings);
        Assert.Contains("{Binding OperatorNextActionText}", textBindings);
        Assert.Contains("{Binding StartupToPowerCommandPathText}", textBindings);
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

        Assert.Equal("{Binding IsMainWorkspaceScrollVisible}", (string?)centerScroll.Attribute("IsVisible"));

        var integrationGrid = Assert.IsType<XElement>(centerScroll.Parent);
        Assert.Equal("Grid", integrationGrid.Name.LocalName);

        var centerHost = Assert.IsType<XElement>(integrationGrid.Parent);
        Assert.Equal("Border", centerHost.Name.LocalName);
        Assert.Equal("1", (string?)centerHost.Attribute("Grid.Column"));
        Assert.Equal("True", (string?)centerHost.Attribute("ClipToBounds"));

        var centerGrid = Assert.IsType<XElement>(centerHost.Parent);
        Assert.Equal("Grid", centerGrid.Name.LocalName);
        Assert.Equal("1", (string?)centerGrid.Attribute("Grid.Row"));
        Assert.Equal("188,*,300", (string?)centerGrid.Attribute("ColumnDefinitions"));
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
        Assert.Equal("Grid", computerHost.Name.LocalName);
        Assert.Equal("{Binding IsOperatorComputerWorkspaceSelected}", (string?)computerHost.Attribute("IsVisible"));
        Assert.Equal("Auto,*", (string?)computerHost.Attribute("RowDefinitions"));
        Assert.Equal("1", (string?)computer.Attribute("Grid.Row"));
        Assert.DoesNotContain(
            computer.Ancestors(),
            static element => element.Name.LocalName == "ScrollViewer"
                && element.Attributes().Any(attribute => attribute.Name.LocalName == "Name" && attribute.Value == "MainWorkspaceScroll"));

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

    private static void AssertPersistent(
        IEnumerable<XElement> buttons,
        string label,
        string activeBinding,
        string enabledBinding)
    {
        var button = Assert.Single(buttons, element => (string?)element.Attribute("Label") == label);
        Assert.Equal(activeBinding, (string?)button.Attribute("IsActive"));
        Assert.Equal(enabledBinding, (string?)button.Attribute("IsCommandEnabled"));
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));
}
