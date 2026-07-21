using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

public sealed record IntegratedAutomaticOperationStepResult(
    AlarmedProtectedAutomaticFullPlantStepResult ControlledStep,
    InstrumentationStepResult InstrumentationStep,
    IntegratedAutomaticOperationState CandidateState,
    IntegratedAutomaticOperationSnapshot Snapshot);
