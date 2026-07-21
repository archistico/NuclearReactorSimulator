using NuclearReactorSimulator.Simulation.Physics.Control.Alarms;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

/// <summary>
/// M5.7 immutable snapshot separating the measured frame used for the current control decision from the candidate-state
/// instrumentation frame that will become committed input to the next logical step.
/// </summary>
public sealed record IntegratedAutomaticOperationSnapshot(
    MeasuredSignalFrame CommittedMeasuredSignals,
    AlarmedProtectedAutomaticControlSnapshot Control,
    InstrumentationSnapshot CandidateInstrumentation)
{
    public MeasuredSignalFrame NextMeasuredSignals => CandidateInstrumentation.MeasuredSignals;
}
