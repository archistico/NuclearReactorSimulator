using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Candidate M4.4 result. Thermofluid inventories are still committed only at the outer simulation boundary.
/// </summary>
public sealed record CondensateFeedwaterSystemStepResult(
    CondenserSystemStepResult CondenserStep,
    CondensateFeedwaterSystemSnapshot Snapshot)
{
    public PlantState CandidatePlantState => CondenserStep.CandidatePlantState;

    public TurbineExpansionState CandidateTurbineState => CondenserStep.CandidateTurbineState;
}
