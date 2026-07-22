using NuclearReactorSimulator.Application.ControlRoom;
namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public static class OperatorComputerInformationProjector
{
    public static OperatorComputerInformationSnapshot Project(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new OperatorComputerInformationSnapshot(new[]
        {
            new OperatorComputerInformationSectionSnapshot("REACTOR", "REACTOR", new[]
            {
                Item("THERMAL POWER", snapshot.ReactorCore.ReactorThermalPower, OperatorComputerInformationProvenance.Measured),
                Item("REACTOR PERIOD", snapshot.ReactorCore.ReactorPeriod, OperatorComputerInformationProvenance.ModelDiagnostic),
                Item("TOTAL REACTIVITY", snapshot.ReactorCore.TotalReactivity, OperatorComputerInformationProvenance.ModelDiagnostic),
                Item("ROD WITHDRAWAL", snapshot.ReactorCore.AverageRodWithdrawal, OperatorComputerInformationProvenance.ModelDiagnostic),
                Item("XENON REACTIVITY", snapshot.ReactorCore.XenonReactivity, OperatorComputerInformationProvenance.ModelDiagnostic),
            }),
            new OperatorComputerInformationSectionSnapshot("PRIMARY", "PRIMARY", new[]
            {
                Item("PRIMARY INVENTORY", snapshot.PrimaryCircuit.TotalPrimaryMass, OperatorComputerInformationProvenance.ModelDiagnostic),
                Item("FEEDWATER TOTAL", snapshot.PrimaryCircuit.TotalFeedwaterFlow, OperatorComputerInformationProvenance.ModelDiagnostic),
                Item("STEAM EXPORT TOTAL", snapshot.PrimaryCircuit.TotalSteamExportFlow, OperatorComputerInformationProvenance.ModelDiagnostic),
            }),
            new OperatorComputerInformationSectionSnapshot("TURBINE", "TURBINE / SECONDARY", new[]
            {
                Item("TURBINE SHAFT POWER", snapshot.TurbineSecondary.TotalTurbineShaftPower, OperatorComputerInformationProvenance.Measured),
                Item("STEAM FLOW", snapshot.TurbineSecondary.TotalSteamFlow, OperatorComputerInformationProvenance.ModelDiagnostic),
                Item("CONDENSER HEAT REJECTION", snapshot.TurbineSecondary.TotalCondenserHeatRejection, OperatorComputerInformationProvenance.Measured),
            }),
            new OperatorComputerInformationSectionSnapshot("ELECTRICAL", "ELECTRICAL", new[]
            {
                Item("GROSS OUTPUT", snapshot.Electrical.GrossElectricalOutput, OperatorComputerInformationProvenance.Measured),
                Item("GRID FREQUENCY", snapshot.Electrical.Grid.Frequency, OperatorComputerInformationProvenance.ModelDiagnostic),
                Item("GRID VOLTAGE", snapshot.Electrical.Grid.LineVoltage, OperatorComputerInformationProvenance.ModelDiagnostic),
                Item("GRID PHASE", snapshot.Electrical.Grid.PhaseAngle, OperatorComputerInformationProvenance.ModelDiagnostic),
            }),
            new OperatorComputerInformationSectionSnapshot("PROTECTION", "PROTECTION / SIGNAL HEALTH", new[]
            {
                StateItem("REACTOR SCRAM", snapshot.ReactorScramActive ? "ACTIVE" : "CLEAR"),
                StateItem("TURBINE TRIP", snapshot.TurbineTripActive ? "ACTIVE" : "CLEAR"),
                StateItem("GENERATOR TRIP", snapshot.GeneratorTripActive ? "ACTIVE" : "CLEAR"),
                StateItem("MEASURED SIGNALS", $"{snapshot.ValidMeasuredSignalCount}/{snapshot.TotalMeasuredSignalCount} VALID"),
            }),
        });
    }

    private static OperatorComputerInformationItemSnapshot Item(
        string label,
        ControlRoomValueSnapshot value,
        OperatorComputerInformationProvenance intendedProvenance)
    {
        var provenance = value.State == ControlRoomVisualState.Unavailable
            ? OperatorComputerInformationProvenance.Unavailable
            : intendedProvenance;
        return new OperatorComputerInformationItemSnapshot(label, value.ValueText, value.Unit, provenance);
    }

    private static OperatorComputerInformationItemSnapshot StateItem(string label, string value)
        => new(label, value, string.Empty, OperatorComputerInformationProvenance.CanonicalState);
}
