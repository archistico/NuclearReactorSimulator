using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class OperatorComputerM10954ObservedResponseViewModelTests
{
    [Fact]
    public void AcceptedCommand_ShowsPostDispatchBaselineLatestAndDirectionWithoutGenericSuccessClaim()
    {
        var dispatcher = new RecordingDispatcher();
        var initial = Snapshot(Command(5d, OperatorComputerCommandAvailability.Available), logicalStep: 100);
        var viewModel = new OperatorComputerViewModel(initial, dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);

        viewModel.ExecuteSelectedCommandCommand.Execute(null);
        viewModel.UpdateSnapshot(Snapshot(Command(7d, OperatorComputerCommandAvailability.Available), logicalStep: 105));

        Assert.Single(dispatcher.Commands);
        Assert.Equal(OperatorComputerCommandDispatchObservationStatus.Accepted, viewModel.LastCommandObservedResponse.Status);
        Assert.Contains("OBSERVED MONITOR DELTAS", viewModel.LastCommandObservedResponseText, StringComparison.Ordinal);
        Assert.Contains("5.0 MWe → 7.0 MWe · INCREASED", viewModel.LastCommandObservedResponseText, StringComparison.Ordinal);
        Assert.Contains("post-dispatch co-variation only", viewModel.LastCommandObservedResponseText, StringComparison.Ordinal);
        Assert.DoesNotContain("OUTCOME      SUCCESS", viewModel.LastCommandObservedResponseText, StringComparison.Ordinal);
    }

    [Fact]
    public void AdvisoryBlockedCommand_ShowsRejectedEvidenceWithoutPlantDeltasOrDispatch()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = new OperatorComputerViewModel(
            Snapshot(Command(5d, OperatorComputerCommandAvailability.Blocked, "Generator breaker is open."), logicalStep: 100),
            dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);

        viewModel.ExecuteSelectedCommandCommand.Execute(null);

        Assert.Empty(dispatcher.Commands);
        Assert.Equal(OperatorComputerCommandDispatchObservationStatus.Rejected, viewModel.LastCommandObservedResponse.Status);
        Assert.Contains("PLANT EFFECTS NOT INFERRED", viewModel.LastCommandObservedResponseText, StringComparison.Ordinal);
        Assert.DoesNotContain("OBSERVED MONITOR DELTAS", viewModel.LastCommandObservedResponseText, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeRejectedCommand_ShowsCanonicalRuntimeFeedbackAndNoPlantDeltas()
    {
        var dispatcher = new ThrowingDispatcher();
        var viewModel = new OperatorComputerViewModel(
            Snapshot(Command(5d, OperatorComputerCommandAvailability.Available), logicalStep: 100),
            dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);

        viewModel.ExecuteSelectedCommandCommand.Execute(null);

        Assert.Equal(OperatorComputerCommandDispatchObservationStatus.Rejected, viewModel.LastCommandObservedResponse.Status);
        Assert.Contains("BLOCKED BY RUNTIME/SCENARIO", viewModel.LastCommandObservedResponseText, StringComparison.Ordinal);
        Assert.Empty(viewModel.LastCommandObservedResponse.MonitorDeltas);
    }

    private static OperatorComputerCommandSnapshot Command(
        double grossMegawatts,
        OperatorComputerCommandAvailability availability,
        string? blockReason = null)
    {
        var target = new OperatorComputerCommandConsequenceReference(
            OperatorComputerCommandConsequenceReferenceKind.PublishedState,
            "Electrical.GrossElectricalOutput",
            "GROSS ELECTRICAL OUTPUT");
        return new OperatorComputerCommandSnapshot(
            "electrical-generator-load-raise",
            OperatorComputerCommandGroup.Electrical,
            "RAISE GENERATOR LOAD",
            new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, "generator-1", ControlRoomCommandTargetKind.Generator),
            availability,
            "GENERATOR",
            blockReason)
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

    private static OperatorComputerSnapshot Snapshot(OperatorComputerCommandSnapshot command, long logicalStep)
    {
        var pages = OperatorComputerPageCatalog.Default.Select(descriptor =>
            new OperatorComputerPageSnapshot(
                descriptor.Id,
                descriptor.MenuLabel,
                descriptor.Title,
                descriptor.Description,
                OperatorComputerPageContentState.Available));
        return new OperatorComputerSnapshot(
            new OperatorComputerRuntimeStatusSnapshot(logicalStep, ControlRoomRunState.Running, 0, 0, 0, false),
            pages,
            commands: new OperatorComputerCommandConsoleSnapshot(new[] { command }));
    }

    private sealed class RecordingDispatcher : IControlRoomCommandDispatcher
    {
        public List<ControlRoomCommand> Commands { get; } = new();
        public void Dispatch(ControlRoomCommand command) => Commands.Add(command);
    }

    private sealed class ThrowingDispatcher : IControlRoomCommandDispatcher
    {
        public void Dispatch(ControlRoomCommand command)
            => throw new InvalidOperationException("Canonical runtime rejected the typed intent.");
    }
}
