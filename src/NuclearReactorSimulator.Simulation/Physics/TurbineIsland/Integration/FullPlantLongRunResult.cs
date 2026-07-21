using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

/// <summary>Immutable summary of the M4.7 deterministic fixed-input full-plant gate run.</summary>
public sealed record FullPlantLongRunResult(
    string OperatingPointId,
    int StepCount,
    TimeSpan SimulatedDuration,
    FullPlantState InitialState,
    FullPlantStepResult FinalStep,
    double MassInventoryDriftKilograms,
    double MaximumAbsoluteMassInventoryDriftKilograms,
    double CoupledStoredEnergyDriftJoules,
    double MaximumAbsoluteCoupledStoredEnergyDriftJoules,
    double MaximumAbsoluteRotorSpeedDriftRevolutionsPerMinute,
    double ElectricalOutputDriftWatts,
    double MaximumAbsoluteElectricalOutputDriftWatts,
    double MaximumAbsoluteMassClosureResidualKilograms,
    double MaximumAbsoluteFullEnergyPathClosureResidualJoules,
    Power AverageNuclearHeatPower,
    Power AverageTurbineShaftPower,
    Power AverageElectricalOutputPower,
    FullPlantSteadyStateCriteria Criteria,
    bool SteadyStateCriteriaSatisfied)
{
    public FullPlantSnapshot FinalSnapshot => FinalStep.Snapshot;

    public FullPlantState FinalState => FinalStep.CandidateState;
}
