namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record TurbineAdmissionTrainPresentationSnapshot(
    string TrainId,
    string HeaderNodeId,
    string TurbineInletNodeId,
    string StopValveId,
    ControlRoomValueSnapshot StopValvePosition,
    string ControlValveId,
    ControlRoomValueSnapshot ControlValvePosition,
    string AdmissionValveId,
    ControlRoomValueSnapshot AdmissionValvePosition,
    ControlRoomValueSnapshot AdmissionFlow,
    ControlRoomValueSnapshot TurbineInletPressure,
    ControlRoomValueSnapshot TurbineInletTemperature,
    string TurbineInletPhase)
{
    public ControlRoomValueSnapshot StopValveRequestedPosition { get; init; } = StopValvePosition;

    public ControlRoomValueSnapshot ControlValveRequestedPosition { get; init; } = ControlValvePosition;

    public ControlRoomValueSnapshot ControlValveManualDemand { get; init; } = ControlValvePosition;

    public ControlRoomValueSnapshot AdmissionValveRequestedPosition { get; init; } = AdmissionValvePosition;

    public bool ControlValveManualMode { get; init; }

    public bool TurbineAdmissionOpeningInhibited { get; init; }

    public bool StopValveForcedClosed { get; init; }

    public string EndpointText => $"{HeaderNodeId} → {TurbineInletNodeId}";

    public string StopValveText => $"STOP {StopValveId}: {StopValvePosition.ValueText} {StopValvePosition.Unit}".TrimEnd();

    public string ControlValveText => $"CONTROL {ControlValveId}: {ControlValvePosition.ValueText} {ControlValvePosition.Unit}".TrimEnd();

    public string AdmissionValveText => $"ADMISSION {AdmissionValveId}: {AdmissionValvePosition.ValueText} {AdmissionValvePosition.Unit}".TrimEnd();

    public string ControlValveModeText => ControlValveManualMode ? "MANUAL" : "AUTO / GOVERNOR";

    public string StopValveRequestedText => $"TARGET {StopValveRequestedPosition.ValueText} {StopValveRequestedPosition.Unit}".TrimEnd();

    public string ControlValveRequestedText => $"TARGET {ControlValveRequestedPosition.ValueText} {ControlValveRequestedPosition.Unit}".TrimEnd();

    public string AdmissionValveRequestedText => $"TARGET {AdmissionValveRequestedPosition.ValueText} {AdmissionValveRequestedPosition.Unit}".TrimEnd();

    public string ValveAuthorityText => StopValveForcedClosed
        ? "TRIP OVERRIDE · STOP FORCED CLOSED"
        : TurbineAdmissionOpeningInhibited
            ? "PROTECTION INHIBIT · OPENING BLOCKED"
            : "NORMAL OPERATOR / GOVERNOR AUTHORITY";
}
