using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public static class OperatorComputerCommandConsoleProjector
{
    public static OperatorComputerCommandConsoleSnapshot Project(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var commands = new List<OperatorComputerCommandSnapshot>();
        AddRuntimeCommands(commands, snapshot);
        AddProtectionCommands(commands, snapshot);
        AddReactorCommands(commands, snapshot);
        AddPrimaryCommands(commands, snapshot);
        AddTurbineElectricalCommands(commands, snapshot);
        AddAlarmCommands(commands, snapshot);
        return new OperatorComputerCommandConsoleSnapshot(commands);
    }

    private static void AddRuntimeCommands(List<OperatorComputerCommandSnapshot> commands, ControlRoomSnapshot snapshot)
    {
        if (snapshot.RunState == ControlRoomRunState.ShellOnly)
        {
            commands.Add(Unavailable("runtime-run", OperatorComputerCommandGroup.Runtime, "RUN", ControlRoomCommandKind.Run, "SHELL ONLY", "No integrated runtime is attached."));
            commands.Add(Unavailable("runtime-pause", OperatorComputerCommandGroup.Runtime, "PAUSE", ControlRoomCommandKind.Pause, "SHELL ONLY", "No integrated runtime is attached."));
            commands.Add(Unavailable("runtime-single-step", OperatorComputerCommandGroup.Runtime, "SINGLE STEP", ControlRoomCommandKind.SingleStep, "SHELL ONLY", "No integrated runtime is attached."));
            return;
        }

        commands.Add(snapshot.RunState == ControlRoomRunState.Running
            ? Blocked("runtime-run", OperatorComputerCommandGroup.Runtime, "RUN", ControlRoomCommandKind.Run, "RUNNING", "Runtime is already running.")
            : Available("runtime-run", OperatorComputerCommandGroup.Runtime, "RUN", ControlRoomCommandKind.Run, "PAUSED"));
        commands.Add(snapshot.RunState == ControlRoomRunState.Paused
            ? Blocked("runtime-pause", OperatorComputerCommandGroup.Runtime, "PAUSE", ControlRoomCommandKind.Pause, "PAUSED", "Runtime is already paused.")
            : Available("runtime-pause", OperatorComputerCommandGroup.Runtime, "PAUSE", ControlRoomCommandKind.Pause, "RUNNING"));
        commands.Add(snapshot.RunState == ControlRoomRunState.Running
            ? Blocked("runtime-single-step", OperatorComputerCommandGroup.Runtime, "SINGLE STEP", ControlRoomCommandKind.SingleStep, "RUNNING", "Pause the runtime before requesting a single deterministic step.")
            : Available("runtime-single-step", OperatorComputerCommandGroup.Runtime, "SINGLE STEP", ControlRoomCommandKind.SingleStep, "PAUSED"));
    }

    private static void AddProtectionCommands(List<OperatorComputerCommandSnapshot> commands, ControlRoomSnapshot snapshot)
    {
        if (snapshot.RunState == ControlRoomRunState.ShellOnly)
        {
            commands.Add(Unavailable("protection-scram", OperatorComputerCommandGroup.Protection, "REACTOR SCRAM", ControlRoomCommandKind.ReactorScram, "SHELL ONLY", "No integrated runtime is attached."));
            commands.Add(Unavailable("protection-reset", OperatorComputerCommandGroup.Protection, "PROTECTION RESET", ControlRoomCommandKind.ProtectionReset, "SHELL ONLY", "No integrated runtime is attached."));
            commands.Add(Unavailable("protection-turbine-trip", OperatorComputerCommandGroup.Protection, "TURBINE TRIP", ControlRoomCommandKind.TurbineTrip, "SHELL ONLY", "No integrated runtime is attached."));
            commands.Add(Unavailable("protection-generator-trip", OperatorComputerCommandGroup.Protection, "GENERATOR TRIP", ControlRoomCommandKind.GeneratorTrip, "SHELL ONLY", "No integrated runtime is attached."));
            return;
        }

        commands.Add(snapshot.ReactorScramActive
            ? Blocked("protection-scram", OperatorComputerCommandGroup.Protection, "REACTOR SCRAM", ControlRoomCommandKind.ReactorScram, "SCRAM ACTIVE", "Reactor SCRAM is already latched.")
            : Available("protection-scram", OperatorComputerCommandGroup.Protection, "REACTOR SCRAM", ControlRoomCommandKind.ReactorScram, "SCRAM CLEAR"));
        commands.Add(!snapshot.AnyTripActive
            ? Blocked("protection-reset", OperatorComputerCommandGroup.Protection, "PROTECTION RESET", ControlRoomCommandKind.ProtectionReset, "PROTECTION CLEAR", "No latched trip is active.")
            : snapshot.ProtectionReset.CanResetNow
                ? Available("protection-reset", OperatorComputerCommandGroup.Protection, "PROTECTION RESET", ControlRoomCommandKind.ProtectionReset, "RESET AVAILABLE · runtime revalidates canonical M5.5 conditions")
                : Blocked("protection-reset", OperatorComputerCommandGroup.Protection, "PROTECTION RESET", ControlRoomCommandKind.ProtectionReset, "RESET BLOCKED", snapshot.ProtectionReset.StatusText));
        commands.Add(snapshot.TurbineTripActive
            ? Blocked("protection-turbine-trip", OperatorComputerCommandGroup.Protection, "TURBINE TRIP", ControlRoomCommandKind.TurbineTrip, "TURBINE TRIP ACTIVE", "Turbine trip is already latched.")
            : Available("protection-turbine-trip", OperatorComputerCommandGroup.Protection, "TURBINE TRIP", ControlRoomCommandKind.TurbineTrip, "TURBINE TRIP CLEAR"));
        commands.Add(snapshot.GeneratorTripActive
            ? Blocked("protection-generator-trip", OperatorComputerCommandGroup.Protection, "GENERATOR TRIP", ControlRoomCommandKind.GeneratorTrip, "GENERATOR TRIP ACTIVE", "Generator trip is already latched.")
            : Available("protection-generator-trip", OperatorComputerCommandGroup.Protection, "GENERATOR TRIP", ControlRoomCommandKind.GeneratorTrip, "GENERATOR TRIP CLEAR"));
    }

    private static void AddReactorCommands(List<OperatorComputerCommandSnapshot> commands, ControlRoomSnapshot snapshot)
    {
        foreach (var target in snapshot.ReactorCore.RodTargets)
        {
            var prefix = $"reactor-{Normalize(target.TargetId)}";
            var targetState = target.Label;
            commands.Add(Available(prefix + "-insert", OperatorComputerCommandGroup.Reactor, "INSERT RODS", new ControlRoomCommand(ControlRoomCommandKind.ControlRodInsert, target.TargetId, target.TargetKind), targetState));
            commands.Add(Available(prefix + "-hold", OperatorComputerCommandGroup.Reactor, "HOLD RODS", new ControlRoomCommand(ControlRoomCommandKind.ControlRodHold, target.TargetId, target.TargetKind), targetState));
            commands.Add(snapshot.ReactorCore.RodWithdrawalInhibited
                ? Blocked(prefix + "-withdraw", OperatorComputerCommandGroup.Reactor, "WITHDRAW RODS", new ControlRoomCommand(ControlRoomCommandKind.ControlRodWithdraw, target.TargetId, target.TargetKind), targetState, "Rod withdrawal is inhibited by the canonical protection/interlock state.")
                : Available(prefix + "-withdraw", OperatorComputerCommandGroup.Reactor, "WITHDRAW RODS", new ControlRoomCommand(ControlRoomCommandKind.ControlRodWithdraw, target.TargetId, target.TargetKind), targetState));
        }
    }

    private static void AddPrimaryCommands(List<OperatorComputerCommandSnapshot> commands, ControlRoomSnapshot snapshot)
    {
        foreach (var pump in snapshot.PrimaryCircuit.Pumps.Where(static pump => pump.IsOperatorCommandable))
        {
            var prefix = $"primary-{Normalize(pump.PumpId)}";
            var commandTarget = new ControlRoomCommand(ControlRoomCommandKind.MainCirculationPumpStart, pump.PumpId, ControlRoomCommandTargetKind.Pump);
            commands.Add(pump.IsRunning
                ? Blocked(prefix + "-start", OperatorComputerCommandGroup.Primary, "START MCP", commandTarget, "RUNNING", "Pump is already running.")
                : Available(prefix + "-start", OperatorComputerCommandGroup.Primary, "START MCP", commandTarget, "STOPPED"));

            commandTarget = new ControlRoomCommand(ControlRoomCommandKind.MainCirculationPumpStop, pump.PumpId, ControlRoomCommandTargetKind.Pump);
            commands.Add(pump.IsRunning
                ? Available(prefix + "-stop", OperatorComputerCommandGroup.Primary, "STOP MCP", commandTarget, "RUNNING")
                : Blocked(prefix + "-stop", OperatorComputerCommandGroup.Primary, "STOP MCP", commandTarget, "STOPPED", "Pump is already stopped."));
        }
    }

    private static void AddTurbineElectricalCommands(List<OperatorComputerCommandSnapshot> commands, ControlRoomSnapshot snapshot)
    {
        var emittedRotorIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var generator in snapshot.Electrical.Generators)
        {
            var generatorPrefix = $"electrical-{Normalize(generator.GeneratorId)}";
            var rotorPrefix = $"turbine-{Normalize(generator.RotorId)}";
            var normalControlBlocked = snapshot.TurbineTripActive || snapshot.GeneratorTripActive;
            var normalControlReason = snapshot.TurbineTripActive
                ? "Turbine trip is active."
                : snapshot.GeneratorTripActive
                    ? "Generator trip is active."
                    : null;

            if (emittedRotorIds.Add(generator.RotorId))
            {
                AddNormalControlCommand(
                    commands,
                    rotorPrefix + "-speed-raise",
                    OperatorComputerCommandGroup.Turbine,
                    "RAISE TURBINE SPEED",
                    new ControlRoomCommand(ControlRoomCommandKind.TurbineSpeedRaise, generator.RotorId, ControlRoomCommandTargetKind.TurbineRotor),
                    normalControlBlocked,
                    normalControlReason,
                    "ROTOR " + generator.RotorId);
                AddNormalControlCommand(
                    commands,
                    rotorPrefix + "-speed-lower",
                    OperatorComputerCommandGroup.Turbine,
                    "LOWER TURBINE SPEED",
                    new ControlRoomCommand(ControlRoomCommandKind.TurbineSpeedLower, generator.RotorId, ControlRoomCommandTargetKind.TurbineRotor),
                    normalControlBlocked,
                    normalControlReason,
                    "ROTOR " + generator.RotorId);
            }

            var loadBlocked = normalControlBlocked || !generator.BreakerClosed;
            var loadReason = normalControlReason ?? (!generator.BreakerClosed ? "Generator breaker is open; electrical load control is unavailable." : null);
            AddNormalControlCommand(
                commands,
                generatorPrefix + "-load-raise",
                OperatorComputerCommandGroup.Electrical,
                "RAISE GENERATOR LOAD",
                new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, generator.GeneratorId, ControlRoomCommandTargetKind.Generator),
                loadBlocked,
                loadReason,
                generator.BreakerText);
            AddNormalControlCommand(
                commands,
                generatorPrefix + "-load-lower",
                OperatorComputerCommandGroup.Electrical,
                "LOWER GENERATOR LOAD",
                new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadLower, generator.GeneratorId, ControlRoomCommandTargetKind.Generator),
                loadBlocked,
                loadReason,
                generator.BreakerText);

            var closeCommand = new ControlRoomCommand(ControlRoomCommandKind.GeneratorBreakerClose, generator.BreakerId, ControlRoomCommandTargetKind.Breaker);
            if (snapshot.GeneratorTripActive)
            {
                commands.Add(Blocked(generatorPrefix + "-breaker-close", OperatorComputerCommandGroup.Electrical, "CLOSE GENERATOR BREAKER", closeCommand, generator.BreakerText, "Generator trip is active."));
            }
            else if (generator.BreakerClosed)
            {
                commands.Add(Blocked(generatorPrefix + "-breaker-close", OperatorComputerCommandGroup.Electrical, "CLOSE GENERATOR BREAKER", closeCommand, generator.BreakerText, "Breaker is already closed."));
            }
            else if (!generator.SynchronizationConditionsSatisfied)
            {
                commands.Add(Blocked(generatorPrefix + "-breaker-close", OperatorComputerCommandGroup.Electrical, "CLOSE GENERATOR BREAKER", closeCommand, generator.DisplaySynchronizationText, "Synchronization permissive is not satisfied."));
            }
            else
            {
                commands.Add(Available(generatorPrefix + "-breaker-close", OperatorComputerCommandGroup.Electrical, "CLOSE GENERATOR BREAKER", closeCommand, generator.DisplaySynchronizationText));
            }

            var openCommand = new ControlRoomCommand(ControlRoomCommandKind.GeneratorBreakerOpen, generator.BreakerId, ControlRoomCommandTargetKind.Breaker);
            commands.Add(generator.BreakerClosed
                ? Available(generatorPrefix + "-breaker-open", OperatorComputerCommandGroup.Electrical, "OPEN GENERATOR BREAKER", openCommand, generator.BreakerText)
                : Blocked(generatorPrefix + "-breaker-open", OperatorComputerCommandGroup.Electrical, "OPEN GENERATOR BREAKER", openCommand, generator.BreakerText, "Breaker is already open."));
        }
    }

    private static void AddAlarmCommands(List<OperatorComputerCommandSnapshot> commands, ControlRoomSnapshot snapshot)
    {
        foreach (var alarm in snapshot.AlarmEvents.Alarms)
        {
            var prefix = $"alarm-{Normalize(alarm.AlarmId)}";
            var acknowledge = new ControlRoomCommand(ControlRoomCommandKind.AlarmAcknowledge, alarm.AlarmId, ControlRoomCommandTargetKind.Alarm);
            commands.Add(alarm.CanAcknowledge
                ? Available(prefix + "-ack", OperatorComputerCommandGroup.Alarms, "ACKNOWLEDGE ALARM", acknowledge, alarm.AnnunciatorText)
                : Blocked(prefix + "-ack", OperatorComputerCommandGroup.Alarms, "ACKNOWLEDGE ALARM", acknowledge, alarm.AnnunciatorText, "Alarm is not currently acknowledgeable."));

            var reset = new ControlRoomCommand(ControlRoomCommandKind.AlarmReset, alarm.AlarmId, ControlRoomCommandTargetKind.Alarm);
            commands.Add(alarm.CanReset
                ? Available(prefix + "-reset", OperatorComputerCommandGroup.Alarms, "RESET ALARM", reset, alarm.AnnunciatorText)
                : Blocked(prefix + "-reset", OperatorComputerCommandGroup.Alarms, "RESET ALARM", reset, alarm.AnnunciatorText, "Alarm reset conditions are not satisfied."));
        }

        var acknowledgeAll = new ControlRoomCommand(ControlRoomCommandKind.AlarmAcknowledgeAll);
        commands.Add(snapshot.AlarmEvents.Alarms.Any(static alarm => alarm.CanAcknowledge)
            ? Available("alarms-ack-all", OperatorComputerCommandGroup.Alarms, "ACKNOWLEDGE ALL ALARMS", acknowledgeAll, $"{snapshot.UnacknowledgedAlarmCount} UNACKNOWLEDGED")
            : Blocked("alarms-ack-all", OperatorComputerCommandGroup.Alarms, "ACKNOWLEDGE ALL ALARMS", acknowledgeAll, $"{snapshot.UnacknowledgedAlarmCount} UNACKNOWLEDGED", "No annunciated alarm is currently acknowledgeable."));

        var resetAll = new ControlRoomCommand(ControlRoomCommandKind.AlarmResetAll);
        var resettableCount = snapshot.AlarmEvents.Alarms.Count(static alarm => alarm.CanReset);
        commands.Add(resettableCount > 0
            ? Available("alarms-reset-all", OperatorComputerCommandGroup.Alarms, "RESET ALL ELIGIBLE ALARMS", resetAll, $"{resettableCount} RESETTABLE")
            : Blocked("alarms-reset-all", OperatorComputerCommandGroup.Alarms, "RESET ALL ELIGIBLE ALARMS", resetAll, "0 RESETTABLE", "No alarm currently satisfies canonical reset conditions."));
    }

    private static void AddNormalControlCommand(
        List<OperatorComputerCommandSnapshot> commands,
        string entryId,
        OperatorComputerCommandGroup group,
        string displayName,
        ControlRoomCommand command,
        bool blocked,
        string? blockReason,
        string currentState)
    {
        commands.Add(blocked
            ? Blocked(entryId, group, displayName, command, currentState, blockReason ?? "Normal control is unavailable.")
            : Available(entryId, group, displayName, command, currentState));
    }

    private static OperatorComputerCommandSnapshot Available(
        string entryId,
        OperatorComputerCommandGroup group,
        string displayName,
        ControlRoomCommandKind kind,
        string currentState)
        => Available(entryId, group, displayName, new ControlRoomCommand(kind), currentState);

    private static OperatorComputerCommandSnapshot Available(
        string entryId,
        OperatorComputerCommandGroup group,
        string displayName,
        ControlRoomCommand command,
        string currentState)
        => new(entryId, group, displayName, command, OperatorComputerCommandAvailability.Available, currentState);

    private static OperatorComputerCommandSnapshot Blocked(
        string entryId,
        OperatorComputerCommandGroup group,
        string displayName,
        ControlRoomCommandKind kind,
        string currentState,
        string reason)
        => Blocked(entryId, group, displayName, new ControlRoomCommand(kind), currentState, reason);

    private static OperatorComputerCommandSnapshot Blocked(
        string entryId,
        OperatorComputerCommandGroup group,
        string displayName,
        ControlRoomCommand command,
        string currentState,
        string reason)
        => new(entryId, group, displayName, command, OperatorComputerCommandAvailability.Blocked, currentState, reason);

    private static OperatorComputerCommandSnapshot Unavailable(
        string entryId,
        OperatorComputerCommandGroup group,
        string displayName,
        ControlRoomCommandKind kind,
        string currentState,
        string reason)
        => new(entryId, group, displayName, new ControlRoomCommand(kind), OperatorComputerCommandAvailability.Unavailable, currentState, reason);

    private static string Normalize(string value)
        => value.Replace(' ', '-').Replace('/', '-').Replace('\\', '-').ToLowerInvariant();
}
