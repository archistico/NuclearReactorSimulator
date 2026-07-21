namespace NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;

public interface ISecondaryTransientFaultTarget
{
    void ActivateTurbineTrip(string faultId, string turbineRotorId);
    void ActivateGeneratorTrip(string faultId, string generatorId);
    void ActivateCondenserCoolingDegradation(string faultId, string coolingBoundaryId, double capacityFraction);
    void ActivateCondenserCoolingLoss(string faultId, string coolingBoundaryId);
    void ClearSecondaryTransientFault(string faultId);
}
