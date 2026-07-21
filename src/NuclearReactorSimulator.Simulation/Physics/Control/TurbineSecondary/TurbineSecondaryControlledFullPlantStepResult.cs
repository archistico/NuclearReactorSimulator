using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

public sealed record TurbineSecondaryControlledFullPlantStepResult(
    ReactorPrimaryControlStepResult ReactorPrimaryControlStep,
    TurbineSecondaryControlStepResult TurbineSecondaryControlStep,
    FullPlantStepResult FullPlantStep,
    IntegratedSecondaryCycleInputs EffectivePlantInputs,
    IntegratedAutomaticControlSnapshot Snapshot);
