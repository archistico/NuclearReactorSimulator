using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// Raw M4.6 first-law reconciliation across thermofluid inventories, turbine-rotor kinetic energy and shaft-to-grid conversion.
/// Internal turbine shaft work is cancelled exactly once when the complete reactor-to-grid path is evaluated.
/// No residual is corrected or hidden.
/// </summary>
public sealed record SecondaryCycleHeatBalanceAudit(
    Power NuclearHeatInputPower,
    Power PrimaryBoundaryNetExternalPower,
    Power GenericPlantHeatSourcePower,
    Power PumpHydraulicPowerExchange,
    Power FeedwaterConditioningPower,
    Power CondenserHeatRejectionPower,
    Power TurbineShaftPower,
    Power GeneratorMechanicalInputPower,
    Power ElectricalExportPower,
    Power GeneratorConversionLossPower,
    Power ThermofluidExpectedExternalPower,
    Power NetReactorToGridExternalPower,
    Energy InitialThermofluidStoredEnergy,
    Energy FinalThermofluidStoredEnergy,
    Energy InitialRotorKineticEnergy,
    Energy FinalRotorKineticEnergy,
    Energy CoupledStoredEnergyChange,
    double SupplementalPowerClassificationResidualWatts,
    double ShaftTransferPowerResidualWatts,
    double MechanicalToElectricalPowerResidualWatts,
    double CoupledDomainEnergyClosureResidualJoules,
    double FullEnergyPathClosureResidualJoules,
    MassFlowRate ExternalMassFlowRate,
    double MassClosureResidualKilograms)
{
    public bool IsFullEnergyPathClosedWithin(double toleranceJoules)
    {
        if (!double.IsFinite(toleranceJoules) || toleranceJoules < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(toleranceJoules), toleranceJoules, "Tolerance must be finite and non-negative.");
        }

        return Math.Abs(FullEnergyPathClosureResidualJoules) <= toleranceJoules;
    }
}
