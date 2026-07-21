using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;

public interface IHydraulicComponentFaultTarget
{
    void ActivatePumpTrip(string faultId, string pumpId);
    void ActivatePumpDegradation(string faultId, string pumpId, double capacityFraction);
    void ActivateValveFailOpen(string faultId, string valveId);
    void ActivateValveFailClosed(string faultId, string valveId);
    void ActivateValveStuck(string faultId, string valveId);
    void ActivatePathRestriction(string faultId, string valveId, double maximumOpenFraction);
    void ActivateLeak(string faultId, string fluidNodeId, MassFlowRate massFlowRate);
    void ClearHydraulicFault(string faultId);
}
