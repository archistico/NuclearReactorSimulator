using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed record CondenserSystemStepResult(
    TurbineExpansionStepResult TurbineStep,
    CondenserSystemSnapshot Snapshot)
{
    public PlantState CandidatePlantState => TurbineStep.CandidatePlantState;

    public TurbineExpansionState CandidateTurbineState => TurbineStep.CandidateTurbineState;
}
