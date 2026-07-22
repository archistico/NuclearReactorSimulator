using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Analysis;

/// <summary>Compact observed control-room state at one recorded logical step.</summary>
public sealed record PostIncidentStateSummary
{
    public PostIncidentStateSummary(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        LogicalStep = snapshot.LogicalStep;
        InvalidMeasuredSignalCount = snapshot.InvalidMeasuredSignalCount;
        AnnunciatedAlarmCount = snapshot.AnnunciatedAlarmCount;
        UnacknowledgedAlarmCount = snapshot.UnacknowledgedAlarmCount;
        ReactorScramActive = snapshot.ReactorScramActive;
        TurbineTripActive = snapshot.TurbineTripActive;
        GeneratorTripActive = snapshot.GeneratorTripActive;
        ActiveFaultCount = snapshot.Faults.ActiveCount;
    }


    private PostIncidentStateSummary(
        long logicalStep,
        int invalidMeasuredSignalCount,
        int annunciatedAlarmCount,
        int unacknowledgedAlarmCount,
        bool reactorScramActive,
        bool turbineTripActive,
        bool generatorTripActive,
        int activeFaultCount)
    {
        if (logicalStep < 0
            || invalidMeasuredSignalCount < 0
            || annunciatedAlarmCount < 0
            || unacknowledgedAlarmCount < 0
            || activeFaultCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        if (unacknowledgedAlarmCount > annunciatedAlarmCount)
        {
            throw new ArgumentException("Unacknowledged alarm count cannot exceed annunciated alarm count.");
        }

        LogicalStep = logicalStep;
        InvalidMeasuredSignalCount = invalidMeasuredSignalCount;
        AnnunciatedAlarmCount = annunciatedAlarmCount;
        UnacknowledgedAlarmCount = unacknowledgedAlarmCount;
        ReactorScramActive = reactorScramActive;
        TurbineTripActive = turbineTripActive;
        GeneratorTripActive = generatorTripActive;
        ActiveFaultCount = activeFaultCount;
    }

    public static PostIncidentStateSummary FromValues(
        long logicalStep,
        int invalidMeasuredSignalCount,
        int annunciatedAlarmCount,
        int unacknowledgedAlarmCount,
        bool reactorScramActive,
        bool turbineTripActive,
        bool generatorTripActive,
        int activeFaultCount)
        => new(
            logicalStep,
            invalidMeasuredSignalCount,
            annunciatedAlarmCount,
            unacknowledgedAlarmCount,
            reactorScramActive,
            turbineTripActive,
            generatorTripActive,
            activeFaultCount);

    public long LogicalStep { get; }
    public int InvalidMeasuredSignalCount { get; }
    public int AnnunciatedAlarmCount { get; }
    public int UnacknowledgedAlarmCount { get; }
    public bool ReactorScramActive { get; }
    public bool TurbineTripActive { get; }
    public bool GeneratorTripActive { get; }
    public int ActiveFaultCount { get; }
}
