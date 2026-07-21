using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Electrical;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// Deterministic M4.6 top-level secondary-cycle solver. It delegates physical evolution to the validated M4.5 stack,
/// then reconciles thermofluid, mechanical and electrical energy domains without introducing another integrator.
/// </summary>
public sealed class IntegratedSecondaryCycleSolver
{
    private readonly IntegratedSecondaryCycleDefinition _definition;
    private readonly GeneratorGridSolver _generatorGridSolver;

    public IntegratedSecondaryCycleSolver(
        IntegratedSecondaryCycleDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _generatorGridSolver = new GeneratorGridSolver(definition.GeneratorGridSystem, thermodynamicModel);
    }

    public IntegratedSecondaryCycleDefinition Definition => _definition;

    public IntegratedSecondaryCycleStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        GeneratorGridState committedElectricalState,
        IntegratedSecondaryCycleInputs inputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(committedTurbineState);
        ArgumentNullException.ThrowIfNull(committedElectricalState);
        ArgumentNullException.ThrowIfNull(inputs);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Integrated secondary-cycle step time must be greater than zero.");
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Integrated secondary-cycle inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use the integrated secondary cycle's canonical plant definition.", nameof(committedPlantState));
        }

        if (!ReferenceEquals(committedTurbineState.Definition, _definition.TurbineExpansionSystem))
        {
            throw new ArgumentException("Committed turbine state does not use the integrated secondary cycle's canonical turbine definition.", nameof(committedTurbineState));
        }

        if (!ReferenceEquals(committedElectricalState.Definition, _definition.GeneratorGridSystem))
        {
            throw new ArgumentException("Committed electrical state does not use the integrated secondary cycle's canonical generator/grid definition.", nameof(committedElectricalState));
        }

        var generatorGridStep = _generatorGridSolver.Step(
            committedPlantState,
            committedTurbineState,
            committedElectricalState,
            inputs.GeneratorGridInputs,
            deltaTime);
        var heatBalance = BuildHeatBalance(generatorGridStep.Snapshot, deltaTime);
        var snapshot = new IntegratedSecondaryCycleSnapshot(_definition, generatorGridStep.Snapshot, heatBalance);

        return new IntegratedSecondaryCycleStepResult(generatorGridStep, snapshot);
    }

    private static SecondaryCycleHeatBalanceAudit BuildHeatBalance(
        GeneratorGridSnapshot generatorGrid,
        TimeSpan deltaTime)
    {
        var feedwater = generatorGrid.CondensateFeedwater;
        var condenser = feedwater.CondenserSnapshot;
        var turbine = generatorGrid.TurbineExpansion;
        var primary = turbine.MainSteamNetwork.PrimaryCircuit;
        var thermofluid = feedwater.ThermofluidAudit;
        var mechanical = turbine.MechanicalAudit;
        var electrical = generatorGrid.ElectricalAudit;

        var classifiedSupplementalPower = primary.TotalNuclearHeatPower
            + primary.Boundaries.NetExternalPower
            + feedwater.TotalThermalConditioningPower
            - condenser.TotalHeatRejectionPower
            - turbine.TotalShaftPower;
        var supplementalClassificationResidual = thermofluid.SupplementalExternalPower.Watts
            - classifiedSupplementalPower.Watts;
        var shaftTransferResidual = mechanical.TotalShaftPower.Watts - turbine.TotalShaftPower.Watts;
        var mechanicalToElectricalResidual = mechanical.TotalExternalLoadPower.Watts - electrical.MechanicalInputPower.Watts;

        var initialCoupledEnergy = thermofluid.InitialTotalStoredEnergy + mechanical.InitialRotorKineticEnergy;
        var finalCoupledEnergy = thermofluid.FinalTotalStoredEnergy + mechanical.FinalRotorKineticEnergy;
        var coupledStoredEnergyChange = finalCoupledEnergy - initialCoupledEnergy;

        var coupledDomainExpectedPower = thermofluid.ExpectedExternalPower
            + mechanical.TotalShaftPower
            - mechanical.TotalExternalLoadPower;
        var coupledDomainResidual = coupledStoredEnergyChange.Joules
            - coupledDomainExpectedPower.Over(deltaTime).Joules;

        var netReactorToGridExternalPower = thermofluid.ExpectedExternalPower
            + turbine.TotalShaftPower
            - electrical.ElectricalExportPower
            - electrical.ConversionLossPower;
        var fullEnergyPathResidual = coupledStoredEnergyChange.Joules
            - netReactorToGridExternalPower.Over(deltaTime).Joules;

        return new SecondaryCycleHeatBalanceAudit(
            primary.TotalNuclearHeatPower,
            primary.Boundaries.NetExternalPower,
            thermofluid.HeatSourcePower,
            thermofluid.PumpHydraulicPowerExchange,
            feedwater.TotalThermalConditioningPower,
            condenser.TotalHeatRejectionPower,
            turbine.TotalShaftPower,
            electrical.MechanicalInputPower,
            electrical.ElectricalExportPower,
            electrical.ConversionLossPower,
            thermofluid.ExpectedExternalPower,
            netReactorToGridExternalPower,
            thermofluid.InitialTotalStoredEnergy,
            thermofluid.FinalTotalStoredEnergy,
            mechanical.InitialRotorKineticEnergy,
            mechanical.FinalRotorKineticEnergy,
            coupledStoredEnergyChange,
            supplementalClassificationResidual,
            shaftTransferResidual,
            mechanicalToElectricalResidual,
            coupledDomainResidual,
            fullEnergyPathResidual,
            thermofluid.ExpectedExternalMassFlowRate,
            thermofluid.MassClosureResidualKilograms);
    }
}
