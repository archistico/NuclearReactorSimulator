using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Analysis;

/// <summary>
/// One synchronized M9.2 logical-step trend sample built exclusively from values already promoted into ControlRoomSnapshot.
/// Nullable numeric values preserve presentation-layer unavailability rather than inventing data.
/// </summary>
public sealed record PostIncidentTrendSample
{
    public PostIncidentTrendSample(ControlRoomSnapshot snapshot, long anchorLogicalStep)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (anchorLogicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(anchorLogicalStep));
        }

        LogicalStep = snapshot.LogicalStep;
        RelativeLogicalStep = snapshot.LogicalStep - anchorLogicalStep;
        ReactorThermalPower = snapshot.ReactorCore.ReactorThermalPower.NumericValue;
        TotalPrimaryMass = snapshot.PrimaryCircuit.TotalPrimaryMass.NumericValue;
        TotalFeedwaterFlow = snapshot.PrimaryCircuit.TotalFeedwaterFlow.NumericValue;
        TotalSteamExportFlow = snapshot.PrimaryCircuit.TotalSteamExportFlow.NumericValue;
        TotalSteamFlow = snapshot.TurbineSecondary.TotalSteamFlow.NumericValue;
        TotalTurbineShaftPower = snapshot.TurbineSecondary.TotalTurbineShaftPower.NumericValue;
        TotalCondenserHeatRejection = snapshot.TurbineSecondary.TotalCondenserHeatRejection.NumericValue;
        GrossElectricalOutput = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        InvalidMeasuredSignalCount = snapshot.InvalidMeasuredSignalCount;
        AnnunciatedAlarmCount = snapshot.AnnunciatedAlarmCount;
        UnacknowledgedAlarmCount = snapshot.UnacknowledgedAlarmCount;
        ActiveFaultCount = snapshot.Faults.ActiveCount;
        ReactorScramActive = snapshot.ReactorScramActive;
        TurbineTripActive = snapshot.TurbineTripActive;
        GeneratorTripActive = snapshot.GeneratorTripActive;
    }

    private PostIncidentTrendSample(
        long logicalStep,
        long relativeLogicalStep,
        double? reactorThermalPower,
        double? totalPrimaryMass,
        double? totalFeedwaterFlow,
        double? totalSteamExportFlow,
        double? totalSteamFlow,
        double? totalTurbineShaftPower,
        double? totalCondenserHeatRejection,
        double? grossElectricalOutput,
        int invalidMeasuredSignalCount,
        int annunciatedAlarmCount,
        int unacknowledgedAlarmCount,
        int activeFaultCount,
        bool reactorScramActive,
        bool turbineTripActive,
        bool generatorTripActive)
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

        ValidateFinite(reactorThermalPower, nameof(reactorThermalPower));
        ValidateFinite(totalPrimaryMass, nameof(totalPrimaryMass));
        ValidateFinite(totalFeedwaterFlow, nameof(totalFeedwaterFlow));
        ValidateFinite(totalSteamExportFlow, nameof(totalSteamExportFlow));
        ValidateFinite(totalSteamFlow, nameof(totalSteamFlow));
        ValidateFinite(totalTurbineShaftPower, nameof(totalTurbineShaftPower));
        ValidateFinite(totalCondenserHeatRejection, nameof(totalCondenserHeatRejection));
        ValidateFinite(grossElectricalOutput, nameof(grossElectricalOutput));

        LogicalStep = logicalStep;
        RelativeLogicalStep = relativeLogicalStep;
        ReactorThermalPower = reactorThermalPower;
        TotalPrimaryMass = totalPrimaryMass;
        TotalFeedwaterFlow = totalFeedwaterFlow;
        TotalSteamExportFlow = totalSteamExportFlow;
        TotalSteamFlow = totalSteamFlow;
        TotalTurbineShaftPower = totalTurbineShaftPower;
        TotalCondenserHeatRejection = totalCondenserHeatRejection;
        GrossElectricalOutput = grossElectricalOutput;
        InvalidMeasuredSignalCount = invalidMeasuredSignalCount;
        AnnunciatedAlarmCount = annunciatedAlarmCount;
        UnacknowledgedAlarmCount = unacknowledgedAlarmCount;
        ActiveFaultCount = activeFaultCount;
        ReactorScramActive = reactorScramActive;
        TurbineTripActive = turbineTripActive;
        GeneratorTripActive = generatorTripActive;
    }

    public static PostIncidentTrendSample FromValues(
        long logicalStep,
        long relativeLogicalStep,
        double? reactorThermalPower,
        double? totalPrimaryMass,
        double? totalFeedwaterFlow,
        double? totalSteamExportFlow,
        double? totalSteamFlow,
        double? totalTurbineShaftPower,
        double? totalCondenserHeatRejection,
        double? grossElectricalOutput,
        int invalidMeasuredSignalCount,
        int annunciatedAlarmCount,
        int unacknowledgedAlarmCount,
        int activeFaultCount,
        bool reactorScramActive,
        bool turbineTripActive,
        bool generatorTripActive)
        => new(
            logicalStep,
            relativeLogicalStep,
            reactorThermalPower,
            totalPrimaryMass,
            totalFeedwaterFlow,
            totalSteamExportFlow,
            totalSteamFlow,
            totalTurbineShaftPower,
            totalCondenserHeatRejection,
            grossElectricalOutput,
            invalidMeasuredSignalCount,
            annunciatedAlarmCount,
            unacknowledgedAlarmCount,
            activeFaultCount,
            reactorScramActive,
            turbineTripActive,
            generatorTripActive);

    public long LogicalStep { get; }
    public long RelativeLogicalStep { get; }
    public double? ReactorThermalPower { get; }
    public double? TotalPrimaryMass { get; }
    public double? TotalFeedwaterFlow { get; }
    public double? TotalSteamExportFlow { get; }
    public double? TotalSteamFlow { get; }
    public double? TotalTurbineShaftPower { get; }
    public double? TotalCondenserHeatRejection { get; }
    public double? GrossElectricalOutput { get; }
    public int InvalidMeasuredSignalCount { get; }
    public int AnnunciatedAlarmCount { get; }
    public int UnacknowledgedAlarmCount { get; }
    public int ActiveFaultCount { get; }
    public bool ReactorScramActive { get; }
    public bool TurbineTripActive { get; }
    public bool GeneratorTripActive { get; }

    private static void ValidateFinite(double? value, string parameterName)
    {
        if (value.HasValue && !double.IsFinite(value.Value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
