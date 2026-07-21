using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

/// <summary>
/// Executes a fixed-input reference operating point headlessly for deterministic M3 gate verification.
/// No wall-clock timing, automatic control, or state correction is introduced.
/// </summary>
public sealed class PrimaryCircuitLongRunRunner
{
    private readonly IntegratedPrimaryCircuitSolver _solver;

    public PrimaryCircuitLongRunRunner(
        IntegratedPrimaryCircuitDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _solver = new IntegratedPrimaryCircuitSolver(definition, thermodynamicModel);
    }

    public PrimaryCircuitLongRunResult Run(PrimaryCircuitReferenceOperatingPoint operatingPoint, int stepCount)
    {
        ArgumentNullException.ThrowIfNull(operatingPoint);

        if (!ReferenceEquals(operatingPoint.Definition, _solver.Definition))
        {
            throw new ArgumentException(
                "Reference operating point does not use this runner's canonical integrated definition.",
                nameof(operatingPoint));
        }

        if (stepCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepCount), stepCount, "Long-run step count must be greater than zero.");
        }

        var initialPlant = new PlantSnapshot(operatingPoint.InitialPlantState);
        var initialMassKilograms = operatingPoint.InitialPlantState.FluidNodes.Sum(static item => item.Mass.Kilograms);
        var initialEnergyJoules = operatingPoint.InitialPlantState.FluidNodes.Sum(static item => item.InternalEnergy.Joules)
            + operatingPoint.InitialPlantState.ThermalBodies.Sum(static item => item.StoredThermalEnergy.Joules);

        var state = operatingPoint.InitialPlantState;
        IntegratedPrimaryCircuitStepResult? finalStep = null;
        var maxBalanceMassRateResidual = 0d;
        var maxMassClosureResidual = 0d;
        var maxBalancePowerResidual = 0d;
        var maxEnergyClosureResidual = 0d;

        for (var index = 0; index < stepCount; index++)
        {
            finalStep = _solver.Step(state, operatingPoint.Inputs, operatingPoint.StepSize);
            state = finalStep.CandidateState;
            var audit = finalStep.NetworkStep.Audit;
            maxBalanceMassRateResidual = Math.Max(maxBalanceMassRateResidual, Math.Abs(audit.BalanceMassRateResidualKilogramsPerSecond));
            maxMassClosureResidual = Math.Max(maxMassClosureResidual, Math.Abs(audit.MassClosureResidualKilograms));
            maxBalancePowerResidual = Math.Max(maxBalancePowerResidual, Math.Abs(audit.BalancePowerResidualWatts));
            maxEnergyClosureResidual = Math.Max(maxEnergyClosureResidual, Math.Abs(audit.EnergyClosureResidualJoules));
        }

        var final = finalStep ?? throw new InvalidOperationException("Long-run execution did not produce a final step.");
        var finalMassKilograms = state.FluidNodes.Sum(static item => item.Mass.Kilograms);
        var finalEnergyJoules = state.FluidNodes.Sum(static item => item.InternalEnergy.Joules)
            + state.ThermalBodies.Sum(static item => item.StoredThermalEnergy.Joules);

        return new PrimaryCircuitLongRunResult(
            operatingPoint.Id,
            stepCount,
            TimeSpan.FromTicks(checked(operatingPoint.StepSize.Ticks * (long)stepCount)),
            initialPlant,
            final,
            finalMassKilograms - initialMassKilograms,
            finalEnergyJoules - initialEnergyJoules,
            maxBalanceMassRateResidual,
            maxMassClosureResidual,
            maxBalancePowerResidual,
            maxEnergyClosureResidual);
    }
}
