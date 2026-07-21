using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Protection;

public sealed record ProtectedAutomaticFullPlantStepResult(
    ProtectionSystemStepResult ProtectionStep,
    ReactorPrimaryControlStepResult ReactorPrimaryControlStep,
    TurbineSecondaryControlStepResult TurbineSecondaryControlStep,
    FullPlantStepResult FullPlantStep,
    IntegratedSecondaryCycleInputs EffectivePlantInputs,
    ProtectedAutomaticControlSnapshot Snapshot);
