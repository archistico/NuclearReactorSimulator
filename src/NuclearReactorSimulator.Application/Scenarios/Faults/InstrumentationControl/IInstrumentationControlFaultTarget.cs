namespace NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;

public interface IInstrumentationControlFaultTarget
{
    void ActivateSensorBias(string faultId, string channelId, double biasEngineeringUnits);
    void ActivateSensorFreeze(string faultId, string channelId);
    void ActivateSensorFailedLow(string faultId, string channelId);
    void ActivateSensorFailedHigh(string faultId, string channelId);
    void ActivateSensorUnavailable(string faultId, string channelId);
    void ActivateControllerOutputFreeze(string faultId, string controllerId);
    void ActivateControllerOutputFailLow(string faultId, string controllerId);
    void ActivateControllerOutputFailHigh(string faultId, string controllerId);
    void ActivateActuatorCommandFreeze(string faultId, string actuatorId);
    void ActivateActuatorCommandFailLow(string faultId, string actuatorId);
    void ActivateActuatorCommandFailHigh(string faultId, string actuatorId);
    void ClearInstrumentationControlFault(string faultId);
}
