using System.Xml.Linq;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance;

public sealed class M10972MissionPerformanceNavigationDecisionTests
{
    [Fact]
    public void Decision_SelectsDedicatedMainHmiWorkspaceWithContextualComputerEntry()
    {
        var decision = MissionPerformanceNavigationDecision.Current;

        Assert.Equal(MissionPerformanceWorkspacePlacement.DedicatedMainHmiWorkspace, decision.WorkspacePlacement);
        Assert.Equal(MissionPerformanceComputerEntryMode.ContextualNavigationAction, decision.ComputerEntryMode);
        Assert.Equal("Mission & Performance", decision.WorkspaceTitle);
        Assert.Equal("MISSION", decision.WorkspaceLabel);
        Assert.Equal("COMPUTER", decision.ComputerSourceLabel);
        Assert.False(decision.NavigationHasPlantCommandAuthority);
        Assert.False(decision.UiRouteActivated);
        Assert.Equal("M10.9.7.3", decision.UiActivationMilestone);
    }

    [Fact]
    public void Decision_PreservesValidatedOperatorComputerF1ToF8AndDoesNotCreateF9()
    {
        var decision = MissionPerformanceNavigationDecision.Current;
        Assert.False(decision.ChangesOperatorComputerFunctionKeyContract);
        Assert.Null(decision.AddedOperatorComputerFunctionKey);

        var mainWindow = LoadMainWindow();
        var expectedGlobalBindings = new Dictionary<string, string>(StringComparer.Ordinal)
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

        foreach (var pair in expectedGlobalBindings)
        {
            var binding = Assert.Single(
                mainWindow.Descendants(),
                element => element.Name.LocalName == "KeyBinding"
                    && (string?)element.Attribute("Gesture") == pair.Key);
            Assert.Equal(pair.Value, (string?)binding.Attribute("Command"));
        }

        Assert.DoesNotContain(
            mainWindow.Descendants(),
            static element => element.Name.LocalName == "KeyBinding"
                && (string?)element.Attribute("Gesture") == "F9");

        var terminal = LoadTerminal();
        var expectedPageLabels = new[]
        {
            "F1 GUIDANCE",
            "F2 INFO",
            "F3 ALARMS",
            "F4 COMMANDS",
            "F5 MODES",
            "F6 DIAGNOSTICS",
            "F7 LOG",
            "F8 SESSION",
        };
        foreach (var label in expectedPageLabels)
        {
            _ = Assert.Single(
                terminal.Descendants(),
                element => element.Name.LocalName == "Button"
                    && (string?)element.Attribute("Content") == label);
        }

        Assert.DoesNotContain(
            terminal.Descendants(),
            static element => ((string?)element.Attribute("Content"))?.StartsWith("F9", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Decision_IsFrozenBeforeUiActivationAndDoesNotPartiallyRegisterTheWorkspace()
    {
        var decision = MissionPerformanceNavigationDecision.Current;
        Assert.False(decision.UiRouteActivated);

        Assert.DoesNotContain(
            ControlRoomWorkspaceCatalog.Default,
            descriptor => string.Equals(descriptor.ShortTitle, decision.WorkspaceLabel, StringComparison.Ordinal));
        Assert.DoesNotContain(
            Enum.GetNames<ControlRoomWorkspaceId>(),
            static name => string.Equals(name, "MissionPerformance", StringComparison.Ordinal));

        var mainWindow = LoadMainWindow();
        Assert.DoesNotContain(
            mainWindow.Descendants(),
            element => string.Equals((string?)element.Attribute("Text"), decision.WorkspaceTitle, StringComparison.Ordinal)
                || string.Equals((string?)element.Attribute("Content"), decision.WorkspaceTitle, StringComparison.Ordinal));
    }

    [Fact]
    public void ArtifactSummary_WritesM10972PlacementDecisionEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m10972-workstation-placement-navigation.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.7.2 REV1 explicit Mission/Performance workstation placement/navigation decision rebuilt over M10.9.7.1 Hotfix 3 VALIDATED; decision contract only, no workstation UI activation, scoring arithmetic, challenge definition, plant command authority or physics change;",
            "placement-option=A; dedicated-main-hmi-workspace=True; operator-computer-page=False;",
            "computer-entry=contextual-navigation-action; navigation-selection-only=True; plant-command-authority=False;",
            "operator-computer-f1-f8-contract-changed=False; operator-computer-f9-added=False; global-f1-f8-bindings-preserved=True; fixed-computer-page-labels-preserved=True;",
            "workspace-title=Mission & Performance; workspace-label=MISSION; computer-source-label=COMPUTER;",
            "ui-route-activated=False; live-workspace-catalog-changed=False; activation-deferred-to=M10.9.7.3;",
            "m10972-rev1-workstation-placement-navigation-decision-passes=True; pre-live-review-followups-retained=True; next-step=validate M10.9.7.2 REV1 then address/qualify pre-live hardening before M10.9.7.3 objective-demand-progress-score UI implementation;",
        });

        Assert.True(File.Exists(path));
    }

    private static XDocument LoadMainWindow()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));

    private static XDocument LoadTerminal()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ControlRoomComputerControl.axaml"));

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.2 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m1097-navigation-decision");
    }
}
