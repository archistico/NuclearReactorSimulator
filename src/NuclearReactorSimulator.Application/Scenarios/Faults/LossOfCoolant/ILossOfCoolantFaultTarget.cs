using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.LossOfCoolant;

public interface ILossOfCoolantFaultTarget
{
    void ActivatePressureDrivenBreak(
        string faultId,
        string fluidNodeId,
        MassFlowRate referenceMassFlowRate,
        Pressure ambientPressure,
        PressureDifference referencePressureDifference,
        double maximumInventoryFractionPerStep);

    void ClearLossOfCoolantFault(string faultId);
}
