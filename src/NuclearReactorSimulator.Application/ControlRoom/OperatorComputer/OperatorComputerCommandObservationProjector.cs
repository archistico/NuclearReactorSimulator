using System.Collections.ObjectModel;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public enum OperatorComputerCommandObservationValueKind
{
    Numeric = 0,
    Boolean = 1,
    Text = 2,
    Unavailable = 3,
}

public sealed record OperatorComputerCommandObservationSample(
    OperatorComputerCommandConsequenceReference Target,
    OperatorComputerInformationProvenance Provenance,
    string Reason,
    OperatorComputerCommandObservationValueKind ValueKind,
    string ValueText,
    string Unit,
    double? NumericValue,
    bool? BooleanValue,
    bool IsAvailable);

/// <summary>
/// M10.9.5.4 deterministic projection of the already-authored command monitor set onto the current UI-safe
/// <see cref="ControlRoomSnapshot"/>. This projector reads published presentation state only; it does not infer causality,
/// success or a future plant state.
/// </summary>
public static class OperatorComputerCommandObservationProjector
{
    public static IReadOnlyList<OperatorComputerCommandObservationSample> Project(
        ControlRoomSnapshot snapshot,
        ControlRoomCommand command)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(command);

        var consequence = OperatorComputerCommandConsequenceCatalog.Project(command);
        if (!consequence.HasAuthoredMap)
        {
            return Array.Empty<OperatorComputerCommandObservationSample>();
        }

        return new ReadOnlyCollection<OperatorComputerCommandObservationSample>(
            consequence.MonitorTargets.Select(monitor => ProjectMonitor(snapshot, command, monitor)).ToArray());
    }

    private static OperatorComputerCommandObservationSample ProjectMonitor(
        ControlRoomSnapshot snapshot,
        ControlRoomCommand command,
        OperatorComputerCommandMonitorTarget monitor)
        => monitor.Target.Id switch
        {
            nameof(ControlRoomSnapshot.RunState) => Text(monitor, snapshot.RunState.ToString().ToUpperInvariant()),
            nameof(ControlRoomSnapshot.LogicalStep) => Numeric(monitor, snapshot.LogicalStep.ToString(System.Globalization.CultureInfo.InvariantCulture), string.Empty, snapshot.LogicalStep),
            nameof(ControlRoomSnapshot.ReactorScramActive) => Boolean(monitor, snapshot.ReactorScramActive, "ACTIVE", "CLEAR"),
            nameof(ControlRoomSnapshot.TurbineTripActive) => Boolean(monitor, snapshot.TurbineTripActive, "ACTIVE", "CLEAR"),
            nameof(ControlRoomSnapshot.GeneratorTripActive) => Boolean(monitor, snapshot.GeneratorTripActive, "ACTIVE", "CLEAR"),
            nameof(ControlRoomSnapshot.UnacknowledgedAlarmCount) => Numeric(monitor, snapshot.UnacknowledgedAlarmCount.ToString(System.Globalization.CultureInfo.InvariantCulture), string.Empty, snapshot.UnacknowledgedAlarmCount),
            "ReactorCore.ReactorThermalPower" => Value(monitor, snapshot.ReactorCore.ReactorThermalPower),
            "ReactorCore.AverageRodWithdrawal" => Value(monitor, snapshot.ReactorCore.AverageRodWithdrawal),
            "PrimaryCircuit.TotalPrimaryMass" => Value(monitor, snapshot.PrimaryCircuit.TotalPrimaryMass),
            "PrimaryCircuit.Pumps.IsRunning" => PumpRunning(snapshot, command, monitor),
            "TurbineSecondary.Rotors.Speed" => RotorSpeed(snapshot, command, monitor),
            "TurbineSecondary.TotalTurbineShaftPower" => Value(monitor, snapshot.TurbineSecondary.TotalTurbineShaftPower),
            "TurbineSecondary.EffectiveTurbineSteamFlow" => Value(monitor, snapshot.TurbineSecondary.EffectiveTurbineSteamFlow),
            "TurbineSecondary.AdmissionTrains.ControlValvePosition" => AdmissionTrainValue(snapshot, command, monitor, static train => train.ControlValvePosition),
            "TurbineSecondary.AdmissionTrains.ControlValveManualDemand" => AdmissionTrainValue(snapshot, command, monitor, static train => train.ControlValveManualDemand),
            "TurbineSecondary.AdmissionTrains.ControlValveManualMode" => AdmissionTrainManualMode(snapshot, command, monitor),
            "Electrical.GrossElectricalOutput" => Value(monitor, snapshot.Electrical.GrossElectricalOutput),
            "Electrical.Generators.BreakerClosed" => GeneratorBreaker(snapshot, command, monitor),
            "AlarmEvents.Alarms.IsAcknowledged" => AlarmBoolean(snapshot, command, monitor, static alarm => alarm.IsAcknowledged, "ACKNOWLEDGED", "UNACKNOWLEDGED"),
            "AlarmEvents.Alarms.IsAnnunciated" => AlarmBoolean(snapshot, command, monitor, static alarm => alarm.IsAnnunciated, "ANNUNCIATED", "CLEAR"),
            "AlarmEvents.Alarms" => AlarmSummary(snapshot, monitor),
            _ => Unavailable(monitor),
        };

    private static OperatorComputerCommandObservationSample PumpRunning(
        ControlRoomSnapshot snapshot,
        ControlRoomCommand command,
        OperatorComputerCommandMonitorTarget monitor)
    {
        var pump = snapshot.PrimaryCircuit.Pumps.FirstOrDefault(item =>
            string.Equals(item.PumpId, command.TargetId, StringComparison.Ordinal));
        return pump is null ? Unavailable(monitor) : Boolean(monitor, pump.IsRunning, "RUNNING", "STOPPED");
    }

    private static OperatorComputerCommandObservationSample RotorSpeed(
        ControlRoomSnapshot snapshot,
        ControlRoomCommand command,
        OperatorComputerCommandMonitorTarget monitor)
    {
        var rotorId = command.TargetKind == ControlRoomCommandTargetKind.TurbineRotor
            ? command.TargetId
            : snapshot.Electrical.Generators.FirstOrDefault(generator =>
                string.Equals(generator.GeneratorId, command.TargetId, StringComparison.Ordinal))?.RotorId;
        var rotor = snapshot.TurbineSecondary.Rotors.FirstOrDefault(item =>
            string.Equals(item.RotorId, rotorId, StringComparison.Ordinal));
        return rotor is null ? Unavailable(monitor) : Value(monitor, rotor.Speed);
    }

    private static OperatorComputerCommandObservationSample GeneratorBreaker(
        ControlRoomSnapshot snapshot,
        ControlRoomCommand command,
        OperatorComputerCommandMonitorTarget monitor)
    {
        var generator = snapshot.Electrical.Generators.FirstOrDefault(item =>
            string.Equals(item.BreakerId, command.TargetId, StringComparison.Ordinal)
            || string.Equals(item.GeneratorId, command.TargetId, StringComparison.Ordinal));
        return generator is null ? Unavailable(monitor) : Boolean(monitor, generator.BreakerClosed, "CLOSED", "OPEN");
    }

    private static OperatorComputerCommandObservationSample AdmissionTrainValue(
        ControlRoomSnapshot snapshot,
        ControlRoomCommand command,
        OperatorComputerCommandMonitorTarget monitor,
        Func<TurbineAdmissionTrainPresentationSnapshot, ControlRoomValueSnapshot> selector)
    {
        var train = FindAdmissionTrain(snapshot, command.TargetId);
        return train is null ? Unavailable(monitor) : Value(monitor, selector(train));
    }

    private static OperatorComputerCommandObservationSample AdmissionTrainManualMode(
        ControlRoomSnapshot snapshot,
        ControlRoomCommand command,
        OperatorComputerCommandMonitorTarget monitor)
    {
        var train = FindAdmissionTrain(snapshot, command.TargetId);
        return train is null ? Unavailable(monitor) : Boolean(monitor, train.ControlValveManualMode, "MANUAL", "AUTO / GOVERNOR");
    }

    private static TurbineAdmissionTrainPresentationSnapshot? FindAdmissionTrain(ControlRoomSnapshot snapshot, string? targetId)
        => snapshot.TurbineSecondary.AdmissionTrains.FirstOrDefault(train =>
            string.Equals(train.StopValveId, targetId, StringComparison.Ordinal)
            || string.Equals(train.ControlValveId, targetId, StringComparison.Ordinal)
            || string.Equals(train.AdmissionValveId, targetId, StringComparison.Ordinal));

    private static OperatorComputerCommandObservationSample AlarmBoolean(
        ControlRoomSnapshot snapshot,
        ControlRoomCommand command,
        OperatorComputerCommandMonitorTarget monitor,
        Func<ControlRoomAlarmPresentationSnapshot, bool> selector,
        string trueText,
        string falseText)
    {
        var alarm = snapshot.AlarmEvents.Alarms.FirstOrDefault(item =>
            string.Equals(item.AlarmId, command.TargetId, StringComparison.Ordinal));
        return alarm is null ? Unavailable(monitor) : Boolean(monitor, selector(alarm), trueText, falseText);
    }

    private static OperatorComputerCommandObservationSample AlarmSummary(
        ControlRoomSnapshot snapshot,
        OperatorComputerCommandMonitorTarget monitor)
        => Numeric(
            monitor,
            $"ANNUNCIATED {snapshot.AlarmEvents.AnnunciatedCount} · UNACK {snapshot.AlarmEvents.UnacknowledgedCount}",
            string.Empty,
            snapshot.AlarmEvents.AnnunciatedCount);

    private static OperatorComputerCommandObservationSample Value(
        OperatorComputerCommandMonitorTarget monitor,
        ControlRoomValueSnapshot value)
        => value.NumericValue is { } numeric
            ? Numeric(monitor, value.ValueText, value.Unit, numeric)
            : Unavailable(monitor, value.Unit);

    private static OperatorComputerCommandObservationSample Numeric(
        OperatorComputerCommandMonitorTarget monitor,
        string valueText,
        string unit,
        double numericValue)
        => new(monitor.Target, monitor.Provenance, monitor.Reason, OperatorComputerCommandObservationValueKind.Numeric,
            valueText, unit, numericValue, null, true);

    private static OperatorComputerCommandObservationSample Boolean(
        OperatorComputerCommandMonitorTarget monitor,
        bool value,
        string trueText,
        string falseText)
        => new(monitor.Target, monitor.Provenance, monitor.Reason, OperatorComputerCommandObservationValueKind.Boolean,
            value ? trueText : falseText, string.Empty, null, value, true);

    private static OperatorComputerCommandObservationSample Text(
        OperatorComputerCommandMonitorTarget monitor,
        string value)
        => new(monitor.Target, monitor.Provenance, monitor.Reason, OperatorComputerCommandObservationValueKind.Text,
            value, string.Empty, null, null, true);

    private static OperatorComputerCommandObservationSample Unavailable(
        OperatorComputerCommandMonitorTarget monitor,
        string unit = "")
        => new(monitor.Target, monitor.Provenance, monitor.Reason, OperatorComputerCommandObservationValueKind.Unavailable,
            "—", unit, null, null, false);
}
