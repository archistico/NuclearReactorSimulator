using NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Supervisory;

public sealed record SupervisoryOperationStepResult(
    SupervisoryOperationState CandidateState,
    ReactorPrimaryControlInputs ReactorPrimaryInputs,
    TurbineSecondaryControlInputs TurbineSecondaryInputs,
    bool AppliedDecision);
