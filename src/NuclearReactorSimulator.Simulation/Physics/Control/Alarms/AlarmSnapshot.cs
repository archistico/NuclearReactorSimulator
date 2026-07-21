using NuclearReactorSimulator.Domain.Physics.Control.Alarms;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

public sealed record AlarmSnapshot(
    string AlarmId,
    string Title,
    AlarmSeverity Severity,
    string? FirstOutGroupId,
    bool ConditionActive,
    bool IsLatched,
    bool IsAcknowledged,
    bool IsAnnunciated,
    bool IsFirstOut,
    long? ActivationSequence,
    AlarmAnnunciatorState AnnunciatorState,
    bool AcknowledgeRequested,
    bool AcknowledgeApplied,
    bool ResetRequested,
    bool ResetAccepted);
