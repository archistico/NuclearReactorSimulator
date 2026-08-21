namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>One immutable deterministic mission timeline row. Logical step/source sequence are the only ordering evidence.</summary>
public sealed record MissionPerformanceTimelineEntrySnapshot(
    long LogicalStep,
    MissionPerformanceTimelineEntryKind Kind,
    string SourceId,
    string Summary,
    long? SourceSequence,
    bool IsCritical,
    MissionPerformanceDrillDownTarget? DrillDownTarget = null);
