using System.Text.Json.Serialization;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// UI-safe presentation snapshot. It intentionally exposes no FullPlantSnapshot or authoritative physical state.
/// </summary>
public sealed record ControlRoomSnapshot
{
    public ControlRoomSnapshot(
        long logicalStep,
        ControlRoomRunState runState,
        int totalMeasuredSignalCount,
        int invalidMeasuredSignalCount,
        int annunciatedAlarmCount,
        int unacknowledgedAlarmCount,
        bool reactorScramActive,
        bool turbineTripActive,
        bool generatorTripActive,
        ReactorCorePanelSnapshot? reactorCore = null,
        PrimaryCircuitPanelSnapshot? primaryCircuit = null,
        TurbineSecondaryPanelSnapshot? turbineSecondary = null,
        ElectricalPanelSnapshot? electrical = null,
        AlarmEventsPanelSnapshot? alarmEvents = null,
        ControlRoomFaultStateSnapshot? faults = null,
        ProtectionResetPresentationSnapshot? protectionReset = null)
    {
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }

        if (totalMeasuredSignalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalMeasuredSignalCount));
        }

        if (invalidMeasuredSignalCount < 0 || invalidMeasuredSignalCount > totalMeasuredSignalCount)
        {
            throw new ArgumentOutOfRangeException(nameof(invalidMeasuredSignalCount));
        }

        if (annunciatedAlarmCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(annunciatedAlarmCount));
        }

        if (unacknowledgedAlarmCount < 0 || unacknowledgedAlarmCount > annunciatedAlarmCount)
        {
            throw new ArgumentOutOfRangeException(nameof(unacknowledgedAlarmCount));
        }

        LogicalStep = logicalStep;
        RunState = runState;
        TotalMeasuredSignalCount = totalMeasuredSignalCount;
        InvalidMeasuredSignalCount = invalidMeasuredSignalCount;
        AnnunciatedAlarmCount = annunciatedAlarmCount;
        UnacknowledgedAlarmCount = unacknowledgedAlarmCount;
        ReactorScramActive = reactorScramActive;
        TurbineTripActive = turbineTripActive;
        GeneratorTripActive = generatorTripActive;
        ReactorCore = reactorCore ?? ReactorCorePanelSnapshot.Unavailable;
        PrimaryCircuit = primaryCircuit ?? PrimaryCircuitPanelSnapshot.Unavailable;
        TurbineSecondary = turbineSecondary ?? TurbineSecondaryPanelSnapshot.Unavailable;
        Electrical = electrical ?? ElectricalPanelSnapshot.Unavailable;
        AlarmEvents = alarmEvents ?? AlarmEventsPanelSnapshot.Unavailable;
        Faults = faults ?? ControlRoomFaultStateSnapshot.Empty;
        ProtectionReset = protectionReset ?? ProtectionResetPresentationSnapshot.Unavailable;
    }

    public static ControlRoomSnapshot ShellOnly { get; } = new(
        0,
        ControlRoomRunState.ShellOnly,
        0,
        0,
        0,
        0,
        false,
        false,
        false,
        ReactorCorePanelSnapshot.Unavailable,
        PrimaryCircuitPanelSnapshot.Unavailable,
        TurbineSecondaryPanelSnapshot.Unavailable,
        ElectricalPanelSnapshot.Unavailable,
        AlarmEventsPanelSnapshot.Unavailable);

    public long LogicalStep { get; }

    public ControlRoomRunState RunState { get; }

    public int TotalMeasuredSignalCount { get; }

    public int InvalidMeasuredSignalCount { get; }

    public int AnnunciatedAlarmCount { get; }

    public int UnacknowledgedAlarmCount { get; }

    public bool ReactorScramActive { get; }

    public bool TurbineTripActive { get; }

    public bool GeneratorTripActive { get; }

    public ReactorCorePanelSnapshot ReactorCore { get; }

    public PrimaryCircuitPanelSnapshot PrimaryCircuit { get; }

    public TurbineSecondaryPanelSnapshot TurbineSecondary { get; }

    public ElectricalPanelSnapshot Electrical { get; }

    public AlarmEventsPanelSnapshot AlarmEvents { get; }

    public ControlRoomFaultStateSnapshot Faults { get; }

    [JsonIgnore]
    public ProtectionResetPresentationSnapshot ProtectionReset { get; }

    public ControlRoomSnapshot WithFaultState(ControlRoomFaultStateSnapshot faults)
    {
        ArgumentNullException.ThrowIfNull(faults);
        return new ControlRoomSnapshot(
            LogicalStep,
            RunState,
            TotalMeasuredSignalCount,
            InvalidMeasuredSignalCount,
            AnnunciatedAlarmCount,
            UnacknowledgedAlarmCount,
            ReactorScramActive,
            TurbineTripActive,
            GeneratorTripActive,
            ReactorCore,
            PrimaryCircuit,
            TurbineSecondary,
            Electrical,
            AlarmEvents,
            faults,
            ProtectionReset);
    }

    public int ValidMeasuredSignalCount => TotalMeasuredSignalCount - InvalidMeasuredSignalCount;

    public bool AnyTripActive => ReactorScramActive || TurbineTripActive || GeneratorTripActive;
}
