namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>Immutable deterministic mission/performance event summary. Ordering is logical-step based; wall clock is never authoritative.</summary>
public sealed record MissionPerformanceEventSnapshot(
    long LogicalStep,
    MissionPerformanceEventKind Kind,
    string SourceId,
    string Summary,
    long? SourceSequence = null,
    bool IsCritical = false);
