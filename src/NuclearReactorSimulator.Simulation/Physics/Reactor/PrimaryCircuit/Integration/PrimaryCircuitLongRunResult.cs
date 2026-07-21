using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

/// <summary>Immutable summary of a deterministic headless primary-circuit baseline run.</summary>
public sealed record PrimaryCircuitLongRunResult(
    string OperatingPointId,
    int StepCount,
    TimeSpan SimulatedDuration,
    PlantSnapshot InitialPlant,
    IntegratedPrimaryCircuitStepResult FinalStep,
    double MassInventoryDriftKilograms,
    double StoredEnergyDriftJoules,
    double MaximumAbsoluteBalanceMassRateResidualKilogramsPerSecond,
    double MaximumAbsoluteMassClosureResidualKilograms,
    double MaximumAbsoluteBalancePowerResidualWatts,
    double MaximumAbsoluteEnergyClosureResidualJoules)
{
    public Mass FinalTotalMass => FinalStep.Snapshot.TotalPlantMass;

    public Energy FinalTotalStoredEnergy => FinalStep.Snapshot.TotalStoredEnergy;
}
