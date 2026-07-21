using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>
/// Executes the M4.7 fixed-input full-plant reference condition headlessly and reports drift without correcting state.
/// </summary>
public sealed class FullPlantLongRunRunner
{
    private readonly FullPlantSolver _solver;

    public FullPlantLongRunRunner(
        IntegratedSecondaryCycleDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _solver = new FullPlantSolver(definition, thermodynamicModel);
    }

    public FullPlantLongRunResult Run(FullPlantReferenceOperatingPoint operatingPoint, int stepCount)
    {
        ArgumentNullException.ThrowIfNull(operatingPoint);

        if (!ReferenceEquals(operatingPoint.Definition, _solver.Definition))
        {
            throw new ArgumentException("Reference operating point does not use this runner's canonical definition.", nameof(operatingPoint));
        }

        if (stepCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepCount), stepCount, "Long-run step count must be greater than zero.");
        }

        var initialMass = TotalPlantMassKilograms(operatingPoint.InitialState);
        var initialCoupledEnergy = TotalCoupledStoredEnergyJoules(operatingPoint.InitialState);
        var initialRotorSpeeds = operatingPoint.InitialState.TurbineState.Rotors.ToDictionary(
            static item => item.RotorId,
            static item => item.AngularSpeed.RevolutionsPerMinute,
            StringComparer.Ordinal);

        var state = operatingPoint.InitialState;
        FullPlantStepResult? finalStep = null;
        double? firstElectricalOutputWatts = null;
        var maxMassInventoryDrift = 0d;
        var maxCoupledEnergyDrift = 0d;
        var maxRotorSpeedDrift = 0d;
        var maxElectricalOutputDrift = 0d;
        var maxMassClosureResidual = 0d;
        var maxEnergyClosureResidual = 0d;
        var sumNuclearHeatWatts = 0d;
        var sumShaftPowerWatts = 0d;
        var sumElectricalOutputWatts = 0d;

        for (var index = 0; index < stepCount; index++)
        {
            finalStep = _solver.Step(state, operatingPoint.Inputs, operatingPoint.StepSize);
            state = finalStep.CandidateState;
            var snapshot = finalStep.Snapshot;
            var audit = snapshot.HeatBalance;

            firstElectricalOutputWatts ??= snapshot.GrossElectricalOutputPower.Watts;
            maxMassInventoryDrift = Math.Max(
                maxMassInventoryDrift,
                Math.Abs(TotalPlantMassKilograms(state) - initialMass));
            maxCoupledEnergyDrift = Math.Max(
                maxCoupledEnergyDrift,
                Math.Abs(TotalCoupledStoredEnergyJoules(state) - initialCoupledEnergy));
            maxElectricalOutputDrift = Math.Max(
                maxElectricalOutputDrift,
                Math.Abs(snapshot.GrossElectricalOutputPower.Watts - firstElectricalOutputWatts.Value));
            maxMassClosureResidual = Math.Max(maxMassClosureResidual, Math.Abs(audit.MassClosureResidualKilograms));
            maxEnergyClosureResidual = Math.Max(maxEnergyClosureResidual, Math.Abs(audit.FullEnergyPathClosureResidualJoules));
            sumNuclearHeatWatts += snapshot.Performance.ReactorThermalPower.Watts;
            sumShaftPowerWatts += snapshot.Performance.TurbineShaftPower.Watts;
            sumElectricalOutputWatts += snapshot.Performance.GrossElectricalOutputPower.Watts;

            foreach (var rotor in state.TurbineState.Rotors)
            {
                var drift = Math.Abs(rotor.AngularSpeed.RevolutionsPerMinute - initialRotorSpeeds[rotor.RotorId]);
                maxRotorSpeedDrift = Math.Max(maxRotorSpeedDrift, drift);
            }
        }

        var final = finalStep ?? throw new InvalidOperationException("Long-run execution did not produce a final step.");
        var massDrift = TotalPlantMassKilograms(state) - initialMass;
        var energyDrift = TotalCoupledStoredEnergyJoules(state) - initialCoupledEnergy;
        var electricalOutputDrift = final.Snapshot.GrossElectricalOutputPower.Watts - firstElectricalOutputWatts!.Value;
        var criteria = operatingPoint.Criteria;
        var criteriaSatisfied = maxMassInventoryDrift <= criteria.MaximumAbsoluteMassInventoryDriftKilograms
            && maxCoupledEnergyDrift <= criteria.MaximumAbsoluteCoupledStoredEnergyDriftJoules
            && maxRotorSpeedDrift <= criteria.MaximumAbsoluteRotorSpeedDriftRevolutionsPerMinute
            && maxElectricalOutputDrift <= criteria.MaximumAbsoluteElectricalOutputDriftWatts
            && maxMassClosureResidual <= criteria.MaximumAbsoluteMassClosureResidualKilograms
            && maxEnergyClosureResidual <= criteria.MaximumAbsoluteFullEnergyPathClosureResidualJoules;

        return new FullPlantLongRunResult(
            operatingPoint.Id,
            stepCount,
            TimeSpan.FromTicks(checked(operatingPoint.StepSize.Ticks * (long)stepCount)),
            operatingPoint.InitialState,
            final,
            massDrift,
            maxMassInventoryDrift,
            energyDrift,
            maxCoupledEnergyDrift,
            maxRotorSpeedDrift,
            electricalOutputDrift,
            maxElectricalOutputDrift,
            maxMassClosureResidual,
            maxEnergyClosureResidual,
            Power.FromWatts(sumNuclearHeatWatts / stepCount),
            Power.FromWatts(sumShaftPowerWatts / stepCount),
            Power.FromWatts(sumElectricalOutputWatts / stepCount),
            criteria,
            criteriaSatisfied);
    }

    private static double TotalPlantMassKilograms(FullPlantState state)
        => state.PlantState.FluidNodes.Sum(static item => item.Mass.Kilograms);

    private static double TotalCoupledStoredEnergyJoules(FullPlantState state)
    {
        var thermofluid = state.PlantState.FluidNodes.Sum(static item => item.InternalEnergy.Joules)
            + state.PlantState.ThermalBodies.Sum(static item => item.StoredThermalEnergy.Joules);
        var rotor = state.Definition.TurbineExpansionSystem.Rotors.Sum(
            rotorDefinition => rotorDefinition.MomentOfInertia.KineticEnergyAt(
                state.TurbineState.GetRotor(rotorDefinition.Id).AngularSpeed).Joules);
        return thermofluid + rotor;
    }
}
