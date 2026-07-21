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
    public string EndpointText => $"{HeaderNodeId} → {TurbineInletNodeId}";

    public string StopValveText => $"STOP {StopValveId}: {StopValvePosition.ValueText} {StopValvePosition.Unit}".TrimEnd();

    public string ControlValveText => $"CONTROL {ControlValveId}: {ControlValvePosition.ValueText} {ControlValvePosition.Unit}".TrimEnd();

    public string AdmissionValveText => $"ADMISSION {AdmissionValveId}: {AdmissionValvePosition.ValueText} {AdmissionValvePosition.Unit}".TrimEnd();
}
