using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

public sealed record TurbineSecondaryControlStepResult(
    ControlAndActuatorStepResult ControlAndActuatorStep,
    TurbineSecondaryControlState CandidateState,
    FullPlantState CommandedFullPlantState,
    TurbineSecondaryControlSnapshot Snapshot);
