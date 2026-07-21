namespace NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;

public static class HydraulicFaultTypeIds
{
    public const string PumpTrip = "hydraulic.pump-trip";
    public const string PumpDegradation = "hydraulic.pump-degradation";
    public const string ValveFailOpen = "hydraulic.valve-fail-open";
    public const string ValveFailClosed = "hydraulic.valve-fail-closed";
    public const string ValveStuck = "hydraulic.valve-stuck";
    public const string PathRestriction = "hydraulic.path-restriction";
    public const string PathBlockage = "hydraulic.path-blockage";
    public const string NodeLeak = "hydraulic.node-leak";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        PumpTrip, PumpDegradation, ValveFailOpen, ValveFailClosed, ValveStuck, PathRestriction, PathBlockage, NodeLeak,
    };
}
