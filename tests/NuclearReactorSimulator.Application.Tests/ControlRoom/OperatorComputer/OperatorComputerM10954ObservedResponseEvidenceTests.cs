using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer;

public sealed class OperatorComputerM10954ObservedResponseEvidenceTests
{
    [Fact]
    public void EveryCurrentConsequenceDefinition_ProjectsExactlyItsAuthoredMonitorSet()
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        foreach (var definition in OperatorComputerCommandConsequenceCatalog.Definitions)
        {
            var command = Representative(definition);
            var consequence = OperatorComputerCommandConsequenceCatalog.Project(command);
            var samples = OperatorComputerCommandObservationProjector.Project(snapshot, command);

            Assert.True(consequence.HasAuthoredMap);
            Assert.Equal(consequence.MonitorTargets.Count, samples.Count);
            Assert.Equal(
                consequence.MonitorTargets.Select(static item => item.Target.Id).ToArray(),
                samples.Select(static item => item.Target.Id).ToArray());
            Assert.Equal(
                consequence.MonitorTargets.Select(static item => item.Provenance).ToArray(),
                samples.Select(static item => item.Provenance).ToArray());
        }
    }

    [Fact]
    public void CommandConsoleProjection_CarriesCurrentGeneratorLoadMonitorEvidenceDeterministically()
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var first = OperatorComputerCommandConsoleProjector.Project(snapshot);
        var second = OperatorComputerCommandConsoleProjector.Project(snapshot);
        var command = first.Commands.First(item => item.Command.Kind == ControlRoomCommandKind.GeneratorLoadRaise);
        var repeat = second.Commands.First(item => item.EntryId == command.EntryId);
        var gross = command.ObservationSamples.First(item => item.Target.Id == "Electrical.GrossElectricalOutput");
        var shaft = command.ObservationSamples.First(item => item.Target.Id == "TurbineSecondary.TotalTurbineShaftPower");

        Assert.Equal(snapshot.Electrical.GrossElectricalOutput.NumericValue, gross.NumericValue);
        Assert.Equal(snapshot.TurbineSecondary.TotalTurbineShaftPower.NumericValue, shaft.NumericValue);
        Assert.Equal(command.ObservationSamples.ToArray(), repeat.ObservationSamples.ToArray());
    }

    [Fact]
    public void AcceptedDispatch_TracksLogicalStepDeltasWithoutInferringSuccessOrCausality()
    {
        var accumulator = new OperatorComputerCommandObservedResponseAccumulator(observationWindowSteps: 10);
        var command = CommandSnapshot(5d);
        var runtime = Runtime(logicalStep: 100, anyTripActive: false);

        accumulator.BeginAttempt(command, runtime);
        accumulator.MarkAccepted("DISPATCHED — test command accepted.");
        accumulator.Observe(Snapshot(CommandSnapshot(7d), Runtime(logicalStep: 105, anyTripActive: false)));

        var current = accumulator.Current;
        var delta = current.MonitorDeltas.Single();
        Assert.Equal(OperatorComputerCommandDispatchObservationStatus.Accepted, current.Status);
        Assert.Equal(5L, current.ObservedAgeSteps);
        Assert.False(current.WindowComplete);
        Assert.Equal(OperatorComputerObservedResponseDirection.Increased, delta.Direction);
        Assert.Equal(2d, delta.NumericDelta);
        Assert.Equal(5d, delta.Baseline.NumericValue);
        Assert.Equal(7d, delta.Latest.NumericValue);
    }

    [Fact]
    public void ObservationWindow_ClosesAtLogicalThresholdAndRepeatIsDeterministic()
    {
        static OperatorComputerCommandObservedResponseSnapshot Run()
        {
            var accumulator = new OperatorComputerCommandObservedResponseAccumulator(observationWindowSteps: 10);
            accumulator.BeginAttempt(CommandSnapshot(5d), Runtime(logicalStep: 100, anyTripActive: false));
            accumulator.MarkAccepted("DISPATCHED — accepted.");
            accumulator.Observe(Snapshot(CommandSnapshot(6d), Runtime(logicalStep: 110, anyTripActive: true)));
            accumulator.Observe(Snapshot(CommandSnapshot(9d), Runtime(logicalStep: 120, anyTripActive: false)));
            return accumulator.Current;
        }

        var first = Run();
        var second = Run();

        Assert.True(first.WindowComplete);
        Assert.Equal(110L, first.LatestLogicalStep);
        Assert.True(first.ProtectionActiveLatest);
        Assert.Equal(6d, first.MonitorDeltas.Single().Latest.NumericValue);
        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.DispatchLogicalStep, second.DispatchLogicalStep);
        Assert.Equal(first.LatestLogicalStep, second.LatestLogicalStep);
        Assert.Equal(first.WindowComplete, second.WindowComplete);
        Assert.Equal(first.ProtectionActiveLatest, second.ProtectionActiveLatest);
        Assert.Equal(first.MonitorDeltas.ToArray(), second.MonitorDeltas.ToArray());
    }

    [Fact]
    public void RejectedDispatch_RecordsFeedbackButNoFictionalPlantEffectDeltas()
    {
        var accumulator = new OperatorComputerCommandObservedResponseAccumulator();
        var command = CommandSnapshot(5d);

        accumulator.RecordRejected(command, Runtime(logicalStep: 100, anyTripActive: false), "NOT DISPATCHED — blocked.");
        accumulator.Observe(Snapshot(CommandSnapshot(9d), Runtime(logicalStep: 200, anyTripActive: true)));

        Assert.Equal(OperatorComputerCommandDispatchObservationStatus.Rejected, accumulator.Current.Status);
        Assert.True(accumulator.Current.WindowComplete);
        Assert.Empty(accumulator.Current.MonitorDeltas);
        Assert.Equal(100L, accumulator.Current.LatestLogicalStep);
        Assert.False(accumulator.Current.ProtectionActiveLatest);
    }

    private static ControlRoomCommand Representative(OperatorComputerCommandConsequenceDefinition definition)
        => definition.SupportedTargetKinds.Count == 0
            ? new ControlRoomCommand(definition.CommandKind)
            : new ControlRoomCommand(
                definition.CommandKind,
                "test-target",
                definition.SupportedTargetKinds[0],
                definition.CommandKind == ControlRoomCommandKind.TurbineControlValveManualDemandSet ? 37.5d : null);

    private static OperatorComputerCommandSnapshot CommandSnapshot(double grossMegawatts)
    {
        var target = new OperatorComputerCommandConsequenceReference(
            OperatorComputerCommandConsequenceReferenceKind.PublishedState,
            "Electrical.GrossElectricalOutput",
            "GROSS ELECTRICAL OUTPUT");
        var command = new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, "generator-1", ControlRoomCommandTargetKind.Generator);
        return new OperatorComputerCommandSnapshot(
            "electrical-generator-load-raise",
            OperatorComputerCommandGroup.Electrical,
            "RAISE GENERATOR LOAD",
            command,
            OperatorComputerCommandAvailability.Available,
            "BREAKER CLOSED")
        {
            ObservationSamples = new[]
            {
                new OperatorComputerCommandObservationSample(
                    target,
                    OperatorComputerInformationProvenance.Measured,
                    "Observe actual electrical output.",
                    OperatorComputerCommandObservationValueKind.Numeric,
                    grossMegawatts.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                    "MWe",
                    grossMegawatts,
                    null,
                    true),
            },
        };
    }

    private static OperatorComputerRuntimeStatusSnapshot Runtime(long logicalStep, bool anyTripActive)
        => new(logicalStep, ControlRoomRunState.Running, 0, 0, 0, anyTripActive);

    private static OperatorComputerSnapshot Snapshot(
        OperatorComputerCommandSnapshot command,
        OperatorComputerRuntimeStatusSnapshot runtime)
    {
        var pages = OperatorComputerPageCatalog.Default.Select(descriptor =>
            new OperatorComputerPageSnapshot(
                descriptor.Id,
                descriptor.MenuLabel,
                descriptor.Title,
                descriptor.Description,
                OperatorComputerPageContentState.Available));
        return new OperatorComputerSnapshot(
            runtime,
            pages,
            commands: new OperatorComputerCommandConsoleSnapshot(new[] { command }));
    }
}
