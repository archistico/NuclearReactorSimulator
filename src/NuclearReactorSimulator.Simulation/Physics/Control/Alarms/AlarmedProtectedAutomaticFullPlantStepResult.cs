using NuclearReactorSimulator.Simulation.Physics.Control.Protection;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

public sealed record AlarmedProtectedAutomaticFullPlantStepResult(
    ProtectedAutomaticFullPlantStepResult ProtectedStep,
    AlarmSystemStepResult AlarmStep,
    AlarmedProtectedAutomaticControlSnapshot Snapshot);
