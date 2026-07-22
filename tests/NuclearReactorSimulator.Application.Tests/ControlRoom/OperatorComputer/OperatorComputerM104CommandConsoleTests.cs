using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer;

public sealed class OperatorComputerM104CommandConsoleTests
{
    [Fact]
    public void ShellOnlyProjection_ActivatesCommandsPageButMarksRuntimeAndProtectionCommandsUnavailable()
    {
        var projected = OperatorComputerSnapshotProjector.Project(ControlRoomSnapshot.ShellOnly);

        Assert.Equal(
            OperatorComputerPageContentState.Available,
            projected.Pages.Single(static page => page.Id == OperatorComputerPageId.Commands).ContentState);
        Assert.NotNull(projected.Commands);
        Assert.All(
            projected.Commands!.Commands.Where(static command => command.Group is OperatorComputerCommandGroup.Runtime or OperatorComputerCommandGroup.Protection),
            static command => Assert.Equal(OperatorComputerCommandAvailability.Unavailable, command.Availability));
        Assert.Contains(projected.Commands.Commands, static command => command.Command.Kind == ControlRoomCommandKind.AlarmAcknowledgeAll);
        Assert.Contains(projected.Commands.Commands, static command => command.Command.Kind == ControlRoomCommandKind.AlarmResetAll);
    }

    [Fact]
    public void PausedRuntime_ProjectsDeterministicHostAvailabilityWithoutReplacingRuntimeValidation()
    {
        var source = new ControlRoomSnapshot(
            logicalStep: 12,
            runState: ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false);

        var commands = OperatorComputerCommandConsoleProjector.Project(source);

        Assert.Equal(OperatorComputerCommandAvailability.Available, Entry(commands, ControlRoomCommandKind.Run).Availability);
        Assert.Equal(OperatorComputerCommandAvailability.Blocked, Entry(commands, ControlRoomCommandKind.Pause).Availability);
        Assert.Equal(OperatorComputerCommandAvailability.Available, Entry(commands, ControlRoomCommandKind.SingleStep).Availability);
        var reset = Entry(commands, ControlRoomCommandKind.ProtectionReset);
        Assert.Equal(OperatorComputerCommandAvailability.Blocked, reset.Availability);
        Assert.NotNull(reset.BlockReason);
        Assert.Contains("No latched trip", reset.BlockReason!);
    }


    [Fact]
    public void ProtectionReset_UsesCanonicalPublishedReadinessInsteadOfTreatingEveryTripAsAvailable()
    {
        var blockedSnapshot = new ControlRoomSnapshot(
            logicalStep: 12,
            runState: ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: true,
            generatorTripActive: false,
            protectionReset: new ProtectionResetPresentationSnapshot(
                anyTripActive: true,
                resetConditionsSatisfied: false,
                lastResetRequested: false,
                lastResetAccepted: false,
                blockers: new[] { "turbine-trip: reset condition not safe" }));
        var availableSnapshot = new ControlRoomSnapshot(
            logicalStep: 13,
            runState: ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: true,
            generatorTripActive: false,
            protectionReset: new ProtectionResetPresentationSnapshot(
                anyTripActive: true,
                resetConditionsSatisfied: true,
                lastResetRequested: false,
                lastResetAccepted: false));

        var blocked = Entry(OperatorComputerCommandConsoleProjector.Project(blockedSnapshot), ControlRoomCommandKind.ProtectionReset);
        var available = Entry(OperatorComputerCommandConsoleProjector.Project(availableSnapshot), ControlRoomCommandKind.ProtectionReset);

        Assert.Equal(OperatorComputerCommandAvailability.Blocked, blocked.Availability);
        Assert.Contains("reset condition not safe", blocked.BlockReason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(OperatorComputerCommandAvailability.Available, available.Availability);
    }

    [Fact]
    public void IntegratedLowLoadSnapshot_ExpandsCanonicalTargetsWithoutInventingTargetKinds()
    {
        var snapshot = new PowerManoeuvringInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var commands = OperatorComputerCommandConsoleProjector.Project(snapshot);

        Assert.Equal(commands.Commands.Count, commands.Commands.Select(static command => command.EntryId).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(commands.Commands, static command => command.Command.Kind == ControlRoomCommandKind.ControlRodInsert && command.Command.TargetKind is ControlRoomCommandTargetKind.ControlRod or ControlRoomCommandTargetKind.ControlRodGroup);
        Assert.Contains(commands.Commands, static command => command.Command.Kind == ControlRoomCommandKind.MainCirculationPumpStart && command.Command.TargetKind == ControlRoomCommandTargetKind.Pump);
        Assert.Contains(commands.Commands, static command => command.Command.Kind == ControlRoomCommandKind.TurbineSpeedRaise && command.Command.TargetKind == ControlRoomCommandTargetKind.TurbineRotor);
        Assert.Contains(commands.Commands, static command => command.Command.Kind == ControlRoomCommandKind.GeneratorLoadLower && command.Command.TargetKind == ControlRoomCommandTargetKind.Generator);
        Assert.Contains(commands.Commands, static command => command.Command.Kind == ControlRoomCommandKind.GeneratorBreakerOpen && command.Command.TargetKind == ControlRoomCommandTargetKind.Breaker);
    }

    [Fact]
    public void CommandConsoleSnapshot_FreezesRowsAndRequiresStableUniqueEntryIds()
    {
        var command = new OperatorComputerCommandSnapshot(
            "runtime-run",
            OperatorComputerCommandGroup.Runtime,
            "RUN",
            new ControlRoomCommand(ControlRoomCommandKind.Run),
            OperatorComputerCommandAvailability.Available,
            "PAUSED");
        var snapshot = new OperatorComputerCommandConsoleSnapshot(new[] { command });
        var rows = Assert.IsAssignableFrom<IList<OperatorComputerCommandSnapshot>>(snapshot.Commands);

        Assert.Throws<NotSupportedException>(() => rows.Clear());
        Assert.Throws<ArgumentException>(() => new OperatorComputerCommandConsoleSnapshot(new[] { command, command }));
        Assert.Throws<ArgumentException>(() => new OperatorComputerCommandSnapshot(
            "blocked",
            OperatorComputerCommandGroup.Runtime,
            "PAUSE",
            new ControlRoomCommand(ControlRoomCommandKind.Pause),
            OperatorComputerCommandAvailability.Blocked,
            "PAUSED"));
    }

    private static OperatorComputerCommandSnapshot Entry(
        OperatorComputerCommandConsoleSnapshot snapshot,
        ControlRoomCommandKind kind)
        => snapshot.Commands.Single(command => command.Command.Kind == kind && command.Command.TargetId is null);
}
