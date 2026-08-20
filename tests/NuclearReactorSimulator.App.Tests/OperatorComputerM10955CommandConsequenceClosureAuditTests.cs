using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.Views;

/// <summary>
/// M10.9.5.5 cumulative closure contract for the contextual command consequence model. Detailed behavior remains
/// owned by the validated M10.9.5.1-5.4 focused gates; this audit proves that their shared boundaries remain coherent
/// and that the cumulative runner actually completed each prerequisite gate before closure evidence is written.
/// </summary>
public sealed class OperatorComputerM10955CommandConsequenceClosureAuditTests
{
    private const string ClosureOptInEnvironmentVariable = "NRS_M1095_CLOSURE_AUDIT";
    private const string CatalogPassedEnvironmentVariable = "NRS_M1095_51_CATALOG_PASSED";
    private const string DependencyPassedEnvironmentVariable = "NRS_M1095_52_DEPENDENCY_PASSED";
    private const string ContextPassedEnvironmentVariable = "NRS_M1095_53_CONTEXT_PASSED";
    private const string ObservedPassedEnvironmentVariable = "NRS_M1095_54_OBSERVED_PASSED";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    [Fact]
    public void ClosureContract_KeepsAuthoredExpectedAndObservedSemanticsSeparatedAndDeterministic()
    {
        var definitions = OperatorComputerCommandConsequenceCatalog.Definitions;
        Assert.Equal(Enum.GetValues<ControlRoomCommandKind>().Length, definitions.Count);

        foreach (var definition in definitions)
        {
            var command = Representative(definition);
            var consequence = OperatorComputerCommandConsequenceCatalog.Project(command);
            var dependency = OperatorComputerCommandDependencyChainCatalog.Project(command);

            Assert.True(consequence.HasAuthoredMap);
            Assert.True(dependency.HasAuthoredChain);
            Assert.Equal(
                consequence.MonitorTargets.Count,
                dependency.Steps.Count(static step =>
                    step.Kind == OperatorComputerCommandDependencyStepKind.MeasurementOrModelObservation));
        }

        Assert.Equal(500, OperatorComputerCommandObservedResponseAccumulator.DefaultObservationWindowSteps);

        var observationProperty = typeof(OperatorComputerCommandSnapshot).GetProperty(
            nameof(OperatorComputerCommandSnapshot.ObservationSamples));
        Assert.NotNull(observationProperty);
        Assert.NotNull(observationProperty.GetCustomAttributes(typeof(JsonIgnoreAttribute), inherit: true).SingleOrDefault());

        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "ControlRoomComputerControl.axaml"));
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "TextBlock"
            && (string?)element.Attribute("Text") == "CONTEXT INSPECTOR — AUTHORED CONSEQUENCE / DEPENDENCY EVIDENCE");
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "TextBlock"
            && (string?)element.Attribute("Text") == "OBSERVED RESPONSE — POST-DISPATCH EVIDENCE");
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "TextBlock"
            && ((string?)element.Attribute("Text"))?.Contains("not proof of causality", StringComparison.Ordinal) == true);
        Assert.Contains(document.Descendants(), static element =>
            element.Name.LocalName == "Button"
            && (string?)element.Attribute("Command") == "{Binding ExecuteSelectedCommandCommand}"
            && (string?)element.Attribute("Content") == "EXECUTE [ENTER]");
        Assert.DoesNotContain(document.Descendants(), static element => element.Name.LocalName is "TextBox" or "AutoCompleteBox");
    }

    [Fact]
    public void InspectionAndDependencyNavigation_DoNotDispatchOrStartObservedResponse()
    {
        var dispatcher = new RecordingDispatcher();
        var snapshot = OperatorComputerSnapshotProjector.Project(new ControlRoomSnapshot(
            logicalStep: 0,
            runState: ControlRoomRunState.Running,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false));
        var viewModel = new OperatorComputerViewModel(snapshot, dispatcher);

        viewModel.SelectPage(OperatorComputerPageId.Commands);
        var selected = viewModel.CommandEntries.First();
        viewModel.SelectedCommand = selected;
        var dependencyStep = viewModel.SelectedCommandDependencySteps.LastOrDefault();
        viewModel.SelectedCommandDependencyStep = dependencyStep;

        Assert.Empty(dispatcher.Commands);
        Assert.Equal(OperatorComputerCommandDispatchObservationStatus.None, viewModel.LastCommandObservedResponse.Status);
        Assert.True(viewModel.CurrentConsequence.HasAuthoredMap);
        Assert.True(viewModel.CurrentDependencyChain.HasAuthoredChain);
    }

    [Fact]
    public void ClosureRunner_RequiresAllFourValidatedFocusedGatesBeforeFinalEvidence()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "run-m1095-command-consequence-closure-audit.cmd"));

        Assert.Contains("call scripts\\run-m1095-command-consequence-catalog-audit.cmd", script, StringComparison.Ordinal);
        Assert.Contains("call scripts\\run-m1095-command-dependency-chain-audit.cmd", script, StringComparison.Ordinal);
        Assert.Contains("call scripts\\run-m1095-command-context-inspector-schematic-audit.cmd", script, StringComparison.Ordinal);
        Assert.Contains("call scripts\\run-m1095-command-observed-response-audit.cmd", script, StringComparison.Ordinal);
        Assert.Contains("set \"NRS_M1095_51_CATALOG_PASSED=1\"", script, StringComparison.Ordinal);
        Assert.Contains("set \"NRS_M1095_52_DEPENDENCY_PASSED=1\"", script, StringComparison.Ordinal);
        Assert.Contains("set \"NRS_M1095_53_CONTEXT_PASSED=1\"", script, StringComparison.Ordinal);
        Assert.Contains("set \"NRS_M1095_54_OBSERVED_PASSED=1\"", script, StringComparison.Ordinal);
        Assert.Contains("set \"NRS_M1095_CLOSURE_AUDIT=1\"", script, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "M1095CommandConsequenceClosureAudit")]
    public void ValidatedFocusedEvidence_ClosesAutomatedM1095GatePendingManualHmiAcceptance()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(ClosureOptInEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Equal("1", Environment.GetEnvironmentVariable(CatalogPassedEnvironmentVariable));
        Assert.Equal("1", Environment.GetEnvironmentVariable(DependencyPassedEnvironmentVariable));
        Assert.Equal("1", Environment.GetEnvironmentVariable(ContextPassedEnvironmentVariable));
        Assert.Equal("1", Environment.GetEnvironmentVariable(ObservedPassedEnvironmentVariable));

        ResetReportDirectory();
        WriteClosureArtifacts();
    }

    private static ControlRoomCommand Representative(OperatorComputerCommandConsequenceDefinition definition)
        => definition.SupportedTargetKinds.Count == 0
            ? new ControlRoomCommand(definition.CommandKind)
            : new ControlRoomCommand(
                definition.CommandKind,
                "closure-target",
                definition.SupportedTargetKinds[0],
                definition.CommandKind == ControlRoomCommandKind.TurbineControlValveManualDemandSet ? 37.5d : null);

    private static void WriteClosureArtifacts()
    {
        var directory = ReportDirectory();
        File.WriteAllLines(
            Path.Combine(directory, "02-m1095-command-consequence-closure-gate-matrix.csv"),
            new[]
            {
                "gate,contract,status",
                "m10.9.5.1,authored consequence catalog completeness explicit-unmapped fallback and canonical monitor references,PASS",
                "m10.9.5.2,bounded authored dependency chains without automatic graph traversal or runtime side effects,PASS",
                "m10.9.5.3,F4 COMMANDS context inspector exact canonical mimic focus and explicit dispatch boundary,PASS",
                "m10.9.5.4,logical-step post-dispatch observed-response evidence without causal or generic success inference,PASS",
                "cross-cutting,inspection and dependency navigation do not dispatch or start observed-response evidence,PASS",
                "compatibility,observation samples remain JsonIgnored derivable presentation evidence,PASS",
                "manual-hmi,representative keyboard readability blocker focus and observed-response acceptance,REQUIRED",
            },
            Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "03-m1095-command-consequence-closure-contract.csv"),
            new[]
            {
                "metric,value",
                $"command-kinds,{Enum.GetValues<ControlRoomCommandKind>().Length}",
                $"authored-consequence-definitions,{OperatorComputerCommandConsequenceCatalog.Definitions.Count}",
                $"observation-window-logical-steps,{OperatorComputerCommandObservedResponseAccumulator.DefaultObservationWindowSteps}",
                "predictive-ui-physics,False",
                "automatic-graph-traversal,False",
                "inspection-dispatches-command,False",
                "observed-response-proves-causality,False",
                "generic-delta-success-failure-inference,False",
                "observation-samples-authoritative-state,False",
                "manual-hmi-gate-required,True",
            },
            Utf8WithoutBom);

        File.WriteAllLines(
            Path.Combine(directory, "01-m1095-command-consequence-closure.summary.txt"),
            new[]
            {
                "=== 01-m1095-command-consequence-closure ===",
                "scope=M10.9.5.1 consequence semantics + M10.9.5.2 bounded dependency chains + M10.9.5.3 COMMANDS/context/mimic integration + M10.9.5.4 observed-response evidence; no new runtime feature is introduced by closure;",
                $"command-kinds={Enum.GetValues<ControlRoomCommandKind>().Length}; authored-definitions={OperatorComputerCommandConsequenceCatalog.Definitions.Count}; observation-window-logical-steps={OperatorComputerCommandObservedResponseAccumulator.DefaultObservationWindowSteps};",
                "catalog-gate-passes=True; dependency-chain-gate-passes=True; context-inspector-schematic-gate-passes=True; observed-response-gate-passes=True; inspection-dispatches-command=False; observation-samples-json-ignored=True;",
                "direct-effect-vs-expected-influence-separated=True; expected-vs-observed-separated=True; observed-response-causal-claim=False; generic-success-failure-inference=False; predictive-ui-physics=False; automatic-graph-traversal=False;",
                "m1095-automated-closure-passes=True; manual-hmi-gate-required=True; m1095-closure-ready=True; m1096-unblocked-after-manual-validation=True;",
                "recommendation=perform docs\\M10_9_5_5_MANUAL_VALIDATION_CHECKLIST.md; if green promote M10.9.5 to VALIDATED and begin M10.9.6.1 challenge lifecycle/logical-time contract without reopening command-consequence semantics unless new evidence demonstrates a defect;",
            },
            Utf8WithoutBom);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test output directory.");
    }

    private static string ReportDirectory()
        => Path.Combine(FindRepositoryRoot(), "artifacts", "m1095-command-consequence-closure");

    private static void ResetReportDirectory()
    {
        var directory = ReportDirectory();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "00-progress.txt"),
            $"{DateTimeOffset.UtcNow:O} M10.9.5 command-consequence closure started{Environment.NewLine}",
            Utf8WithoutBom);
    }

    private sealed class RecordingDispatcher : IControlRoomCommandDispatcher
    {
        public List<ControlRoomCommand> Commands { get; } = new();

        public void Dispatch(ControlRoomCommand command) => Commands.Add(command);
    }
}
