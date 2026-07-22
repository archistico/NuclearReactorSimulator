namespace NuclearReactorSimulator.Application.Scenarios.Faults.ElectricalLoss;

/// <summary>
/// M8.6 runtime seam for loss of the modeled external electrical-supply connection. The fault constrains only the
/// canonical M4.5 generator/grid connection; pump/control consequences are declared separately through validated M8.2/M8.3 faults.
/// </summary>
public interface IElectricalLossFaultTarget
{
    void ActivateExternalSupplyLoss(string faultId, string gridId);

    void ClearElectricalLossFault(string faultId);
}
